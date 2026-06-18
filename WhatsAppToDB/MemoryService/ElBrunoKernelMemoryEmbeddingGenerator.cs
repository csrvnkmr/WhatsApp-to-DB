using System;
using System.Threading;
using System.Threading.Tasks;
using ElBruno.LocalEmbeddings;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;

namespace WhatsAppToDB.Services
{
    public class ElBrunoKernelMemoryEmbeddingGenerator : ITextEmbeddingGenerator, IDisposable
    {
        private readonly LocalEmbeddingGenerator _generator;

        // "sentence-transformers/all-MiniLM-L6-v2" maps to 384 dimensions
        public int MaxTokens => 512; 

        public ElBrunoKernelMemoryEmbeddingGenerator(string modelName = "sentence-transformers/all-MiniLM-L6-v2")
        {
            var options = new ElBruno.LocalEmbeddings.Options.LocalEmbeddingsOptions
            {
                ModelName = modelName
            };
            _generator = new LocalEmbeddingGenerator(options);
        }

        /// <summary>
        /// Kernel Memory uses this to calculate text chunk fitting.
        /// </summary>
        public int CountTokens(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            
            // Approximate token calculation if your local generator doesn't expose a tokenizer count.
            // 1 token is roughly 4 characters in English.
            return text.Length / 4; 
        }

        /// <summary>
        /// The main engine method Kernel Memory calls to generate a vector for text chunks.
        /// </summary>
        public async Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(text))
            {
                return new Embedding(Array.Empty<float>());
            }

            // Call your underlying ElBruno ONNX generator
            var result = await _generator.GenerateEmbeddingAsync(text);
            
            // Convert ReadOnlyMemory<float> to Kernel Memory's Embedding type
            return new Embedding(result.Vector.ToArray());
        }

        public void Dispose()
        {
            _generator?.Dispose();
        }
 
        public IReadOnlyList<string> GetTokens(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<string>();
            }

            // Split by words while keeping the whitespace/punctuation boundaries
            // This provides a highly accurate structural approximation for standard sentence-transformers
            return text.Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}