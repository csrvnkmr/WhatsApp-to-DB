using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDBSync
{
    internal interface IEmbeddingService
    {
        Task<List<ReadOnlyMemory<float>>> GetVectors(List<string> texts);
        Task<ReadOnlyMemory<float>> GetVector(string text);
    }
}
