using WhatsAppToDB.Abstractions;

namespace ChromaVectorDBProvider
{
    public sealed class ChromaVectorDBServiceProvider : IVectorDBServiceProvider
    {
        public string Type => "chroma";

        public IVectorDBService CreateVectorDBService(VectorDBSettings settings, IEmbeddingService embeddingService)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(embeddingService);
            return new ChromaDBService(settings, embeddingService);
        }
    }
}
