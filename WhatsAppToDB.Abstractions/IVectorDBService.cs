using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsAppToDB.Abstractions
{
    public interface IVectorDBService
    {
        Task<List<ReadOnlyMemory<float>>> GetVectors(List<string> texts);

        Task Delete(string collectionName);
        Task Delete(string collectionName, string id);

        Task Add(string collectionName, 
            List<string> ids, List<string>? documents, List<Dictionary<string, object>>? metadatas);
        Task<List<VectorSearchResult>> SearchCollection(
                string collectionName,
                string queryText,
                int limit = 5,
                IDictionary<string, object>? filter = null);
        Task AfterCollectionSyncCompletedAsync(
        string collectionName,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public interface IVectorDBServiceProvider
    {
        string Type { get; }

        IVectorDBService CreateVectorDBService(VectorDBSettings settings, IEmbeddingService embeddingService);
    }
}
