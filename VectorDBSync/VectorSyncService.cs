using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using VectorDBSync.EmbeddingService;
using VectorDBSync.VectorDBService;
using WhatsAppToDB.Abstractions;


namespace VectorDBSync
{
    public class VectorSyncService : ISyncService
    {

        private static VectorDBSettings LoadSettingsFromFile(string settingsFile)
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
            ISyncService vss = new VectorSyncService(settings, string.Empty);
            return vss;
        }

        private readonly string connectionString;
        string sqliteDbPath;
        private IVectorDBService _vectorDBService;
        private readonly IEmbeddingService _embeddingService;
        private readonly SQLiteCacheService _sqliteCacheService = new();        

        public VectorSyncService(VectorDBSettings settings, string sourceConnectionString)
        {
            _vectorDBService = VectorDBServiceFactory.CreateVectorDBService(settings);
            _embeddingService = EmbeddingServiceFactory.Create(settings.EmbeddingServiceSettings);
            this.connectionString = sourceConnectionString?.Trim() ?? string.Empty;
            var cacheFolder = settings.CacheFolder;
            if (string.IsNullOrWhiteSpace(cacheFolder))
            {
                cacheFolder = settings.VectorDBProviderSettings.VectorDBFolder;
            }
            this.sqliteDbPath = Path.Combine(cacheFolder ?? string.Empty, "vector_sync_cache.db");
        }

        public VectorSyncService(VectorDBSettings settings)
            : this(settings, string.Empty)
        {
        }

        
        private static string QuoteIfIdentifier(string field, string dbProviderName)
        {
            if (string.IsNullOrWhiteSpace(field))
                return field;

            field = field.Trim();

            if (dbProviderName.ToLower()== "sqlite" || dbProviderName.ToLower() == "mssql")
            {
                return field;
            }

            // Treat as expression if:
            // - contains quotes
            // - contains comma
            // - contains spaces
            // - starts with digit
            // - contains brackets/operators

            bool isExpression =
                field.Contains('"') ||
                field.Contains('\'') ||
                field.Contains(',') ||
                field.Contains(' ') ||
                char.IsDigit(field[0]) ||
                field.Contains("(") ||
                field.Contains(")") ||
                field.Contains("+") ||
                field.Contains("-") ||
                field.Contains("*") ||
                field.Contains("/") ||
                field.Contains("|");

            if (isExpression)
                return field;

            return $"\"{field}\"";
        }


        public async Task SyncAllCollections(List<VectorSyncConfig> configs, IDbProvider dbProvider)
        {
            using IDbConnection sourceDb     = dbProvider.GetConnection(connectionString);
            using var           sqliteConn   = _sqliteCacheService.Open(sqliteDbPath);
 
            foreach (var config in configs)
            {
                Console.WriteLine($"\n[{config.CollectionName}] Sync starting at " +
                                  $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
 
                // DeleteAndCreate: wipe vector DB collection + SQLite cache, then full re-embed
                if (config.DeleteAndCreate)
                {
                    Console.WriteLine($"[{config.CollectionName}] DeleteAndCreate=true — " +
                                      $"clearing vector DB collection and SQLite cache.");
                    await _vectorDBService.Delete(config.CollectionName);
                    _sqliteCacheService.DropSqliteCache(sqliteConn, config.CollectionName);
                }
 
                // Ensure SQLite cache table exists for this collection
                _sqliteCacheService.EnsureSqliteCacheTable(sqliteConn, config.CollectionName);
               
                var keyfield = QuoteIfIdentifier(config.KeyField, dbProvider.Name);
                var contentfield = QuoteIfIdentifier(config.ContentField, dbProvider.Name);
                var metafields = config.MetadataFields.Select(f => QuoteIfIdentifier(f, dbProvider.Name)).ToList();
                var syncSql = "SELECT " +
                              $@"{keyfield} AS ""Id"", " +
                              $@"{contentfield} AS ""Content""" +
                              (config.MetadataFields.Any() ? ", " : "") +
                              string.Join(", ", metafields) +
                              $" FROM {config.TableName};";
                Console.WriteLine($"[{config.CollectionName}] Fetching source records using SQL {syncSql}...");
                var sourceResults = (await sourceDb.QueryAsync<dynamic>(syncSql)).ToList();
                Console.WriteLine($"[{config.CollectionName}] {sourceResults.Count} records fetched from source.");
 
                // -- Step 2: Load SQLite cache (key + hash only — ~4MB for 50K rows) --
                var cachedHashes = _sqliteCacheService.LoadCachedHashes(sqliteConn, config.CollectionName);
                Console.WriteLine($"[{config.CollectionName}] {cachedHashes.Count} records in SQLite cache.");
 
                // -- Step 3: Compare source vs cache — classify each record ----
                var toUpsert   = new List<VectorRecord>();   // new or changed
                var sourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 
                foreach (var item in sourceResults)
                {
                    var row     = (IDictionary<string, object>)item;
                    var id      = row["Id"]?.ToString() ?? "";
                    var content = row["Content"]?.ToString() ?? "";
 
                    if (string.IsNullOrEmpty(id)) continue;
                    if (string.IsNullOrEmpty(content)) continue;
 
                    sourceKeys.Add(id);
 
                    var hash = _sqliteCacheService.ComputeHash(content);
 
                    // Only queue for embedding if new OR content changed
                    if (!cachedHashes.TryGetValue(id, out var storedHash) || storedHash != hash)
                    {
                        var record = new VectorRecord
                        {
                            Id       = id,
                            Content  = content,
                            Metadata = new Dictionary<string, object>()
                        };

                        record.VectorId = _sqliteCacheService.GetOrCreateVectorId(sqliteConn, config.CollectionName, id);
                        if (!SQLiteCacheService.IsValidVectorId(record.VectorId))
                        {
                            record.VectorId = Guid.NewGuid().ToString();
                        }
                        record.Metadata["_source_id"] = id;

                        foreach (var field in config.MetadataFields)
                            if (row.ContainsKey(field) && row[field] != null)
                                record.Metadata[field] = row[field];
 
                        toUpsert.Add(record);
                    }
                    // else: identical hash ? skip entirely, zero embedding cost
                }
 
                // -- Step 4: Find deleted records (in cache but gone from source) -
                var toDelete = cachedHashes.Keys
                    .Where(k => !sourceKeys.Contains(k))
                    .ToList();
 
                Console.WriteLine($"[{config.CollectionName}] " +
                                  $"To embed: {toUpsert.Count}, " +
                                  $"Unchanged: {sourceResults.Count - toUpsert.Count - toDelete.Count}, " +
                                  $"To delete: {toDelete.Count}");
 
                // -- Step 5: Delete removed records from vector DB + SQLite cache -
                if (toDelete.Any())
                {
                    Console.WriteLine($"[{config.CollectionName}] Deleting {toDelete.Count} removed records...");
                    var cachedVectorIds = _sqliteCacheService.LoadCachedVectorIds(sqliteConn, config.CollectionName);
                    foreach (var deletedId in toDelete)
                    {
                        if (cachedVectorIds.TryGetValue(deletedId, out var vectorId) && !string.IsNullOrWhiteSpace(vectorId))
                        {
                            await _vectorDBService.Delete(config.CollectionName, vectorId);
                        }
                    }
                    _sqliteCacheService.DeleteFromSqliteCache(sqliteConn, config.CollectionName, toDelete);
                }
 
                // -- Step 6: Embed + push changed/new records ------------------
                if (toUpsert.Any())
                {
                    Console.WriteLine($"[{config.CollectionName}] Embedding {toUpsert.Count} records...");
                    await SyncToCollectionByBatch(config.CollectionName, toUpsert);

                    // Update SQLite cache with new hashes for upserted records
                    _sqliteCacheService.UpsertSqliteCache(sqliteConn, config.CollectionName, toUpsert);
 
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
 
        public async Task SyncToCollectionByBatch(string collectionName, List<VectorRecord> records)
        {
            int batchSize = 2000; 
            int totalProcessed = 0;

            for (int i = 0; i < records.Count; i += batchSize)
            {
                // Get the current window of records
                var currentBatch = records.Skip(i).Take(batchSize).ToList();

                var ids = currentBatch.Select(r =>
                    {
                        if (!SQLiteCacheService.IsValidVectorId(r.VectorId))
                        {
                            r.VectorId = Guid.NewGuid().ToString();
                        }
                        return r.VectorId!;
                    }
                ).ToList();
                var documents = currentBatch.Select(r => r.Content).ToList();

                Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [Sync] Generating embeddings for " +
                    $"{i+1} to {i + currentBatch.Count} of ({records.Count} records)...");

                // 2. Bulk fetch embeddings for the entire batch
                var vectors = await _embeddingService.GetVectors(documents);

                // Filter metadata
                var metadatas = currentBatch.Select(r =>
                    r.Metadata.Where(m => m.Value != null)
                              .ToDictionary(m => m.Key, m => m.Value)
                ).ToList();

                await _vectorDBService.Add(
                    collectionName,
                    ids,
                    vectors,
                    documents,
                    metadatas
                );

                totalProcessed += currentBatch.Count;
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [Sync] Successfully pushed {totalProcessed}/{records.Count} to Vector DB.");
            }
        }

        public async Task<List<VectorSearchResult>> SearchCollection(
                string collectionName,
                string queryText,
                int limit = 5,
                IDictionary<string, object>? filter = null)
        {
            var queryVector = await _embeddingService.GetVector(queryText);
            var results = await _vectorDBService.SearchCollection(collectionName, queryVector, queryText: null, limit, filter);

            return results;
        }
    }
}


