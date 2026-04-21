using ElBruno.LocalEmbeddings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VectorDBSync
{


    internal class LocalEmbeddingService : IEmbeddingService, IDisposable
    {
        private readonly LocalEmbeddingGenerator _generator;

        public LocalEmbeddingService(string modelName = "sentence-transformers/all-MiniLM-L6-v2")
        {
            // The library handles model downloading/loading automatically.
            // all-MiniLM-L6-v2 is the default and produces 384-dimension vectors.
            var options = new ElBruno.LocalEmbeddings.Options.LocalEmbeddingsOptions
            {
                ModelName = modelName
            };

            _generator = new LocalEmbeddingGenerator(options);
        }

        public async Task<List<ReadOnlyMemory<float>>> GetVectors(List<string> texts)
        {
            // GenerateAsync handles batching for multiple strings
            var result = await _generator.GenerateAsync(texts.ToArray());

            // Result is a list of Embedding objects; we extract the Vector property
            return result.Select(r => r.Vector).ToList();
        }

        public async Task<ReadOnlyMemory<float>> GetVector(string text)
        {
            // GenerateEmbeddingAsync is optimized for a single string
            var result = await _generator.GenerateEmbeddingAsync(text);
            return result.Vector;
        }

        public void Dispose()
        {
            // Local models use ONNX Runtime resources that should be released
            _generator?.Dispose();
        }
    }
}

