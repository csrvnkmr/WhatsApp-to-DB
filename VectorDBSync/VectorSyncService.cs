using ChromaDB.Client;
using Dapper;
using OpenAI;
using OpenAI.Embeddings;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Threading.Tasks;

namespace VectorDBSync
{
    public class ChromaSyncService
    {
        private readonly ChromaConfigurationOptions _config;
        private readonly HttpClient _httpClient;
        private readonly ChromaClient _adminClient;
        private readonly EmbeddingClient _embeddingClient;
        private readonly string connectionString;
        private const string chromabaseurl = "http://localhost:8000";        

        private const string ItemCollectionName = "item_master";
        private const string BpCollectionName = "bp_master";

        public ChromaSyncService(string apiKey, string chromaUrl, string connectionString)
        {
            // Point the base URL to the specific database path
            // Note: Some libraries append a slash, so we trim it to be safe
            //string v2DatabasePath = "http://localhost:8000/api/v2/tenants/default_tenant/databases/default_database";

            string v1Path = chromaUrl;
            //"http://localhost:8000/api/v1/";

            _config = new ChromaConfigurationOptions(v1Path);
            _httpClient = new HttpClient();
            _adminClient = new ChromaClient(_config, _httpClient);
            _embeddingClient = new OpenAIClient(apiKey).GetEmbeddingClient("text-embedding-3-small");
            this.connectionString = connectionString;
        }

        private VectorRecord MapBPToVector(OCRDRecord bpRecord)
        {
            return new VectorRecord
            {
                Id = bpRecord.CardCode,
                Content = bpRecord.CardName,
                Metadata = new Dictionary<string, object>
                        {
                            { "Type", bpRecord.CardType=="C"?"Customer":"Supplier" }
                        }
            };
        }

        private VectorRecord MapItemToVector (OITMRecord itemRecord)
        {
            return new VectorRecord
            {
                Id = itemRecord.ItemCode,
                Content = itemRecord.ItemName,
                Metadata = new Dictionary<string, object>
                        {
                            { "Type", "Item" }
                        }
            };
        }

      

        public async Task SyncDataWithDB<T>(string sql, Func<T, VectorRecord> getVectorRecord,
            Func<List<VectorRecord>, Task> syncMethod)
        {
            IDbConnection dbConnection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            var results = await dbConnection.QueryAsync<T>(sql);
            if (results.Any())
            {
                var lstRecords = results.Select(r => getVectorRecord(r)).ToList();
                await syncMethod(lstRecords);
            }
        }

        public async Task SyncBusinessPartnerWithDB()
        {
            var sql = Sqls.GetBusinessPartnersToSync; //@"Select top 100 ""CardCode"", ""CardName"", ""CardType"" from ocrd a ";
            await SyncDataWithDB<OCRDRecord>(sql, MapBPToVector, SyncBusinessPartners);
            //await SyncItems(lstRecords);                        
        }

        public async Task SyncItemWithDB()
        {
            var sql = Sqls.GetItemsToSync; // @"Select top 100 ""ItemCode"", ""ItemName"" from oitm a ";
            await SyncDataWithDB<OITMRecord>(sql, MapItemToVector, SyncItems);
            //await SyncItems(lstRecords);                        
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

            // 4. Perform the Add
            // Note: If you have pre-calculated embeddings, pass them in the 'embeddings' parameter.
            // If Chroma is handling embeddings, pass documents.
            await collectionClient.Add(
                ids: ids,
                metadatas: metadatas,
                documents: documents,
                embeddings: embeddings // Pass pre-calculated embeddings here
            );
        }
        public async Task PeekBusinessPartners(int limit = 10)
        {
            await PeekCollection(BpCollectionName, limit);
        }

        public async Task PeekCollection(string collectionName, int limit = 10)
        {
            var collection = await _adminClient.GetOrCreateCollection(collectionName);
            var collectionClient = new ChromaCollectionClient(collection, _config, _httpClient);

            // .Get() retrieves data without needing a vector/embedding
            var data = await collectionClient.Get(
                limit: limit,
                include: ChromaGetInclude.Metadatas | ChromaGetInclude.Documents
            );

            if (data == null || !data.Any())
            {
                Console.WriteLine($"Collection '{collectionName}' is empty.");
                return;
            }

            foreach (var item in data)
            {
                Console.WriteLine($"ID: {item.Id}");
                Console.WriteLine($"Document: {item.Document}");
                Console.WriteLine("Metadata: " + string.Join(", ", item.Metadata.Select(m => $"{m.Key}: {m.Value}")));
                Console.WriteLine("-------------------------------");
            }
        }

        public async Task SyncItems(List<VectorRecord> items)
        {
            await SyncToCollection(ItemCollectionName, items);
            await UpsertSyncTable(Sqls.UpsertItemSyncSuccess, items);
        }

        public async Task UpsertSyncTable(string sql, List<VectorRecord> lst)
        {
                IDbConnection dbConnection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
    
                foreach (var record in lst)
                {
                    await dbConnection.ExecuteAsync(sql, new { primarykey = record.Id });
            }
        }

        public async Task SyncBusinessPartners(List<VectorRecord> bps)
        {
            await SyncToCollection(BpCollectionName, bps);
            await UpsertSyncTable(Sqls.UpsertBusinessPartnerSyncSuccess, bps);
        }

        public async Task<List<SearchResult>> SearchBusinessPartners(string queryText, int limit = 5)  {
            var results = await SearchCollection(BpCollectionName, queryText, limit);
            return results;
        }
        public async Task<List<SearchResult>> SearchItems(string queryText, int limit = 5)
        {
            var results = await SearchCollection(ItemCollectionName, queryText, limit);
            return results;
        }


        public async Task DeleteBPCollection() =>
            await DeleteCollection(BpCollectionName);
        public async Task DeleteItemCollection() =>
            await DeleteCollection(ItemCollectionName);


        public async Task DeleteCollection(string collectionName)
        {
            try
            {
                await _adminClient.DeleteCollection(collectionName);
            } 
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting collection '{collectionName}': {ex.Message}");
            }
            // Run this once to clear the "bad" data
        }

        public async Task<List<SearchResult>> SearchCollection(string collectionName, string queryText, int limit = 5)
        {
            var collection = await _adminClient.GetOrCreateCollection(collectionName);
            var collectionClient = new ChromaCollectionClient(collection, _config, _httpClient);

            var queryVector = await GetVector(queryText);

            var queryEmbeddings = new List<ReadOnlyMemory<float>> { queryVector };
            var results = await collectionClient.Query(
                queryEmbeddings: queryEmbeddings, // Pass the vector here
                nResults: limit,
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

    public class OITMRecord
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
    }

    public class OCRDRecord
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string CardType { get; set; }
    }

}