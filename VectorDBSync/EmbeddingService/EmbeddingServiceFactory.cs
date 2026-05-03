using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VectorDBSync.EmbeddingService;

namespace VectorDBSync.EmbeddingService
{
    internal class EmbeddingServiceFactory
    {
        public static IEmbeddingService Create(Settings settings)
        {
            if (settings.EmbeddingServiceSettings.Type.ToLower()=="local")
            {
                IEmbeddingService embeddingService = new LocalEmbeddingService(settings.SqliteSettings.Model);
                return embeddingService;
            } 
            else if (settings.EmbeddingServiceSettings.Type.ToLower() == "jina")
            {
                IEmbeddingService embeddingService = new JinaEmbeddingService(settings);
                return embeddingService;
            }
            return new OpenAiEmbeddingService(settings.OpenAiSettings.Model, settings.OpenAiSettings.ApiKey);
        }
    }
}
