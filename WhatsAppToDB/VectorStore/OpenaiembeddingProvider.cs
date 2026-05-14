using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WhatsAppToDB.Abstractions;
namespace WhatsAppToDB.VectorStore
{
    /// <summary>
    /// OpenAI embedding provider.
    /// Recommended model: text-embedding-3-small (1536 dims, cheap)
    /// </summary>
    public class OpenAIEmbeddingProvider : IEmbeddingProvider
    {
        private readonly HttpClient _http;
        private readonly string _model;
        private readonly int _dimensions;

        public int Dimensions => _dimensions;
        public string ProviderName => $"OpenAI ({_model})";

        /// <param name="apiKey">Your OpenAI API key</param>
        /// <param name="model">Embedding model — default: text-embedding-3-small</param>
        /// <param name="dimensions">Vector size — 1536 for small, 3072 for large</param>
        public OpenAIEmbeddingProvider(
            string apiKey,
            string model = "text-embedding-3-small",
            int dimensions = 1536)
        {
            _model = model;
            _dimensions = dimensions;
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<float[]> GenerateAsync(string text)
        {
            var results = await GenerateBatchAsync(new List<string> { text });
            return results[0];
        }

        public async Task<List<float[]>> GenerateBatchAsync(List<string> texts)
        {
            var payload = new
            {
                model = _model,
                input = texts
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(
                "https://api.openai.com/v1/embeddings", content);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var results = new List<float[]>();
            var dataArray = doc.RootElement.GetProperty("data");

            // OpenAI returns embeddings in the same order as input
            foreach (var item in dataArray.EnumerateArray())
            {
                var embeddingArray = item.GetProperty("embedding");
                var vector = new float[_dimensions];
                int i = 0;
                foreach (var val in embeddingArray.EnumerateArray())
                    vector[i++] = val.GetSingle();

                results.Add(vector);
            }

            return results;
        }
    }
}