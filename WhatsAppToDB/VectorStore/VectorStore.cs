using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using WhatsAppToDB.Abstractions;

namespace WhatsAppToDB.VectorStore
{
    /// <summary>
    /// Represents one entry in the JSON file.
    /// </summary>
    public class SqlMemoryEntry
    {
        public string Name { get; set; }    // The intent/question — this gets embedded
        public string Module { get; set; }  // Sales, Finance, Inventory, etc.
        public string Query { get; set; }   // The actual SQL — stored but not embedded
    }

    /// <summary>
    /// Result returned from a vector search.
    /// </summary>
    public class SearchResult
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public string Query { get; set; }
        public double Score { get; set; }   // Cosine similarity: 1.0 = perfect match
    }

    /// <summary>
    /// SQLite-backed vector store with incremental upsert.
    /// - Zero configuration: creates the .db file on first run
    /// - Portable: copy the .db file to any server and it works
    /// - Incremental: only re-embeds changed entries (detected by content hash)
    /// </summary>
    public class VectorStore : IDisposable
    {
        private readonly SqliteConnection _db;
        private readonly IEmbeddingProvider _embeddings;
        private bool _initialized = false;

        /// <param name="embeddingProvider">Any IEmbeddingProvider implementation</param>
        /// <param name="dbPath">Path to the SQLite .db file — auto-created if missing</param>
        public VectorStore(IEmbeddingProvider embeddingProvider, string dbPath = "sql_memory.db")
        {
            _embeddings = embeddingProvider;
            _db = new SqliteConnection($"Data Source={dbPath}");
            _db.Open();
            LoadSqliteVecExtension();
            InitializeSchema();
        }

        // ─────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────

        /// <summary>
        /// Load entries from a JSON file.
        /// - New entries are embedded and inserted.
        /// - Changed entries (Name/Module/Query changed) are re-embedded and updated.
        /// - Unchanged entries are skipped — no API call made.
        /// - Entries removed from the JSON are NOT auto-deleted (call DeleteByName if needed).
        /// </summary>
        public async Task LoadFromJsonAsync(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
                throw new FileNotFoundException($"JSON file not found: {jsonFilePath}");

            var json = await File.ReadAllTextAsync(jsonFilePath);
            var entries = JsonSerializer.Deserialize<List<SqlMemoryEntry>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (entries == null || entries.Count == 0)
                return;

            // Separate into new vs changed (need embedding) vs unchanged (skip)
            var toEmbed = new List<(SqlMemoryEntry Entry, string Hash)>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Name))
                    continue;

                var hash = ComputeHash(entry);
                var existingHash = GetStoredHash(entry.Name);

                if (existingHash == null || existingHash != hash)
                    toEmbed.Add((entry, hash));
                // else: unchanged — skip entirely, no API call
            }

            if (toEmbed.Count == 0)
            {
                Console.WriteLine($"[VectorStore] All {entries.Count} entries unchanged — no embedding needed.");
                return;
            }

            Console.WriteLine($"[VectorStore] Embedding {toEmbed.Count} new/changed entries " +
                              $"(skipping {entries.Count - toEmbed.Count} unchanged)...");

            // Batch embed all changed entries in one API call (where supported)
            var texts = toEmbed.Select(x => x.Entry.Name).ToList();
            var embeddings = await _embeddings.GenerateBatchAsync(texts);

            // Upsert each one
            for (int i = 0; i < toEmbed.Count; i++)
            {
                var (entry, hash) = toEmbed[i];
                var embedding = embeddings[i];
                await UpsertAsync(entry, embedding, hash);
            }

            Console.WriteLine($"[VectorStore] Done. Total entries in store: {GetCount()}");
        }

        /// <summary>
        /// Add or update a single entry.
        /// Skips the API call if entry is unchanged (same name, module, query).
        /// </summary>
        public async Task AddOrUpdateAsync(SqlMemoryEntry entry)
        {
            var hash = ComputeHash(entry);
            var existingHash = GetStoredHash(entry.Name);

            if (existingHash == hash)
            {
                Console.WriteLine($"[VectorStore] '{entry.Name}' unchanged — skipped.");
                return;
            }

            var embedding = await _embeddings.GenerateAsync(entry.Name);
            await UpsertAsync(entry, embedding, hash);
            Console.WriteLine($"[VectorStore] '{entry.Name}' upserted.");
        }

        /// <summary>
        /// Search for the most similar entries to a user's question.
        /// Returns top-K results ordered by cosine similarity (highest first).
        /// </summary>
        public async Task<List<SearchResult>> SearchAsync(string question, int topK = 5)
        {
            var queryEmbedding = await _embeddings.GenerateAsync(question);
            return SearchByVector(queryEmbedding, topK);
        }

        /// <summary>
        /// Search using a pre-computed embedding vector (avoids extra API call).
        /// </summary>
        public List<SearchResult> SearchByVector(float[] queryEmbedding, int topK = 5)
        {
            var vectorBlob = FloatsToBlob(queryEmbedding);

            // sqlite-vec cosine distance (0 = identical, 2 = opposite)
            // Convert to similarity score: 1 - (distance / 2) → range [0, 1]
            var cmd = _db.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    m.name,
                    m.module,
                    m.query,
                    1.0 - (vec_distance_cosine(v.embedding, @queryVec) / 2.0) AS score
                FROM sql_memory_vec v
                JOIN sql_memory_meta m ON v.rowid = m.id
                ORDER BY vec_distance_cosine(v.embedding, @queryVec)
                LIMIT @topK;
            ";
            cmd.Parameters.AddWithValue("@queryVec", vectorBlob);
            cmd.Parameters.AddWithValue("@topK", topK);

            var results = new List<SearchResult>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new SearchResult
                {
                    Name = reader.GetString(0),
                    Module = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Query = reader.GetString(2),
                    Score = reader.GetDouble(3)
                });
            }
            return results;
        }

        /// <summary>
        /// Delete an entry by its name/intent.
        /// </summary>
        public void DeleteByName(string name)
        {
            var id = GetIdByName(name);
            if (id == null) return;

            var cmd = _db.CreateCommand();
            cmd.CommandText = "DELETE FROM sql_memory_meta WHERE id = @id; " +
                              "DELETE FROM sql_memory_vec WHERE rowid = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public int GetCount()
        {
            var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sql_memory_meta;";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // ─────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────

        private void InitializeSchema()
        {
            if (_initialized) return;

            var dims = _embeddings.Dimensions;

            // Metadata table — stores name, module, query, hash
            _db.CreateCommand().CommandText = @"
                CREATE TABLE IF NOT EXISTS sql_memory_meta (
                    id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    name        TEXT NOT NULL UNIQUE,
                    module      TEXT,
                    query       TEXT NOT NULL,
                    content_hash TEXT NOT NULL,
                    provider    TEXT,
                    created_at  DATETIME DEFAULT CURRENT_TIMESTAMP,
                    updated_at  DATETIME DEFAULT CURRENT_TIMESTAMP
                );
            ";
            _db.CreateCommand().ExecuteNonQuery();

            using (var cmd = _db.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS sql_memory_meta (
                        id           INTEGER PRIMARY KEY AUTOINCREMENT,
                        name         TEXT NOT NULL UNIQUE,
                        module       TEXT,
                        query        TEXT NOT NULL,
                        content_hash TEXT NOT NULL,
                        provider     TEXT,
                        created_at   DATETIME DEFAULT CURRENT_TIMESTAMP,
                        updated_at   DATETIME DEFAULT CURRENT_TIMESTAMP
                    );
                ";
                cmd.ExecuteNonQuery();
            }

            // Vector table — rowid matches sql_memory_meta.id
            using (var cmd = _db.CreateCommand())
            {
                cmd.CommandText = $@"
                    CREATE VIRTUAL TABLE IF NOT EXISTS sql_memory_vec
                    USING vec0(embedding FLOAT[{dims}]);
                ";
                cmd.ExecuteNonQuery();
            }

            _initialized = true;
        }

        private async Task UpsertAsync(SqlMemoryEntry entry, float[] embedding, string hash)
        {
            var existingId = GetIdByName(entry.Name);

            if (existingId.HasValue)
            {
                // Update metadata
                using var cmd = _db.CreateCommand();
                cmd.CommandText = @"
                    UPDATE sql_memory_meta 
                    SET module = @module, query = @query, 
                        content_hash = @hash, provider = @provider,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE id = @id;
                ";
                cmd.Parameters.AddWithValue("@module", entry.Module ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@query", entry.Query);
                cmd.Parameters.AddWithValue("@hash", hash);
                cmd.Parameters.AddWithValue("@provider", _embeddings.ProviderName);
                cmd.Parameters.AddWithValue("@id", existingId.Value);
                cmd.ExecuteNonQuery();

                // Update vector (delete + reinsert — sqlite-vec doesn't support UPDATE)
                using var vecCmd = _db.CreateCommand();
                vecCmd.CommandText = @"
                    DELETE FROM sql_memory_vec WHERE rowid = @id;
                    INSERT INTO sql_memory_vec(rowid, embedding) VALUES(@id, @vec);
                ";
                vecCmd.Parameters.AddWithValue("@id", existingId.Value);
                vecCmd.Parameters.AddWithValue("@vec", FloatsToBlob(embedding));
                vecCmd.ExecuteNonQuery();
            }
            else
            {
                // Insert metadata — get the new AUTOINCREMENT id
                long newId;
                using (var cmd = _db.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO sql_memory_meta (name, module, query, content_hash, provider)
                        VALUES (@name, @module, @query, @hash, @provider);
                        SELECT last_insert_rowid();
                    ";
                    cmd.Parameters.AddWithValue("@name", entry.Name);
                    cmd.Parameters.AddWithValue("@module", entry.Module ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@query", entry.Query);
                    cmd.Parameters.AddWithValue("@hash", hash);
                    cmd.Parameters.AddWithValue("@provider", _embeddings.ProviderName);
                    newId = (long)cmd.ExecuteScalar();
                }

                // Insert vector with matching rowid
                using var vecCmd = _db.CreateCommand();
                vecCmd.CommandText = @"
                    INSERT INTO sql_memory_vec(rowid, embedding) VALUES(@id, @vec);
                ";
                vecCmd.Parameters.AddWithValue("@id", newId);
                vecCmd.Parameters.AddWithValue("@vec", FloatsToBlob(embedding));
                vecCmd.ExecuteNonQuery();
            }
        }

        private string GetStoredHash(string name)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT content_hash FROM sql_memory_meta WHERE name = @name;";
            cmd.Parameters.AddWithValue("@name", name);
            var result = cmd.ExecuteScalar();
            return result as string;
        }

        private long? GetIdByName(string name)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT id FROM sql_memory_meta WHERE name = @name;";
            cmd.Parameters.AddWithValue("@name", name);
            var result = cmd.ExecuteScalar();
            return result == null ? null : (long?)Convert.ToInt64(result);
        }

        /// <summary>
        /// Hash of name + module + query — used to detect changes.
        /// If any field changes, the hash changes and re-embedding is triggered.
        /// </summary>
        private static string ComputeHash(SqlMemoryEntry entry)
        {
            var raw = $"{entry.Name}|{entry.Module}|{entry.Query}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes)[..16]; // first 16 chars is enough
        }

        /// <summary>
        /// Serialize float[] to raw bytes for sqlite-vec blob storage.
        /// sqlite-vec expects IEEE 754 little-endian float32 binary.
        /// </summary>
        private static byte[] FloatsToBlob(float[] floats)
        {
            var bytes = new byte[floats.Length * sizeof(float)];
            Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private void LoadSqliteVecExtension()
        {
            Batteries_V2.Init();
            _db.EnableExtensions(true);

            var vecPath = GetSqliteVecModulePath();
            if (!File.Exists(vecPath))
            {
                throw new FileNotFoundException($"SQLite vec0 extension not found: {vecPath}");
            }

            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT load_extension(@path);";
            cmd.Parameters.AddWithValue("@path", vecPath);
            cmd.ExecuteNonQuery();
        }

        private static string GetSqliteVecModulePath()
        {
            var baseDir = AppContext.BaseDirectory;
            var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win-x64"
                    : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux-x64"
                    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx-x64"
                    : throw new PlatformNotSupportedException("Unsupported OS for sqlite-vec module.");

            var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "vec0.dll"
                : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    ? "vec0.so"
                    : "vec0.dylib";

            return Path.Combine(baseDir, "runtimes", os, "native", fileName);
        }

        public void Dispose()
        {
            _db?.Close();
            _db?.Dispose();
        }
    }
}