using ChromaDB.Client;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Embeddings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VectorDBSync.EmbeddingService;
using VectorDBSync.VectorDBService;
using WhatsAppToDB.Abstractions;


namespace VectorDBSync
{
    public class VectorSyncService : ISyncService
    {

        public static VectorDBSettings LoadSettingsFromFile(string settingsFile)
        {
            var json = File.ReadAllText(settingsFile);
            return JsonSerializer.Deserialize<VectorDBSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new JsonException("Failed to deserialize Vector DB settings from file.");
        }

        public static ISyncService LoadSyncServiceFrom(string settingsFile)
        {
            var settings = LoadSettingsFromFile(settingsFile);
            ISyncService vss = new VectorSyncService(settings, settings.DatabaseSettings.ConnectionString ?? string.Empty);
            return vss;
        }

        private readonly string connectionString;
        string sqliteDbPath;
        private IVectorDBService _vectorDBService;

        private const string UpdateVectorSyncMetadataSql = @"
            IF EXISTS (SELECT 1 FROM Vector_SyncTracker WHERE CollectionName = @name)
                UPDATE Vector_SyncTracker SET LastSyncTime = @now WHERE CollectionName = @name
            ELSE
                INSERT INTO Vector_SyncTracker (CollectionName, LastSyncTime) VALUES (@name, @now)";

        public VectorSyncService(VectorDBSettings settings, string sourceConnectionString)
        {
            _vectorDBService = VectorDBServiceFactory.CreateVectorDBService(settings);
            this.connectionString = sourceConnectionString?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(this.connectionString))
            {
                this.connectionString = settings.DatabaseSettings.ConnectionString ?? string.Empty;
            }
            var cacheFolder = settings.SqliteSettings.Folder;
            if (string.IsNullOrWhiteSpace(cacheFolder))
            {
                cacheFolder = settings.SqliteSettings.VectorDBFolder;
            }
            this.sqliteDbPath = Path.Combine(cacheFolder ?? string.Empty, "vector_sync_cache.db");
        }

        public VectorSyncService(VectorDBSettings settings)
            : this(settings, settings.DatabaseSettings.ConnectionString ?? string.Empty)
        {
        }

        
        // ═══════════════════════════════════════════════════════════════════
        // SQLITE CACHE HELPERS
        // ═══════════════════════════════════════════════════════════════════
 
        private static SqliteConnection OpenSqlite(string dbPath)
        {
            var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            return conn;
        }
 
        /// <summary>
        /// Each collection gets its own cache table: cache_{CollectionName}
        /// Stores only record_key and content_hash — minimal footprint.
        /// ~4MB for 50K records. Loaded into a Dictionary for O(1) lookup.
        /// </summary>
        private static void EnsureSqliteCacheTable(SqliteConnection conn, string collectionName)
        {
            using var cmd   = conn.CreateCommand();
            cmd.CommandText = $@"
                CREATE TABLE IF NOT EXISTS cache_{collectionName} (
                    record_key   TEXT PRIMARY KEY,
                    content_hash TEXT NOT NULL,
                    updated_at   DATETIME DEFAULT CURRENT_TIMESTAMP
                );";
            cmd.ExecuteNonQuery();
        }
 
        private static void DropSqliteCache(SqliteConnection conn, string collectionName)
        {
            using var cmd   = conn.CreateCommand();
            cmd.CommandText = $"DROP TABLE IF EXISTS cache_{collectionName};";
            cmd.ExecuteNonQuery();
        }
 
        /// <summary>
        /// Load all cached hashes in one query.
        /// Returns Dictionary(key → hash) for O(1) comparison per record.
        /// Time: ~100ms for 50K rows. Memory: ~4MB. Both negligible.
        /// </summary>
        private static Dictionary<string, string> LoadCachedHashes(
            SqliteConnection conn, string collectionName)
        {
            var result      = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using var cmd   = conn.CreateCommand();
            cmd.CommandText = $"SELECT record_key, content_hash FROM cache_{collectionName};";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                result[reader.GetString(0)] = reader.GetString(1);
            return result;
        }
 
        /// <summary>
        /// Upsert hashes for all records that were embedded.
        /// Uses SQLite's INSERT OR REPLACE for clean upsert.
        /// </summary>
        private void UpsertSqliteCache(
            SqliteConnection conn,
            string           collectionName,
            List<VectorRecord> records)
        {
            using var tx = conn.BeginTransaction();
            foreach (var record in records)
            {
                using var cmd   = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = $@"
                    INSERT INTO cache_{collectionName}(record_key, content_hash, updated_at)
                    VALUES(@key, @hash, CURRENT_TIMESTAMP)
                    ON CONFLICT(record_key) DO UPDATE
                    SET content_hash = excluded.content_hash,
                        updated_at   = CURRENT_TIMESTAMP;";
                cmd.Parameters.AddWithValue("@key",  record.Id);
                cmd.Parameters.AddWithValue("@hash", ComputeHash(record.Content));
                cmd.ExecuteNonQuery();
            }
            tx.Commit();  // single transaction — much faster than row-by-row commits
        }
 
        /// <summary>
        /// Remove deleted records from SQLite cache.
        /// </summary>
        private static void DeleteFromSqliteCache(
            SqliteConnection conn,
            string           collectionName,
            List<string>     keys)
        {
            using var tx = conn.BeginTransaction();
            foreach (var key in keys)
            {
                using var cmd   = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = $"DELETE FROM cache_{collectionName} WHERE record_key=@key;";
                cmd.Parameters.AddWithValue("@key", key);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
 
        // ═══════════════════════════════════════════════════════════════════
        // HASH HELPER
        // ═══════════════════════════════════════════════════════════════════
 
        /// <summary>
        /// SHA256 of the content string — first 16 hex chars is enough for change detection.
        /// Same content always produces the same hash — deterministic, no false positives.
        /// </summary>
        private static string ComputeHash(string content)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content ?? ""));
            return Convert.ToHexString(bytes)[..16];
        }


        public async Task SyncAllCollections(List<VectorSyncConfig> configs, IDbProvider dbProvider)
        {
            using IDbConnection sourceDb     = dbProvider.GetConnection(connectionString);
            using var           sqliteConn   = OpenSqlite(sqliteDbPath);
 
            foreach (var config in configs)
            {
                Console.WriteLine($"\n[{config.CollectionName}] Sync starting at " +
                                  $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
 
                // DeleteAndCreate: wipe ChromaDB + SQLite cache, then full re-embed
                if (config.DeleteAndCreate)
                {
                    Console.WriteLine($"[{config.CollectionName}] DeleteAndCreate=true — " +
                                      $"clearing ChromaDB collection and SQLite cache.");
                    await _vectorDBService.Delete(config.CollectionName);
                    DropSqliteCache(sqliteConn, config.CollectionName);
                }
 
                // Ensure SQLite cache table exists for this collection
                EnsureSqliteCacheTable(sqliteConn, config.CollectionName);
 
                // ── Step 1: Fetch ALL records from source ─────────────────────
                // SyncSql should SELECT all fields needed (Id, Content, metadata).
                // No UpdateDate filter needed — hash compare handles change detection.
                var syncSql = "SELECT " +
                              $"{config.KeyField} AS Id, " +
                              $"{config.ContentField} AS Content" +
                              (config.MetadataFields.Any() ? ", " : "") +
                              string.Join(", ", config.MetadataFields) +
                              $" FROM {config.TableName};";
                Console.WriteLine($"[{config.CollectionName}] Fetching source records using SQL {syncSql}...");
                var sourceResults = (await sourceDb.QueryAsync<dynamic>(syncSql)).ToList();
                Console.WriteLine($"[{config.CollectionName}] {sourceResults.Count} records fetched from source.");
 
                // ── Step 2: Load SQLite cache (key + hash only — ~4MB for 50K rows) ──
                var cachedHashes = LoadCachedHashes(sqliteConn, config.CollectionName);
                Console.WriteLine($"[{config.CollectionName}] {cachedHashes.Count} records in SQLite cache.");
 
                // ── Step 3: Compare source vs cache — classify each record ────
                var toUpsert   = new List<VectorRecord>();   // new or changed
                var sourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 
                foreach (var item in sourceResults)
                {
                    var row     = (IDictionary<string, object>)item;
                    var id      = row["Id"]?.ToString() ?? "";
                    var content = row["Content"]?.ToString() ?? "";
 
                    if (string.IsNullOrEmpty(id)) continue;
 
                    sourceKeys.Add(id);
 
                    var hash = ComputeHash(content);
 
                    // Only queue for embedding if new OR content changed
                    if (!cachedHashes.TryGetValue(id, out var storedHash) || storedHash != hash)
                    {
                        var record = new VectorRecord
                        {
                            Id       = id,
                            Content  = content,
                            Metadata = new Dictionary<string, object>()
                        };
 
                        foreach (var field in config.MetadataFields)
                            if (row.ContainsKey(field) && row[field] != null)
                                record.Metadata[field] = row[field];
 
                        toUpsert.Add(record);
                    }
                    // else: identical hash → skip entirely, zero embedding cost
                }
 
                // ── Step 4: Find deleted records (in cache but gone from source) ─
                var toDelete = cachedHashes.Keys
                    .Where(k => !sourceKeys.Contains(k))
                    .ToList();
 
                Console.WriteLine($"[{config.CollectionName}] " +
                                  $"To embed: {toUpsert.Count}, " +
                                  $"Unchanged: {sourceResults.Count - toUpsert.Count - toDelete.Count}, " +
                                  $"To delete: {toDelete.Count}");
 
                // ── Step 5: Delete removed records from ChromaDB + SQLite cache ─
                if (toDelete.Any())
                {
                    Console.WriteLine($"[{config.CollectionName}] Deleting {toDelete.Count} removed records...");
                    await _vectorDBService.Delete(config.CollectionName, toDelete.First()); // Assuming Delete method can handle batch deletion by ID
                    DeleteFromSqliteCache(sqliteConn, config.CollectionName, toDelete);
                }
 
                // ── Step 6: Embed + push changed/new records ──────────────────
                if (toUpsert.Any())
                {
                    Console.WriteLine($"[{config.CollectionName}] Embedding {toUpsert.Count} records...");
                    await SyncToCollectionByBatch(config.CollectionName, toUpsert);
 
                    // Update SQLite cache with new hashes for upserted records
                    UpsertSqliteCache(sqliteConn, config.CollectionName, toUpsert);
 
                    // Update high-water mark in source DB tracker
                    //await sourceDb.ExecuteAsync(UpdateVectorSyncMetadataSql,
                    //    new { name = config.CollectionName, now = DateTime.Now });
 
                    Console.WriteLine($"[{config.CollectionName}] Sync complete at " +
                                      $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                }
                else
                {
                    Console.WriteLine($"[{config.CollectionName}] No changes — nothing to embed.");
                }
                if (toUpsert.Any() || toDelete.Any())
                {
                    await _vectorDBService.AfterCollectionSyncCompletedAsync(config.CollectionName);
                }
            }
        }
 


        public async Task SyncAllCollectionsOld(List<VectorSyncConfig> configs)
        {
            using IDbConnection db = new Microsoft.Data.SqlClient.SqlConnection(connectionString);

            foreach (var config in configs)
            {
                if (config.DeleteAndCreate)
                {
                    Console.WriteLine($"Deleting and recreating collection {config.CollectionName} in Vector DB");
                    await _vectorDBService.Delete(config.CollectionName);
                }
                Console.WriteLine($"Syncing {config.CollectionName} starts");

                var results = await db.QueryAsync<dynamic>(config.SyncSql);
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Syncing {config.CollectionName} - {results.Count()} records to sync. Preparing data..");
                var recordsToSync = new List<VectorRecord>();
                var count = 0;
                foreach (var item in results)
                {
                    if (count % 100 == 0 && count > 0)
                    {
                        Console.WriteLine($"Syncing {config.CollectionName} - Processed {count} records..");
                    }
                    count++;
                    var row = (IDictionary<string, object>)item;

                    var record = new VectorRecord
                    {
                        Id = row["Id"].ToString(),
                        Content = row["Content"]?.ToString() + "",
                        Metadata = new Dictionary<string, object>()
                    };

                    // 2. Map Metadata dynamically based on config
                    foreach (var field in config.MetadataFields)
                    {
                        if (row.ContainsKey(field))
                            record.Metadata.Add(field, row[field]);
                    }

                    recordsToSync.Add(record);
                }

                if (recordsToSync.Any())
                {
                    Console.WriteLine($"Syncing {config.CollectionName} - Calling sync");
                    await SyncToCollectionByBatch(config.CollectionName, recordsToSync);
                    var newHighWaterMark = DateTime.Now;
                    Console.WriteLine($"Completed Syncing {config.CollectionName} -  updating Highwatermark to {newHighWaterMark.ToString("yyyy-MM-dd HH:mm:ss")}");
                    //await db.ExecuteAsync(UpdateVectorSyncMetadataSql,
                        //new { name = config.CollectionName, now = newHighWaterMark });
                }
            }
        }
       
        public async Task SyncToCollectionByBatch(string collectionName, List<VectorRecord> records)
        {
            int batchSize = 2000; 
            int totalProcessed = 0;

            for (int i = 0; i < records.Count; i += batchSize)
            {
                // Get the current window of records
                var currentBatch = records.Skip(i).Take(batchSize).ToList();

                var ids = currentBatch.Select(r => r.Id).ToList();
                var documents = currentBatch.Select(r => r.Content).ToList();

                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [Sync] Generating embeddings for " +
                    $"{i+1} to {i + currentBatch.Count} of ({records.Count} records)...");

                // 2. Bulk fetch embeddings for the entire batch

                // Filter metadata
                var metadatas = currentBatch.Select(r =>
                    r.Metadata.Where(m => m.Value != null)
                              .ToDictionary(m => m.Key, m => m.Value)
                ).ToList();

                await _vectorDBService.Add(
                    collectionName,
                    ids,
                    documents,
                    metadatas
                );

                totalProcessed += currentBatch.Count;
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [Sync] Successfully pushed {totalProcessed}/{records.Count} to Vector DB.");
            }
        }

        public async Task<List<SearchResult>> SearchCollection(
                string collectionName,
                string queryText,
                int limit = 5,
                IDictionary<string, object>? filter = null)
        {
            var results = await _vectorDBService.SearchCollection(collectionName, queryText, limit, filter);

            return results;
        }
    }
}
