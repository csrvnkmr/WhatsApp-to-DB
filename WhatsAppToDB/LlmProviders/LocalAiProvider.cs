using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.LlmProviders
{
    public class LocalAiProvider : ILlmProvider
    {
        public string Name => "local";

        public bool SupportsKernel => true;

        public Task<string> GenerateAsync(ChatHistory history, string model, Kernel kernel)
        {
            throw new NotImplementedException();
        }

        public void Register(IKernelBuilder builder, IServiceProvider sp, string model)
        {
            var llmRegistry = sp.GetRequiredService<LlmRegistry>();
            var llmConfig = llmRegistry.Get(Name); // Ensure config is loaded

            Console.WriteLine($"Registering {Name} model {model}");
            builder.AddOpenAIChatCompletion(
                modelId: model,
                apiKey: llmConfig.ApiKey,
                httpClient: new HttpClient
                {
                    BaseAddress = new Uri(llmConfig.HttpEndPoint)
                }
            );

            Console.WriteLine("[Kernel] Local AI connected via OpenAI-Compatible HTTP Endpoint.");
        }

        public void Register(IKernelBuilder builder, LlmConfig config, string model)
        {
            Console.WriteLine($"Registering {Name} model {model}");
            builder.AddOpenAIChatCompletion(
                modelId: model,
                apiKey: config.ApiKey,
                httpClient: new HttpClient
                {
                    BaseAddress = new Uri(config.HttpEndPoint)
                }
            );

            Console.WriteLine("[Kernel] Local AI connected via OpenAI-Compatible HTTP Endpoint.");
        }
    }
}
