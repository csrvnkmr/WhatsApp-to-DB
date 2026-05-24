using System.Net.Http.Json;
using System.Text.Json.Serialization;
using WhatsAppToDB.Abstractions;

namespace JinaEmbeddingProvider;

public sealed class JinaEmbeddingProvider : IEmbeddingServiceProvider
{
    public string Type => "jina";

    public IEmbeddingService CreateEmbeddingService(EmbeddingServiceSettings settings)
    {
        return new JinaEmbeddingService(settings);
    }

    private sealed class JinaEmbeddingService : IEmbeddingService, IDisposable
    {
        private const string DefaultApiUrl = "https://api.jina.ai/v1/embeddings";
        private const string DefaultModel = "jina-embeddings-v3";
        private const int ApiBatchSize = 100;

        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        private readonly string _model;

        public JinaEmbeddingService(EmbeddingServiceSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _apiUrl = string.IsNullOrWhiteSpace(settings.Url) ? DefaultApiUrl : settings.Url.Trim();
            _model = string.IsNullOrWhiteSpace(settings.Model) ? DefaultModel : settings.Model.Trim();

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.ApiKey.Trim()}");
            }
        }

        public async Task<List<ReadOnlyMemory<float>>> GetVectors(List<string> texts)
        {
            if (texts is null || texts.Count == 0)
            {
                return new List<ReadOnlyMemory<float>>();
            }

            var vectors = new List<ReadOnlyMemory<float>>(texts.Count);

            for (var index = 0; index < texts.Count; index += ApiBatchSize)
            {
                var batch = texts.Skip(index).Take(ApiBatchSize).ToList();
                vectors.AddRange(await CallJinaApiAsync(batch));

                if (index + ApiBatchSize < texts.Count)
                {
                    await Task.Delay(100);
                }
            }

            return vectors;
        }

        public async Task<ReadOnlyMemory<float>> GetVector(string text)
        {
            var vectors = await CallJinaApiAsync(new List<string> { text });
            return vectors.FirstOrDefault();
        }

        private async Task<List<ReadOnlyMemory<float>>> CallJinaApiAsync(List<string> texts)
        {
            var payload = new
            {
                model = _model,
                input = texts,
                task = "text-matching",
                normalized = true
            };

            using var response = await _httpClient.PostAsJsonAsync(_apiUrl, payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Jina API error {response.StatusCode}: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<JinaResponse>();
            if (result?.Data is null)
            {
                return new List<ReadOnlyMemory<float>>();
            }

            return result.Data
                .OrderBy(item => item.Index)
                .Select(item => new ReadOnlyMemory<float>(item.Embedding))
                .ToList();
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private sealed record JinaResponse(
            [property: JsonPropertyName("data")] List<JinaData>? Data,
            [property: JsonPropertyName("usage")] JinaUsage? Usage);

        private sealed record JinaData(
            [property: JsonPropertyName("embedding")] float[] Embedding,
            [property: JsonPropertyName("index")] int Index);

        private sealed record JinaUsage(
            [property: JsonPropertyName("total_tokens")] int TotalTokens);
    }
}
