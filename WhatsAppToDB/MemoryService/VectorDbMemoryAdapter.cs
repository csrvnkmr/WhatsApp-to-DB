using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory.MemoryStorage; // Core interface location
using Microsoft.KernelMemory;
using WhatsAppToDB.Abstractions;
using Microsoft.KernelMemory.AI;

namespace VectorDBSync.VectorDBService
{
    public class VectorDbMemoryAdapter : IMemoryDb
    {
        private readonly IVectorDBService _vectorDbService;
        private readonly ITextEmbeddingGenerator _embeddingGenerator;

        public VectorDbMemoryAdapter(IVectorDBService vectorDbService, ITextEmbeddingGenerator embeddingGenerator)
        {
            _vectorDbService = vectorDbService ?? throw new ArgumentNullException(nameof(vectorDbService));
            _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
        }

        public Task CreateIndexAsync(string index, int vectorSize, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public async Task<string> UpsertAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(record);

            var metadataDictionary = new Dictionary<string, object>();
            foreach (var tag in record.Tags)
            {
                metadataDictionary[tag.Key] = tag.Value.FirstOrDefault() ?? string.Empty;
            }

            metadataDictionary["__document_id"] = record.Id;

            var ids = new List<string> { record.Id };
            //var vectors = new List<ReadOnlyMemory<float>> { record.Vector.AsReadOlyMemory() };
            var vectors = new List<ReadOnlyMemory<float>> { record.Vector.Data };
            
            string payloadText = record.Payload.FirstOrDefault().Value?.ToString() ?? string.Empty;
            var documents = new List<string> { payloadText };
            var metadatas = new List<Dictionary<string, object>> { metadataDictionary };

            await _vectorDbService.Add(index, ids, vectors, documents, metadatas);
            await _vectorDbService.AfterCollectionSyncCompletedAsync(index, cancellationToken);

            return record.Id;
        }

        /// <summary>
        /// Fixed: Explicitly returns IAsyncEnumerable<MemoryRecord> to resolve your second error.
        /// </summary>
        public async IAsyncEnumerable<MemoryRecord> GetListAsync(
            string index,
            ICollection<MemoryFilter>? filters = null,
            int limit = 1,
            bool withEmbeddings = false,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var executionFilter = FlattenFilters(filters);
            List<VectorSearchResult> customSearchResults = await _vectorDbService.SearchCollection(
                collectionName: index,
                queryVector: Enumerable.Empty<float>().ToArray(), 
                queryText: string.Empty,
                limit: limit,
                filter: executionFilter
            );

            foreach (var customResult in customSearchResults)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return MapToMemoryRecord(customResult, withEmbeddings);
            }
        }

        /// <summary>
        /// Fixed: Explicitly returns IAsyncEnumerable<(MemoryRecord, double)> to resolve CS0738.
        /// </summary>
        public async IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(
            string index,
            string text,
            ICollection<MemoryFilter>? filters = null,
            double minRelevance = 0,
            int limit = 1,
            bool withEmbeddings = false,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var executionFilter = FlattenFilters(filters);
            Embedding queryEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(text);
            List<VectorSearchResult> customSearchResults = await _vectorDbService.SearchCollection(
                collectionName: index,
                queryVector: queryEmbedding.Data,
                queryText: text,
                limit: limit,
                filter: executionFilter
            );

            foreach (var customResult in customSearchResults)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Your service returns Distance (0 = identical, 1 = distant)
                // Kernel Memory expects Relevance (1 = identical, 0 = distant)
                double relevance = (double)(1.0 - (customResult.Distance.HasValue ? customResult.Distance.Value : 0.0));

                if (relevance >= minRelevance)
                {
                    MemoryRecord record = MapToMemoryRecord(customResult, withEmbeddings);
                    
                    // Yield the expected Tuple layout value back out
                    yield return (record, relevance);
                }
            }
        }

        public async Task DeleteAsync(string index, string id, CancellationToken cancellationToken = default)
        {
            await _vectorDbService.Delete(index, id);
        }

        public async Task DeleteAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(record);
            await _vectorDbService.Delete(index, record.Id);
        }

        public async Task DeleteIndexAsync(string index, CancellationToken cancellationToken = default)
        {
            await _vectorDbService.Delete(index);
        }

        public async Task<IEnumerable<string>> GetIndexesAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(Enumerable.Empty<string>());
        }

        // --- PRIVATE HELPER METHODS ---

        private Dictionary<string, object> FlattenFilters(ICollection<MemoryFilter>? filters)
        {
            var flattened = new Dictionary<string, object>();
            if (filters != null)
            {
                foreach (var filterGroup in filters)
                {
                    foreach (var pair in filterGroup)
                    {
                        flattened[pair.Key] = pair.Value.FirstOrDefault() ?? string.Empty;
                    }
                }
            }
            return flattened;
        }

        private MemoryRecord MapToMemoryRecord(VectorSearchResult customResult, bool withEmbeddings)
        {
            var record = new MemoryRecord
            {
                Id = customResult.Id
            };

            record.Payload["text"] = customResult.Document ?? string.Empty;

            if (customResult.Metadata != null)
            {
                foreach (var kvp in customResult.Metadata)
                {
                    record.Tags.Add(kvp.Key, kvp.Value?.ToString());
                }
            }

            if (withEmbeddings)
            {
                record.Vector = new Embedding(Array.Empty<float>()); 
            }

            return record;
        }
    }
}