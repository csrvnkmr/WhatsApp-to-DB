using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace VectorDBSync
{
    internal sealed class SQLiteCacheService
    {
        public SqliteConnection Open(string dbPath)
        {
            var directory = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(directory))            
            {
                Directory.CreateDirectory(directory);
            }

            var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            return conn;
        }

        public string ComputeHash(string content)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content ?? ""));
            return Convert.ToHexString(bytes)[..16];
        }

        /// <summary>
        /// Each collection gets its own cache table: cache_{CollectionName}.
        /// Stores only record_key and content_hash.
        /// </summary>
        public void EnsureSqliteCacheTable(SqliteConnection conn, string collectionName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
                CREATE TABLE IF NOT EXISTS cache_{collectionName} (
                    record_key    TEXT PRIMARY KEY,
                    content_hash  TEXT NOT NULL,
                    vector_id     TEXT,
                    updated_at    DATETIME DEFAULT CURRENT_TIMESTAMP
                );";
            cmd.ExecuteNonQuery();

            EnsureVectorIdColumn(conn, collectionName);
        }

        private static void EnsureVectorIdColumn(SqliteConnection conn, string collectionName)
        {
            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = $"PRAGMA table_info(cache_{collectionName});";
            using var reader = checkCmd.ExecuteReader();
            var hasTable = false;
            var hasVectorId = false;
            while (reader.Read())
            {
                hasTable = true;
                var columnName = reader.GetString(1);
                if (string.Equals(columnName, "vector_id", StringComparison.OrdinalIgnoreCase))
                {
                    hasVectorId = true;
                    break;
                }
            }

            if (hasTable && !hasVectorId)
            {
                using var alterCmd = conn.CreateCommand();
                alterCmd.CommandText = $"ALTER TABLE cache_{collectionName} ADD COLUMN vector_id TEXT;";
                alterCmd.ExecuteNonQuery();
            }
        }

        public void DropSqliteCache(SqliteConnection conn, string collectionName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DROP TABLE IF EXISTS cache_{collectionName};";
            cmd.ExecuteNonQuery();
        }

        public Dictionary<string, string> LoadCachedHashes(SqliteConnection conn, string collectionName)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT record_key, content_hash FROM cache_{collectionName};";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result[reader.GetString(0)] = reader.GetString(1);
            }

            return result;
        }

        public Dictionary<string, string> LoadCachedVectorIds(SqliteConnection conn, string collectionName)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT record_key, vector_id FROM cache_{collectionName};";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (reader.IsDBNull(1))
                {
                    continue;
                }

                var vectorId = reader.GetString(1);
                if (!string.IsNullOrWhiteSpace(vectorId))
                {
                    result[reader.GetString(0)] = vectorId;
                }
            }

            return result;
        }

        public string GetOrCreateVectorId(SqliteConnection conn, string collectionName, string recordKey)
        {
            using var getCmd = conn.CreateCommand();
            getCmd.CommandText = $"SELECT vector_id FROM cache_{collectionName} WHERE record_key=@key;";
            getCmd.Parameters.AddWithValue("@key", recordKey);

            var existing = getCmd.ExecuteScalar()?.ToString();
            if (!string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }

            var vectorId = !string.IsNullOrWhiteSpace(recordKey)
                ? recordKey
                : Guid.NewGuid().ToString();
            using var upsertCmd = conn.CreateCommand();
            upsertCmd.CommandText = $@"
                INSERT INTO cache_{collectionName}(record_key, content_hash, vector_id, updated_at)
                VALUES(@key, '', @vectorId, CURRENT_TIMESTAMP)
                ON CONFLICT(record_key) DO UPDATE
                SET vector_id = excluded.vector_id,
                    updated_at = CURRENT_TIMESTAMP;";
            upsertCmd.Parameters.AddWithValue("@key", recordKey);
            upsertCmd.Parameters.AddWithValue("@vectorId", vectorId);
            upsertCmd.ExecuteNonQuery();
            return vectorId;
        }

        public static bool IsValidVectorId(string? value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        public void UpsertSqliteCache(SqliteConnection conn, string collectionName, List<VectorRecord> records)
        {
            using var tx = conn.BeginTransaction();
            foreach (var record in records)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = $@"
                    INSERT INTO cache_{collectionName}(record_key, content_hash, vector_id, updated_at)
                    VALUES(@key, @hash, @vectorId, CURRENT_TIMESTAMP)
                    ON CONFLICT(record_key) DO UPDATE
                    SET content_hash = excluded.content_hash,
                        vector_id = excluded.vector_id,
                        updated_at   = CURRENT_TIMESTAMP;";
                cmd.Parameters.AddWithValue("@key", record.Id);
                cmd.Parameters.AddWithValue("@hash", ComputeHash(record.Content));
                cmd.Parameters.AddWithValue("@vectorId", string.IsNullOrWhiteSpace(record.VectorId) ? Guid.NewGuid().ToString() : record.VectorId);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }

        public void DeleteFromSqliteCache(SqliteConnection conn, string collectionName, List<string> keys)
        {
            using var tx = conn.BeginTransaction();
            foreach (var key in keys)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = $"DELETE FROM cache_{collectionName} WHERE record_key=@key;";
                cmd.Parameters.AddWithValue("@key", key);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }

    }
}
