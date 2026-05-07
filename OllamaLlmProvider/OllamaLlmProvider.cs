using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OllamaLlmPlugin
{
    using Microsoft.SemanticKernel;
    using WhatsAppToDB.Abstractions;

    namespace OllamaLlmPlugin
    {
        public class OllamaLlmProvider : ILlmProvider
        {
            public string Name => "Ollama";

            public bool SupportsKernel => true;

            public List<string> GetModels()
            {
                return new()
                {
                    "gpt-oss:120b-cloud",
                    "gemma4:31b-cloud",
                    "devstral-2:123b-cloud"
                };
            }

            public Task<string> GenerateAsync(
                Microsoft.SemanticKernel.ChatCompletion.ChatHistory history,
                string model)
            {
                throw new NotImplementedException();
            }

            public void Register(
                IKernelBuilder builder,
                IServiceProvider sp,
                string model)
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri("http://localhost:11434/v1")
                };

                builder.AddOpenAIChatCompletion(
                    modelId: model,
                    apiKey: "ollama",
                    httpClient: client);
            }
        }
    }
}
