using OpenAI;
using OpenAI.Embeddings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsAppToDB.Abstractions;

namespace VectorDBSync.EmbeddingService
{
    internal class OpenAiEmbeddingService : IEmbeddingService
    {

        string modelname;
        string apikey;
        private readonly EmbeddingClient _embeddingClient;
        public OpenAiEmbeddingService(string modelname, string apikey)
        {
            this.modelname = modelname;
            this.apikey = apikey;
            _embeddingClient = new OpenAIClient(apikey).GetEmbeddingClient(modelname);
        }

        public async Task<List<ReadOnlyMemory<float>>> GetVectors(List<string> texts)
        {
            var result = await _embeddingClient.GenerateEmbeddingsAsync(texts);
            return result.Value.Select(r => r.ToFloats()).ToList();
        }
        public async Task<ReadOnlyMemory<float>> GetVector(string text)
        {
            var result = await _embeddingClient.GenerateEmbeddingAsync(text);
            return result.Value.ToFloats();
        }
    }

    public class OpenAiEmbeddingServiceProvider : IEmbeddingServiceProvider
    {
        public string Type => "openai";

        public IEmbeddingService CreateEmbeddingService(EmbeddingServiceSettings settings)
        {
            return new OpenAiEmbeddingService(settings.Model, settings.ApiKey);
        }
    }
}
