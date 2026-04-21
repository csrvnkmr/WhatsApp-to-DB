using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDBSync
{
    internal interface IVectorDBService
    {
        Task<List<ReadOnlyMemory<float>>> GetVectors(List<string> texts);
        Task Add(string collectionName, 
            List<string> ids, List<string>? documents, List<Dictionary<string, Object>>? metadatas);
        Task<List<SearchResult>> SearchCollection(
                string collectionName,
                string queryText,
                int limit = 5,
                IDictionary<string, object>? filter = null);
    }
}
