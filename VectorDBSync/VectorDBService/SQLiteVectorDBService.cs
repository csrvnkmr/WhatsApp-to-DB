using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VectorDBSync.EmbeddingService;

namespace VectorDBSync.VectorDBService
{
    internal class SQLiteVectorDBService : IVectorDBService
    {
        private IEmbeddingService _embeddingService;
        private readonly string _basePath;


        public SQLiteVectorDBService(Settings settings)
        {
            _embeddingService = EmbeddingServiceFactory.Create(settings);
            _basePath = settings.SqliteSettings.Folder;
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }
        
        private string GetCollectionPath(string collectionName) => Path.Combine(_basePath, $"{collectionName}.db");

        public Task Delete(string collectionName)
        {
            string dbPath = GetCollectionPath(collectionName);
            if (File.Exists(dbPath))
                File.Delete(dbPath);
            return Task.CompletedTask;
        }

        public async Task Add(string collectionName, List<string> ids, List<string>? documents,
            List<Dictionary<string, object>>? metadatas)
        {
            var batchsize = 100;
            for (int outer=0; outer < ids.Count; outer += batchsize)
            {
                var currentIds = ids.Skip(outer).Take(batchsize).ToList();
                var currentDocs = documents?.Skip(outer).Take(batchsize).ToList();
                var currentMetas = metadatas?.Skip(outer).Take(batchsize).ToList();
            

                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [SQLiteVectorDBService] Generating embeddings for {currentIds?.Count ?? 0} records...");
                var vectors = await GetVectors(currentDocs ?? new List<string>());
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [SQLiteVectorDBService] starting to insert {currentIds?.Count ?? 0} records.");
                string dbPath = GetCollectionPath(collectionName);
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                connection.Open();

                // Initialize the tables if they don't exist
                if (outer == 0) InitializeSchema(connection);

                using var transaction = connection.BeginTransaction();
                try
                {
                    var cmd = connection.CreateCommand();
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"
                    INSERT INTO collection_data (id, document, metadata, vector) 
                    VALUES ($id, $doc, $meta, $vec)
                    ON CONFLICT(id) DO UPDATE SET 
                        document = excluded.document, 
                        metadata = excluded.metadata, 
                        vector = excluded.vector;";
                    for (int i = 0; i < currentIds.Count; i++)
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("$id", currentIds[i]);
                        cmd.Parameters.AddWithValue("$doc", (object)currentDocs?[i] ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("$meta", currentMetas != null ? JsonSerializer.Serialize(currentMetas[i]) : DBNull.Value);

                        // Convert ReadOnlyMemory<float> to byte array for BLOB storage
                        byte[] vectorBytes = new byte[vectors[i].Length * sizeof(float)];
                        Buffer.BlockCopy(vectors[i].ToArray(), 0, vectorBytes, 0, vectorBytes.Length);
                        cmd.Parameters.AddWithValue("$vec", vectorBytes);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    transaction.Commit();
                }

                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private void InitializeSchema(SqliteConnection connection)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS collection_data (
                    id TEXT PRIMARY KEY,
                    document TEXT,
                    metadata TEXT,
                    vector BLOB
                );
                CREATE VIRTUAL TABLE IF NOT EXISTS collection_fts 
                    USING fts5(id, document, content='collection_data', content_rowid='rowid');

                -- Trigger to keep FTS in sync with the main data table
                CREATE TRIGGER IF NOT EXISTS fts_sync_insert AFTER INSERT ON collection_data BEGIN
                  INSERT INTO collection_fts(id, document) VALUES (new.id, new.document);
                END;

                -- Optimization for large datasets (100k rows)
                PRAGMA journal_mode = WAL;
                PRAGMA synchronous = NORMAL;";
            cmd.ExecuteNonQuery();
        }

        public async Task<List<ReadOnlyMemory<float>>> GetVectors(List<string> texts)
        {
            var vectors = await _embeddingService.GetVectors(texts);
            return vectors;            
        }


        private async Task<List<SearchResult>> GetFtsResults(string collectionName, string queryText, int limit,
            IDictionary<string, object>? filter = null)
        {
            try
            {

            
            string dbPath = Path.Combine(_basePath, $"{collectionName}.db");
            var results = new List<SearchResult>();

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();

            // 1. Handle Metadata Filters
            string filterClause = "";
            if (filter != null && filter.Any())
            {
                var clauses = new List<string>();
                foreach (var kvp in filter)
                {
                    string paramName = $"$f_{kvp.Key}";
                    // Filtering on the joined 'd' (collection_data) table
                    clauses.Add($"json_extract(d.metadata, '$.{kvp.Key}') = {paramName}");
                    cmd.Parameters.AddWithValue(paramName, kvp.Value.ToString());
                }
                filterClause = " AND " + string.Join(" AND ", clauses);
            }

            // 2. The SQL Query
            // We filter by both the FTS MATCH and the JSON metadata
            cmd.CommandText = $@"
                SELECT f.id, f.document, d.metadata, bm25(collection_fts) as rank
                FROM collection_fts f
                JOIN collection_data d ON f.id = d.id
                WHERE f.document MATCH $query {filterClause}
                ORDER BY rank
                LIMIT $limit";

            //var sanitizedQuery = SanitizeFtsQuery(queryText);
            var sanitized = Regex.Replace(queryText, @"[^\w\s]", " "); // Removes all punctuation

            cmd.Parameters.AddWithValue("$query", sanitized);
            cmd.Parameters.AddWithValue("$limit", limit);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var metadataJson = reader.IsDBNull(2) ? null : reader.GetString(2);

                results.Add(new SearchResult
                {
                    Id = reader.GetString(0),
                    Document = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Distance = (float)Math.Max(0, Math.Min(1, (reader.GetDouble(3) + 10) / 20)),
                    Metadata = metadataJson != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson) : null
                });
            }

            return results;
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error in GetFtsResults: {ex}");
                return new List<SearchResult>();
            }
        }

        public static string SanitizeFtsQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return string.Empty;

            // Remove common FTS special characters that cause syntax errors
            char[] specialChars = { ',', ':', '"', '*', '(', ')', '+', '-' };
            foreach (var c in specialChars)
            {
                query = query.Replace(c.ToString(), " ");
            }

            // Optional: Join words with "OR" or "AND" if you want broader/stricter matching
            // For a fuzzy search feel, just keeping the cleaned string is usually best.
            return query.Trim();
        }

        public async Task<List<SearchResult>> SearchCollection(string collectionName, string queryText, int limit = 5,            
                IDictionary<string, object>? filter = null)
        {

            // 1. Get Vector Matches (Semantic)
            var vectorMatches = await GetVectorResults(collectionName, queryText, 20, filter);

            // 2. Get FTS5 Matches (Typo/Literal)
            var ftsMatches = await GetFtsResults(collectionName, queryText, 20);

            // 3. Simple Re-ranking Logic
            // If a result is in BOTH lists, boost it significantly.
            // If "Linda Mitchel" is an FTS match, it will jump to #1 even if Smith is a closer vector.
            var combined = vectorMatches.Union(ftsMatches)
                .GroupBy(x => x.Id)
                .Select(g => new SearchResult
                {
                    Id = g.Key,
                    Document = g.First().Document,
                    // Boost score if found in both
                    Distance = g.Count() > 1 ? g.Min(x => x.Distance) * 0.5f : g.Min(x => x.Distance),
                    Metadata = g.First().Metadata
                })
                .OrderBy(x => x.Distance)
                .Take(limit)
                .ToList();

            return combined;
        }

        public async Task<List<SearchResult>> GetVectorResults(
            string collectionName,
            string queryText,
            int limit = 5,
            IDictionary<string, object>? filter = null)
        {
            string dbPath = Path.Combine(_basePath, $"{collectionName}.db");
            if (!File.Exists(dbPath)) return new List<SearchResult>();

            // 1. Get the vector for the query text
            var queryVector = await _embeddingService.GetVector(queryText);
            var querySpan = queryVector.Span;

            var allResults = new List<SearchResult>();

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            // 2. Fetch records (Apply filters if they exist)
            var cmd = connection.CreateCommand();
            string whereClause = BuildWhereClause(filter, cmd);
            cmd.CommandText = $"SELECT id, document, metadata, vector FROM collection_data {whereClause}";
            
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetString(0);
                var document = reader.IsDBNull(1) ? null : reader.GetString(1);
                var metadataJson = reader.IsDBNull(2) ? null : reader.GetString(2);

                // Read BLOB back into float array
                byte[] blob = (byte[])reader["vector"];
                float[] storedVector = new float[blob.Length / sizeof(float)];
                Buffer.BlockCopy(blob, 0, storedVector, 0, blob.Length);

                // 3. Calculate Cosine Similarity
                //double similarity = CalculateCosineSimilarity(queryVector.Span, storedVector);
                double similarity = CalculateCosineSimilarity(queryVector, storedVector);
                allResults.Add(new SearchResult
                {
                    Id = id,
                    Document = document,
                    Distance = (float)(1.0 - similarity), // Convert similarity to "distance" to match ChromaDB style
                    Metadata = metadataJson != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson) : null
                });
            }

            // 4. Sort by distance (lowest first) and take the limit
            return allResults.OrderBy(r => r.Distance).Take(limit).ToList();
        }

        private double CalculateCosineSimilarity(ReadOnlyMemory<float> vecA1, float[] vecB)
        {
            double dotProduct = 0, magnitudeA = 0, magnitudeB = 0;
            var vecA = vecA1.Span;
            for (int i = 0; i < vecA.Length; i++)
            {
                dotProduct += vecA[i] * vecB[i];
                magnitudeA += vecA[i] * vecA[i];
                magnitudeB += vecB[i] * vecB[i];
            }
            return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        }

        private string BuildWhereClause(IDictionary<string, object>? filter, SqliteCommand cmd)
        {
            if (filter == null || !filter.Any()) return "";

            var clauses = new List<string>();
            foreach (var kvp in filter)
            {
                // Simple equality filter to match your Chroma code logic
                string paramName = $"$p_{kvp.Key}";
                clauses.Add($"json_extract(metadata, '$.{kvp.Key}') = {paramName}");
                cmd.Parameters.AddWithValue(paramName, kvp.Value.ToString());
            }
            return "WHERE " + string.Join(" AND ", clauses);
        }

     
    }
}