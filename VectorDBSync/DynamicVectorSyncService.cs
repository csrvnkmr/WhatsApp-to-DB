using ChromaDB.Client;
using Dapper;
using Microsoft.Data.SqlClient;
using OpenAI;
using OpenAI.Embeddings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VectorDBSync
{
    public class DynamicVectorSyncService : ISyncService
    {
        private readonly ChromaConfigurationOptions _config;
        private readonly HttpClient _httpClient;
        private readonly ChromaClient _adminClient;
        private readonly EmbeddingClient _embeddingClient;
        private readonly string connectionString;

        private const string UpdateVectorSyncMetadataSql = @"
            IF EXISTS (SELECT 1 FROM Vector_SyncTracker WHERE CollectionName = @name)
                UPDATE Vector_SyncTracker SET LastSyncTime = @now WHERE CollectionName = @name
            ELSE
                INSERT INTO Vector_SyncTracker (CollectionName, LastSyncTime) VALUES (@name, @now)";

        public DynamicVectorSyncService(string apiKey, string chromaUrl, string connectionString)
        {

            string v1Path = chromaUrl;

            _config = new ChromaConfigurationOptions(v1Path);
            _httpClient = new HttpClient();
            _adminClient = new ChromaClient(_config, _httpClient);
            _embeddingClient = new OpenAIClient(apiKey).GetEmbeddingClient("text-embedding-3-small");
            this.connectionString = connectionString;
        }        

        public async Task SyncAllCollections(List<VectorSyncConfig> configs)
        {
            using IDbConnection db = new Microsoft.Data.SqlClient.SqlConnection(connectionString);

            foreach (var config in configs)
            {
                Console.WriteLine($"Syncing {config.CollectionName} starts");
                // 1. Fetch data dynamically
                // Note: You'd pass the actual last sync date here from your tracker table
                var results = await db.QueryAsync<dynamic>(config.SyncSql);
                Console.WriteLine($"Syncing {config.CollectionName} - {results.Count()} records to sync. Preparing vector data.." );
                var recordsToSync = new List<VectorRecord>();
                var count = 0;
                foreach (var item in results)
                {
                    if (count%100==0 && count>0)                     
                    {
                        Console.WriteLine($"Syncing {config.CollectionName} - Processed {count} records..");
                    }
                    count++;
                    var row = (IDictionary<string, object>)item;

                    var record = new VectorRecord
                    {
                        Id = row["Id"].ToString(),
                        Content = row["Content"].ToString(),
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
                    // 3. Sync to Chroma
                    //await SyncToCollection(config.CollectionName, recordsToSync);
                    await SyncToCollectionByBatch(config.CollectionName, recordsToSync);
                    var newHighWaterMark = DateTime.Now;
                    Console.WriteLine($"Completed Syncing {config.CollectionName} -  updating Highwatermark to {newHighWaterMark.ToString("yyyy-MM-dd HH:mm:ss")}");
                    await db.ExecuteAsync(UpdateVectorSyncMetadataSql,
                        new { name = config.CollectionName, now = newHighWaterMark });
                    // 4. Update Tracker table
                    //foreach (var rec in recordsToSync)
                    //{
                    //    await db.ExecuteAsync(config.UpdateTrackerSql, new { primarykey = rec.Id });
                    //}
                }
            }
        }

        // 1. Refactored to handle multiple strings at once
        private async Task<List<ReadOnlyMemory<float>>> GetVectors(List<string> texts)
        {
            // OpenAI supports up to 2048 inputs per request. 
            // This is significantly faster than calling singular GenerateEmbeddingAsync in a loop.
            var result = await _embeddingClient.GenerateEmbeddingsAsync(texts);

            // Convert the result collection to a list of ReadOnlyMemory<float>
            return result.Value.Select(r => r.ToFloats()).ToList();
        }

        public async Task SyncToCollectionByBatch(string collectionName, List<VectorRecord> records)
        {
            var collection = await _adminClient.GetOrCreateCollection(collectionName);
            var collectionClient = new ChromaCollectionClient(collection, _config, _httpClient);

            int batchSize = 100; // Optimal for 17k records to avoid timeout and rate limits
            int totalProcessed = 0;

            for (int i = 0; i < records.Count; i += batchSize)
            {
                // Get the current window of records
                var currentBatch = records.Skip(i).Take(batchSize).ToList();

                var ids = currentBatch.Select(r => r.Id).ToList();
                var documents = currentBatch.Select(r => r.Content).ToList();

                Console.WriteLine($"[Sync] Generating embeddings for batch {i / batchSize + 1} ({documents.Count} records)...");

                // 2. Bulk fetch embeddings for the entire batch
                var embeddings = await GetVectors(documents);

                // Filter metadata
                var metadatas = currentBatch.Select(r =>
                    r.Metadata.Where(m => m.Value != null)
                              .ToDictionary(m => m.Key, m => m.Value)
                ).ToList();

                // 3. Perform the Bulk Add to Chroma
                await collectionClient.Add(
                    ids: ids,
                    metadatas: metadatas,
                    documents: documents,
                    embeddings: embeddings
                );

                totalProcessed += currentBatch.Count;
                Console.WriteLine($"[Sync] Successfully pushed {totalProcessed}/{records.Count} to Chroma.");

                // Optional: Small delay to respect OpenAI Rate Limits (TPM) if needed
                // await Task.Delay(200); 
            }
        }

        private async Task<ReadOnlyMemory<float>> GetVector(string text)
        {
            var result = await _embeddingClient.GenerateEmbeddingAsync(text);
            return result.Value.ToFloats();
        }

        public async Task SyncToCollection(string collectionName, List<VectorRecord> records)
        {
            // 1. Get the collection reference from the admin client
            var collection = await _adminClient.GetOrCreateCollection(collectionName);

            // 2. Create the specialized client for this specific collection
            var collectionClient = new ChromaCollectionClient(collection, _config, _httpClient);

            // 3. Prepare data for the Add/Upsert call
            var ids = records.Select(r => r.Id).ToList();
            var documents = records.Select(r => r.Content).ToList();
            var embeddings = new List<ReadOnlyMemory<float>>();
            foreach (var doc in documents)
            {
                embeddings.Add(await GetVector(doc));
            }
            // Filter out nulls from metadata to prevent API errors
            var metadatas = records.Select(r =>
                r.Metadata.Where(m => m.Value != null)
                          .ToDictionary(m => m.Key, m => m.Value)
            ).ToList();

            await collectionClient.Add(
                ids: ids,
                metadatas: metadatas,
                documents: documents,
                embeddings: embeddings // Pass pre-calculated embeddings here
            );
        }

        public async Task<List<SearchResult>> SearchCollection(
                string collectionName,
                string queryText,
                int limit = 5,
                IDictionary<string, object>? filter = null)
        {
            var collection = await _adminClient.GetOrCreateCollection(collectionName);
            var collectionClient = new ChromaCollectionClient(collection, _config, _httpClient);

            var queryVector = await GetVector(queryText);
            var queryEmbeddings = new List<ReadOnlyMemory<float>> { queryVector };

            // Build the ChromaWhereOperator using overloaded operators
            ChromaWhereOperator? whereFilter = null;

            if (filter != null && filter.Any())
            {
                foreach (var kvp in filter)
                {
                    var currentFilter = ChromaWhereOperator.Equal(kvp.Key, kvp.Value);

                    if (whereFilter == null)
                    {
                        whereFilter = currentFilter;
                    }
                    else
                    {
                        whereFilter &= currentFilter;
                    }
                }
            }

            var results = await collectionClient.Query(
                queryEmbeddings: queryEmbeddings,
                nResults: limit,
                where: whereFilter, 
                include: ChromaQueryInclude.Metadatas | ChromaQueryInclude.Distances | ChromaQueryInclude.Documents
            );

            return results.FirstOrDefault()?.Select(entry => new SearchResult
            {
                Id = entry.Id,
                Document = entry.Document,
                Distance = entry.Distance,
                Metadata = entry.Metadata
            }).ToList() ?? new List<SearchResult>();
        }      

    }
    public class VectorSyncRoot
    {
        public List<VectorSyncConfig> SyncCollections { get; set; } = new();
    }

    public class VectorSyncConfig
    {
        public string CollectionName { get; set; }
        public string SyncSql { get; set; }
        public string UpdateTrackerSql { get; set; }
        public List<string> MetadataFields { get; set; }
    }

    public class SearchResult
    {
        public string Id { get; set; }
        public string Document { get; set; }
        public float? Distance { get; set; } // Lower is better (more similar)
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class VectorRecord
    {
        public string Id { get; set; }        // SAP ItemCode or CardCode
        public string Content { get; set; }   // Text for embedding
        public Dictionary<string, object> Metadata { get; set; }
    }
}
