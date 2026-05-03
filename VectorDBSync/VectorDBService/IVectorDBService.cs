using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDBSync.VectorDBService
{
    public interface IVectorDBService
    {
        Task<List<ReadOnlyMemory<float>>> GetVectors(List<string> texts);

        Task Delete(string collectionName);

        Task Add(string collectionName, 
            List<string> ids, List<string>? documents, List<Dictionary<string, object>>? metadatas);
        Task<List<SearchResult>> SearchCollection(
                string collectionName,
                string queryText,
                int limit = 5,
                IDictionary<string, object>? filter = null);
    }
}
