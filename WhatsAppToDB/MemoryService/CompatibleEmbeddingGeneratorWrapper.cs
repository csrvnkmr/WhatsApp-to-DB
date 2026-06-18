using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;

namespace WhatsAppToDB.Services
{
    /// <summary>
    /// Wrapper around ITextEmbeddingGenerator that adjusts the MaxTokens limit
    /// to be compatible with Kernel Memory's default text partitioning (1000 tokens per paragraph).
    /// 
    /// This allows using embedding generators with lower token limits (e.g., 512 tokens)
    /// by reporting a higher limit to Kernel Memory while still passing through to the
    /// underlying generator for actual embedding generation.
    /// </summary>
    public class CompatibleEmbeddingGeneratorWrapper : ITextEmbeddingGenerator
    {
        private readonly ITextEmbeddingGenerator _innerGenerator;
        private readonly int _reportedMaxTokens;

        /// <summary>
        /// Create a wrapper that adjusts the reported MaxTokens limit.
        /// </summary>
        /// <param name="innerGenerator">The actual embedding generator to use</param>
        /// <param name="reportedMaxTokens">The token limit to report to Kernel Memory (default: 1024 for compatibility)</param>
        public CompatibleEmbeddingGeneratorWrapper(ITextEmbeddingGenerator innerGenerator, int reportedMaxTokens = 1024)
        {
            _innerGenerator = innerGenerator ?? throw new ArgumentNullException(nameof(innerGenerator));
            _reportedMaxTokens = reportedMaxTokens;
        }

        /// <summary>
        /// Reports an adjusted MaxTokens limit for Kernel Memory text partitioning.
        /// </summary>
        public int MaxTokens => _reportedMaxTokens;

        /// <summary>
        /// Count tokens using the inner generator's implementation.
        /// </summary>
        public int CountTokens(string text)
        {
            return _innerGenerator.CountTokens(text);
        }

        /// <summary>
        /// Generate embeddings using the inner generator.
        /// </summary>
        public async Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            return await _innerGenerator.GenerateEmbeddingAsync(text, cancellationToken);
        }

        /// <summary>
        /// Get tokens using the inner generator's implementation.
        /// </summary>
        public IReadOnlyList<string> GetTokens(string text)
        {
            return _innerGenerator.GetTokens(text);
        }
    }
}
