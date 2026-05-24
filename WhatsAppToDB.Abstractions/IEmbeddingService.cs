using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsAppToDB.Abstractions
{
    public interface IEmbeddingService
    {
        Task<List<ReadOnlyMemory<float>>> GetVectors(List<string> texts);
        Task<ReadOnlyMemory<float>> GetVector(string text);
    }
    public interface IEmbeddingServiceProvider
    {
        string Type { get; }

        IEmbeddingService CreateEmbeddingService(EmbeddingServiceSettings settings);
    }
}
