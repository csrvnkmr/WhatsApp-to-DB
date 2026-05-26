using WhatsAppToDB.Abstractions;

namespace ChromaVectorDBProvider
{
    public sealed class ChromaVectorDBServiceProvider : IVectorDBServiceProvider
    {
        public string Type => "chroma";

        public IVectorDBService CreateVectorDBService(VectorDBSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            return new ChromaDBService(settings);
        }
    }
}
