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

        public Task<string> GenerateAsync(ChatHistory history, string model)
        {
            throw new NotImplementedException();
        }

        public List<string> GetModels() => new()
    {
        "llama3",
        "mistral"
    };

        public void Register(IKernelBuilder builder, IServiceProvider sp, string model)
        {
            var settings = sp.GetRequiredService<IOptions<LocalAiSettings>>().Value;
            Console.WriteLine($"Registering {Name} model {model}");
            builder.AddOpenAIChatCompletion(
                modelId: model,
                apiKey: settings.ApiKey,
                httpClient: new HttpClient
                {
                    BaseAddress = new Uri(settings.HttpEndPoint)
                }
            );

            Console.WriteLine("[Kernel] Local AI connected via OpenAI-Compatible HTTP Endpoint.");
        }
    }
}
