using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.LlmProviders;

namespace AzureOpenAiLlmPlugin
{
    public class AzureOpenAiLlmProvider : ILlmProvider
    {
        public string Name => "AzureOpenAI";

        public bool SupportsKernel => true;

        public Task<string> GenerateAsync(
            Microsoft.SemanticKernel.ChatCompletion.ChatHistory history,
            string model,Kernel kernel)
        {
            throw new NotImplementedException();
        }

        public void Register(IKernelBuilder builder, 
            LlmConfig config,
            string model)
        {
            // 1. Explicitly parse your Azure Foundry project endpoint as a Uri
            // Example string format: "https://<YOUR-RESOURCE>.services.ai.azure.com/api/projects/<YOUR-PROJECT>/openai/v1"
            var foundryEndpoint = new Uri(config.HttpEndPoint);

            // 2. Use Semantic Kernel's native Custom Endpoint overload
            builder.AddOpenAIChatCompletion(
                modelId: model,                      // The name of your deployed model in Foundry
                endpoint: foundryEndpoint,           // Direct Custom API Endpoint
                apiKey: config.ApiKey,               // Your Service Principal Secret or Entra token
                httpClient: new HttpClient()         // Standard HTTP client factory instance
            );
        }
    }
}
