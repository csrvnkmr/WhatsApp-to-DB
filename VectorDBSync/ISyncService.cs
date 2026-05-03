using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDBSync
{
    public interface ISyncService
    {
        Task SyncAllCollections(List<VectorSyncConfig> configs);

        
        Task<List<SearchResult>> SearchCollection(string collectionName,
                string queryText,
                int limit = 5,
                IDictionary<string, object>? filter = null);
    }
}
