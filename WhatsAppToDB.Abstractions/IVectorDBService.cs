using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WhatsAppToDB.Abstractions
{
    public interface IVectorDBService
    {
        Task Delete(string collectionName);
        Task Delete(string collectionName, string id);

        Task Add(string collectionName,
            List<string> ids, List<ReadOnlyMemory<float>> vectors, List<string>? documents = null, List<Dictionary<string, object>>? metadatas = null);
        Task<List<VectorSearchResult>> SearchCollection(
                string collectionName,
                ReadOnlyMemory<float> queryVector,
                string? queryText = null,
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

        IVectorDBService CreateVectorDBService(VectorDBSettings settings);
    }
}
