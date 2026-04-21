using ChromaDB.Client;
using Dapper;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Embeddings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDBSync
{
    public class VectorSyncService : ISyncService
    {
        private readonly string connectionString;
        private  IVectorDBService _vectorDBService;

        private const string UpdateVectorSyncMetadataSql = @"
            IF EXISTS (SELECT 1 FROM Vector_SyncTracker WHERE CollectionName = @name)
                UPDATE Vector_SyncTracker SET LastSyncTime = @now WHERE CollectionName = @name
            ELSE
                INSERT INTO Vector_SyncTracker (CollectionName, LastSyncTime) VALUES (@name, @now)";

        private void SetVectorDBService(string dataFolder="Data")
        {
            _vectorDBService = new SQLiteVectorDBService(new LocalEmbeddingService(), dataFolder); 
        }

        public VectorSyncService(IConfiguration config)
        {
            SetVectorDBService();
            this.connectionString = config["DatabaseSettings:ConnectionString"];             
        }

        public VectorSyncService(string connectionString)
        {
            SetVectorDBService("C:\\Development\\AiAgents\\git\\WhatsAppToDB\\VectorDBSync\\bin\\Debug\\net9.0\\Data\\");
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

                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [Sync] Generating embeddings for batch {i / batchSize + 1} ({documents.Count} records)...");

                // 2. Bulk fetch embeddings for the entire batch

                // Filter metadata
                var metadatas = currentBatch.Select(r =>
                    r.Metadata.Where(m => m.Value != null)
                              .ToDictionary(m => m.Key, m => m.Value)
                ).ToList();

                // 3. Perform the Bulk Add to Chroma
                await _vectorDBService.Add(
                    collectionName,
                    ids,
                    documents,
                    metadatas
                );

                totalProcessed += currentBatch.Count;
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [Sync] Successfully pushed {totalProcessed}/{records.Count} to Chroma.");
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
