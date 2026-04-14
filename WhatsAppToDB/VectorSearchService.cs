using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.InMemory;

using Microsoft.SemanticKernel.Memory;
using System.Linq;

namespace WhatsAppToDB
{

    public class MasterDataRecord
    {
        [VectorStoreKey]
        public string Id { get; set; } // CardCode or ItemCode

        [VectorStoreData(IsFullTextIndexed = true)]
        public string Name { get; set; } // The name we fuzzy match against

        [VectorStoreVector(Dimensions: 1536)] // 1536 for text-embedding-3-small
        public ReadOnlyMemory<float> Embedding { get; set; }
    }


    public class VectorSearchService
    {
        private readonly VectorStoreCollection<string, MasterDataRecord> _collection;
        private readonly ITextEmbeddingGenerationService _embeddingService;

        string collection_name = "bp_data";
        public VectorSearchService(ITextEmbeddingGenerationService embeddingService)
        {
            // 1. Initialize the Store
            var vectorStore = new InMemoryVectorStore();

            // 2. Get the collection (Strongly Typed)
            _collection = vectorStore.GetCollection<string, MasterDataRecord>(collection_name);
            _collection.EnsureCollectionExistsAsync();
            _embeddingService = embeddingService;
        }

        public async Task UpsertEntityAsync(string id, string name)
        {
            // Generate the high-dimensional vector for the name
            var embedding = await _embeddingService.GenerateEmbeddingAsync(name);

            var record = new MasterDataRecord
            {
                Id = id,
                Name = name,
                Embedding = embedding
            };
            

            await _collection.UpsertAsync(record);
        }

        public async Task<string?> SearchAsync(string query)
        {
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

            // FIX: Use a Lambda Expression for the VectorProperty
            var searchOptions = new VectorSearchOptions<MasterDataRecord>
            {
                // This is a Func<MasterDataRecord, object> expression
                VectorProperty = record => record.Embedding
            };

            // The '1' represents 'Top' (the number of results)
            var searchResult = _collection.SearchAsync(queryEmbedding, 1, searchOptions);


            // Stream the results
            await foreach (var result in searchResult)
            {
                // Add a relevance check
                if (result.Score > 0.8)
                {
                    return result.Record.Id;
                }
            }

            return null;

            //var resultsList = await searchResult.ToListAsync();
            //return resultsList.FirstOrDefault()?.Record.Id;
        }

    }
}
