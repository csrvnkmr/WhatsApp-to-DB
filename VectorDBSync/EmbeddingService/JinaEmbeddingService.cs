
using ElBruno.LocalEmbeddings;
using ElBruno.LocalEmbeddings.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace VectorDBSync.EmbeddingService
{
    /// <summary>
    /// Jina AI Embedding Service — matches IEmbeddingService interface.
    ///
    /// GetVectors → Jina REST API  (bulk, fast, free 10M tokens on signup)
    ///              Use for: building / refreshing the vector DB
    ///
    /// GetVector  → Local ONNX via ElBruno  (same model weights, no API call)
    ///              Use for: real-time search query embedding
    ///
    /// Model: jina-embeddings-v3 (online) == jinaai/jina-embeddings-v3 (local ONNX)
    /// Both produce identical 1024-dim vectors — fully compatible for search.
    ///
    /// Sign up for free API key: https://jina.ai/embeddings/
    /// </summary>
    public class JinaEmbeddingService : IEmbeddingService, IDisposable
    {
        // ── Online (Jina API) ────────────────────────────────────────────────
        private readonly HttpClient _http;
        private const string ApiUrl = "https://api.jina.ai/v1/embeddings";
        private readonly string ApiModel = "jina-embeddings-v3";
        private readonly string LocalModel = "jinaai/jina-embeddings-v3";

        // Max texts per API call — Jina supports up to 2048 but 100 is safe
        private const int ApiBatchSize = 100;

        // ── Local ONNX (ElBruno) ─────────────────────────────────────────────
        private LocalEmbeddingGenerator _localGenerator;

        // ── Constructor ──────────────────────────────────────────────────────

        /// <param name="jinaApiKey">
        ///   Jina API key from jina.ai — free tier gives 10M tokens.
        /// </param>
        /// <param name="localModelName">
        ///   HuggingFace model ID for local ONNX search.
        ///   Must match the API model to keep vectors compatible.
        ///   Default: "jinaai/jina-embeddings-v3"
        ///   Fallback: "sentence-transformers/all-MiniLM-L6-v2" if you want
        ///             to stay with your existing local model (but then use
        ///             HuggingFace API for GetVectors instead — see note below).
        /// </param>
        public JinaEmbeddingService(Settings settings)
        {
            // ── HTTP client for Jina API ──
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.JinaAiSettings.ApiKey}");

            // ── Local ONNX for search queries ──
            // ElBruno downloads the model on first run and caches it locally.
            // jina-embeddings-v3 is ~300MB — downloaded once, used forever.
            ApiModel = settings.JinaAiSettings.ApiModel;
            LocalModel = settings.JinaAiSettings.LocalModel ?? LocalModel;
        }

        // ── GetVectors — Jina API (bulk, use for DB creation) ────────────────

        public async Task<List<ReadOnlyMemory<float>>> GetVectors(List<string> texts)
        {
            if (texts == null || texts.Count == 0)
                return new List<ReadOnlyMemory<float>>();

            var allVectors = new List<ReadOnlyMemory<float>>(texts.Count);

            // Slice into API-safe batches
            for (int i = 0; i < texts.Count; i += ApiBatchSize)
            {
                var batch = texts.Skip(i).Take(ApiBatchSize).ToList();
                var batchVectors = await CallJinaApiAsync(batch);
                allVectors.AddRange(batchVectors);

                // Small delay to respect rate limits on free tier
                if (i + ApiBatchSize < texts.Count)
                    await Task.Delay(100);
            }

            return allVectors;
        }

        private async Task<List<ReadOnlyMemory<float>>> CallJinaApiAsync(List<string> texts)
        {
            var payload = new
            {
                model = ApiModel,
                input = texts,
                task = "text-matching",   // best for similarity/search
                normalized = true          // L2-normalized, ready for cosine similarity
            };

            var response = await _http.PostAsJsonAsync(ApiUrl, payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Jina API error {response.StatusCode}: {error}");
            }

            var result = await response.Content
                .ReadFromJsonAsync<JinaResponse>();

            return result!.Data
                .OrderBy(d => d.Index)
                .Select(d => new ReadOnlyMemory<float>(d.Embedding))
                .ToList();
        }

        // ── GetVector — Local ONNX (single query, use for search) ────────────

        private LocalEmbeddingGenerator LocalGenerator
        {
            get
            {
                if (_localGenerator == null)
                {
                    var options = new LocalEmbeddingsOptions
                    {
                        ModelName = LocalModel,
                        NormalizeEmbeddings = true  // required for cosine similarity
                    };
                    _localGenerator = new LocalEmbeddingGenerator(options);

                }
                return _localGenerator;
            }
        }

        public async Task<ReadOnlyMemory<float>> GetVectorLocal(string text)
        {
            // Local ONNX — no API call, no internet, instant (~1ms after warmup)
            var result = await LocalGenerator.GenerateEmbeddingAsync(text);
            return result.Vector;
        }

        public async Task<ReadOnlyMemory<float>> GetVector(string text)
        {
            // Jina API — use for search
            var result = await CallJinaApiAsync(new List<string> { text });
            return result.FirstOrDefault();
        }

        // ── Jina API response models ─────────────────────────────────────────

        private record JinaResponse(
            [property: JsonPropertyName("data")] List<JinaData> Data,
            [property: JsonPropertyName("usage")] JinaUsage? Usage);

        private record JinaData(
            [property: JsonPropertyName("embedding")] float[] Embedding,
            [property: JsonPropertyName("index")] int Index);

        private record JinaUsage(
            [property: JsonPropertyName("total_tokens")] int TotalTokens);

        // ── Dispose ──────────────────────────────────────────────────────────

        public void Dispose()
        {
            _http.Dispose();
            _localGenerator?.Dispose();
        }
    }

}
