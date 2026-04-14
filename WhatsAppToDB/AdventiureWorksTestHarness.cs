using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using WhatsAppToDB;

public class AdventureWorksTestHarness
{
    private readonly IServiceProvider _serviceProvider;

    public AdventureWorksTestHarness(IServiceCollection services)
    {
        // This mimics your Program.cs DI setup
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task RunTestScenario(string userQuery)
    {
        Console.WriteLine($"\n--- TESTING QUERY: \"{userQuery}\" ---");

        using var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        try
        {
            // 1. Resolve the Kernel
            var kernel = sp.GetRequiredService<Kernel>();
            var aiOptions = sp.GetRequiredService<IOptions<OpenAiSettings>>();

            // 2. Setup Chat History with your AdventureWorks System Prompt
            var history = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            history.AddSystemMessage(aiOptions.Value.FullSystemPrompt);
            history.AddUserMessage(userQuery);

            // 3. Execution Settings (Enable Tool Calling)
            var settings = new Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var chatService = kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();

            // 4. Execute
            var result = await chatService.GetChatMessageContentAsync(history, settings, kernel);

            Console.WriteLine("AI RESPONSE:");
            Console.WriteLine(result.Content);

            // Optional: Print the function calling trace if needed
            // This helps verify if 'SearchBusinessPartner' was actually called
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TEST FAILURE]: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
        }
    }

    public async static Task RunTestMessages(WebApplicationBuilder builder)
    {
        // Inside your Program.cs or a Test Runner
        var harness = new AdventureWorksTestHarness(builder.Services);

        // Scenario 1: Fuzzy Name Matching + SQL (Retail)
        await harness.RunTestScenario("What did David Robinett buy recently?");

        // Scenario 2: Complex Join (Corporate)
        await harness.RunTestScenario("How many orders were placed by 'Professional Sales and Service'?");

        // Scenario 3: Filtered Search (Ensuring the AI uses the Role filter)
        await harness.RunTestScenario("Find the contact details for the Sales Person named 'Linda Mitchel'.");
    }
}