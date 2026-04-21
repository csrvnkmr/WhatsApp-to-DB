using ChromaDB.Client;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Embeddings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDBSync
{
    internal class ChromaDBService : IVectorDBService
    {
        private readonly ChromaConfigurationOptions _config;
        private readonly HttpClient _httpClient;
        private readonly ChromaClient _adminClient;
        private readonly IEmbeddingService _embeddingService;

        public ChromaDBService(IConfiguration config)
        {            
            var apiKey = config["OpenAiSettings:ApiKey"];
            var chromaUrl = config["DatabaseSettings:ChromaUrl"];
            var connectionString = config["DatabaseSettings:ConnectionString"];
            string v1Path = chromaUrl;

            _config = new ChromaConfigurationOptions(v1Path);
            _httpClient = new HttpClient();
            _adminClient = new ChromaClient(_config, _httpClient);
            //_embeddingClient = new OpenAIClient(apiKey).GetEmbeddingClient("text-embedding-3-small");
            _embeddingService = new OpenAiEmbeddingService("text-embedding-3-small", apiKey);
            //_embeddingService = new LocalEmbeddingService();
        }

        public async Task Add(string collectionName,List<string> ids, List<string>? documents, 
            List<Dictionary<string, object>>? metadatas)
        {
            var collection = await _adminClient.GetOrCreateCollection(collectionName);
            var collectionClient = new ChromaCollectionClient(collection, _config, _httpClient);
            var embeddings = await GetVectors(documents);
            await collectionClient.Add(
                    ids: ids,
                    metadatas: metadatas,
                    documents: documents,
                    embeddings: embeddings
                );
        }

        public async Task<List<ReadOnlyMemory<float>>> GetVectors(List<string> texts)
        {
            var result = await _embeddingService.GetVectors(texts);
            return result;

        }

        public async Task<List<SearchResult>> SearchCollection(
                string collectionName,
                string queryText,
                int limit = 5,
                IDictionary<string, object>? filter = null)
        {
            var collection = await _adminClient.GetOrCreateCollection(collectionName);
            var collectionClient = new ChromaCollectionClient(collection, _config, _httpClient);

            var queryVector = await _embeddingService.GetVector(queryText);
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
}
