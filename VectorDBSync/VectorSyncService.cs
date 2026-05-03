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
using VectorDBSync.EmbeddingService;
using VectorDBSync.VectorDBService;

namespace VectorDBSync
{
    public class VectorSyncService : ISyncService
    {

        public static ISyncService LoadSyncServiceFrom(string settingsFile)
        {
            var settings = Settings.LoadFromFile(settingsFile);
            ISyncService vss = new VectorSyncService(settings);
            return vss;
        }

        private readonly string connectionString;
        private  IVectorDBService _vectorDBService;

        private const string UpdateVectorSyncMetadataSql = @"
            IF EXISTS (SELECT 1 FROM Vector_SyncTracker WHERE CollectionName = @name)
                UPDATE Vector_SyncTracker SET LastSyncTime = @now WHERE CollectionName = @name
            ELSE
                INSERT INTO Vector_SyncTracker (CollectionName, LastSyncTime) VALUES (@name, @now)";

        public VectorSyncService(Settings settings)
        {
            _vectorDBService = VectorDBServiceFactory.CreateVectorDBService(settings);
            this.connectionString = settings.DatabaseSettings.ConnectionString ?? "";
        }

        public async Task SyncAllCollections(List<VectorSyncConfig> configs)
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
