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
                model, llmConfig.ApiKey                
            );
        }

        public void Register(IKernelBuilder builder, LlmConfig config, string model)
        {
            Console.WriteLine($"Registering {Name} model {model}");
            builder.AddOpenAIChatCompletion(
                model, config.ApiKey                
            );
        }
    }
}
