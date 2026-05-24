using WhatsAppToDB.Abstractions;

namespace VectorDBSync.VectorDBService
{
    public sealed class SQLiteVectorDBServiceProvider : IVectorDBServiceProvider
    {
        public string Type => "sqlite";

        public IVectorDBService CreateVectorDBService(VectorDBSettings settings, IEmbeddingService embeddingService)
        {
            ArgumentNullException.ThrowIfNull(settings);
            return new SQLiteVectorDBService(settings);
        }
    }
}
