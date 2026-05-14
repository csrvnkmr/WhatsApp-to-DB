using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WhatsAppToDB.VectorStore;
using WhatsAppToDB.Abstractions;

namespace WhatsAppToDB.VectorStore
{
    /// <summary>
    /// Service class for updating the vector database with SQL queries and embeddings.
    /// </summary>
    public class VectorDbService : IDisposable
    {
        private readonly VectorStore _store;

        /// <summary>
        /// Initializes a new instance of the UpdateVectorDb class.
        /// </summary>
        /// <param name="embeddingProvider">The embedding provider to use.</param>
        /// <param name="dbPath">Path to the SQLite database file.</param>
        public VectorDbService(IEmbeddingProvider embeddingProvider, string dbPath = "sql_memory.db")
        {
            _store = new VectorStore(embeddingProvider, dbPath);
        }

        /// <summary>
        /// Loads entries from a JSON file into the vector database.
        /// </summary>
        /// <param name="jsonFilePath">Path to the JSON file containing the entries.</param>
        public async Task LoadFromJsonAsync(string jsonFilePath)
        {
            await _store.LoadFromJsonAsync(jsonFilePath);
        }

        /// <summary>
        /// Adds or updates a single entry in the vector database.
        /// </summary>
        /// <param name="entry">The entry to add or update.</param>
        public async Task AddOrUpdateAsync(SqlMemoryEntry entry)
        {
            await _store.AddOrUpdateAsync(entry);
        }

        /// <summary>
        /// Searches for the most similar entries to a question.
        /// </summary>
        /// <param name="question">The search question.</param>
        /// <param name="topK">Number of top results to return.</param>
        /// <returns>List of search results.</returns>
        public async Task<List<SearchResult>> SearchAsync(string question, int topK = 3)
        {
            return await _store.SearchAsync(question, topK);
        }

        /// <summary>
        /// Deletes an entry by name.
        /// </summary>
        /// <param name="name">The name of the entry to delete.</param>
        public void DeleteByName(string name)
        {
            _store.DeleteByName(name);
        }

        /// <summary>
        /// Gets the total count of entries in the database.
        /// </summary>
        /// <returns>The count of entries.</returns>
        public int GetCount()
        {
            return _store.GetCount();
        }

        /// <summary>
        /// Disposes the underlying VectorStore.
        /// </summary>
        public void Dispose()
        {
            _store.Dispose();
        }
        
        public async static Task TestVectorDbService()
        {
            Console.WriteLine("Testing VectorDbService Starts...");
            // Example usage:
            var embeddingProvider = new OllamaEmbeddingProvider();
            using var vectorDb = new VectorDbService(embeddingProvider, 
                "C:\\Development\\AiAgents\\SemanticKernel\\ChatData\\test_sql_memory.db");
            await vectorDb.LoadFromJsonAsync(
                "C:\\Development\\AiAgents\\git\\WhatsAppToDB\\WhatsAppToDB\\Config\\databases\\B1Database2\\fewshotqueries.json");

            var qn = "how many items sold last month but not this month";

            var  result = await vectorDb.SearchAsync(qn, topK: 2);
                foreach (var r in result)
                {
                    Console.WriteLine($"Name: {r.Name}, Similarity: {r.Score}");
                    Console.WriteLine($"Query: {r.Query}");
                    Console.WriteLine("-----");
                }


            Console.WriteLine("Testing VectorDbService Completed...");
        }
    }

}
