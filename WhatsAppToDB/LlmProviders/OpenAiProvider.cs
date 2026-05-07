using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.LlmProviders
{
    public class OpenAiProvider : ILlmProvider
    {
        public string Name => "OpenAI";

        public bool SupportsKernel => true;

        public Task<string> GenerateAsync(ChatHistory history, string model)
        {
            throw new NotImplementedException();
        }

        public List<string> GetModels()
        {
            return new List<string>
            {
                "gpt-3.5-turbo",
                "gpt-4",
                "gpt-4-0613",
                "gpt-4-32k",
                "gpt-4-32k-0613"
            };
        }
        public void Register(IKernelBuilder builder, IServiceProvider sp, string model)
        {
            var settings = sp.GetRequiredService<IOptions<OpenAiSettings>>().Value;
            Console.WriteLine($"Registering {Name} model {model}");
            builder.AddOpenAIChatCompletion(
                model,
                settings.ApiKey
            );
        }
    }
}
