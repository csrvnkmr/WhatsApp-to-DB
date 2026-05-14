using System.Collections.Generic;
using System.Threading.Tasks;

namespace WhatsAppToDB.Abstractions
{
    /// <summary>
    /// Generic interface for embedding providers.
    /// Implement this to support any provider (OpenAI, Ollama, Azure, Cohere, etc.)
    /// </summary>
    public interface IEmbeddingProvider
    {
        /// <summary>
        /// Generate embedding for a single text input.
        /// </summary>
        Task<float[]> GenerateAsync(string text);

        /// <summary>
        /// Generate embeddings for multiple texts in one batch call.
        /// More efficient than calling GenerateAsync in a loop.
        /// </summary>
        Task<List<float[]>> GenerateBatchAsync(List<string> texts);

        /// <summary>
        /// Dimension size of the embedding vector (e.g. 1536 for OpenAI, 384 for MiniLM).
        /// Used when creating the SQLite vec0 virtual table.
        /// </summary>
        int Dimensions { get; }

        /// <summary>
        /// Provider name for logging/diagnostics.
        /// </summary>
        string ProviderName { get; }
    }
}