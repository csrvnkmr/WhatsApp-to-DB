using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WhatsAppToDB.Abstractions;

namespace WhatsAppToDB.VectorStore
{
    /// <summary>
    /// Ollama embedding provider — runs locally, no API key needed.
    /// Recommended models:
    ///   nomic-embed-text  → 768 dims  (good quality, fast)
    ///   mxbai-embed-large → 1024 dims (best quality)
    ///   all-minilm        → 384 dims  (fastest, lightest)
    ///
    /// Pull model first: ollama pull nomic-embed-text
    /// </summary>
    public class OllamaEmbeddingProvider : IEmbeddingProvider
    {
        private readonly HttpClient _http;
        private readonly string _model;
        private readonly int _dimensions;

        public int Dimensions => _dimensions;
        public string ProviderName => $"Ollama ({_model})";

        /// <param name="model">Ollama model name</param>
        /// <param name="dimensions">Must match the model's output dimensions</param>
        /// <param name="baseUrl">Ollama server URL — default: localhost</param>
        public OllamaEmbeddingProvider(
            string model = "nomic-embed-text",
            int dimensions = 768,
            string baseUrl = "http://localhost:11434")
        {
            _model = model;
            _dimensions = dimensions;
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public async Task<float[]> GenerateAsync(string text)
        {
            var payload = new { model = _model, prompt = text };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("/api/embeddings", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var embeddingArray = doc.RootElement.GetProperty("embedding");
            var vector = new float[_dimensions];
            int i = 0;
            foreach (var val in embeddingArray.EnumerateArray())
                vector[i++] = val.GetSingle();

            return vector;
        }

        /// <summary>
        /// Ollama does not have a native batch endpoint.
        /// Calls GenerateAsync sequentially — still more efficient than rebuilding.
        /// </summary>
        public async Task<List<float[]>> GenerateBatchAsync(List<string> texts)
        {
            var results = new List<float[]>();
            foreach (var text in texts)
                results.Add(await GenerateAsync(text));
            return results;
        }
    }
}