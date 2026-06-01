using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.LlmProviders;

namespace GoogleAiLlmPlugin
{
    public class GoogleAiLlmProvider : ILlmProvider
    {
        public string Name => "GoogleAi";

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
            var client = new HttpClient
            {
                BaseAddress = new  Uri(config.HttpEndPoint) //Uri("http://localhost:11434/v1")
            };

            builder.AddOpenAIChatCompletion(                    
                modelId: model,
                apiKey: config.ApiKey,
                httpClient: client);
        }
    }
}
