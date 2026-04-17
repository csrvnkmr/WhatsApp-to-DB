using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using WhatsAppToDB;
using WhatsAppToDB.Abstractions;

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

    async Task ExecuteQuery(string messageText, IdentityContext identity)
    {
        WhatsAppSettings? waSettings = null;
        try
        {
            var sp = _serviceProvider;
            var logger = sp.GetService<IWhatsAppLogger>();
            {
                var waOptions = sp.GetRequiredService<IOptions<WhatsAppSettings>>();
                waSettings = waOptions.Value; // Capture the actual settings object

                //var identityService = sp.GetRequiredService<IIdentityService>();
                //var identity = await identityService.GetIdentityAsync(senderPhone);

                var kernel = sp.GetRequiredService<Kernel>();
                if (kernel.FunctionInvocationFilters?.Count == 0)
                {
                    kernel.FunctionInvocationFilters.Add(new FunctionCallLogger());
                }
                var aiOptions = sp.GetRequiredService<IOptions<OpenAiSettings>>();
                var history = new ChatHistory();
                var chatService = kernel.GetRequiredService<IChatCompletionService>();
                var systemPrompt = aiOptions.Value.FullSystemPrompt;

                if (identity != null)
                {
                    //systemPrompt = $"Context {identity.SessionContextKey}, ID {identity.InternalUserId} \n\n" + systemPrompt;
                    systemPrompt += $"\n[ACTIVE CONTEXT]";
                    systemPrompt += $"\nUserRole: {identity.Role}";
                    systemPrompt += $"\nYourID: {identity.InternalUserId}";
                    systemPrompt += $"\nContextKey: {identity.SessionContextKey}";
                }

                history.AddSystemMessage(systemPrompt);

                history.AddUserMessage(messageText);
                kernel.Data["UserIdentity"] = identity;
                kernel.Data["WhatsAppNumber"] = identity.WhatsAppNumber; // senderPhone;
                kernel.Data["UserQuestion"] = messageText;
                var settings = new Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                    Temperature = 0.0,
                    TopP = 0.1   // further tightens token sampling
                };
                var aiResponse = await chatService.GetChatMessageContentAsync(history,
                            executionSettings: settings, //openAIPromptExecutionSettings,
                            kernel: kernel
                            );
                history.Add(aiResponse);
                await logger.LogAsync("Question: " + messageText);
                await logger.LogAsync("AI Response:");
                await logger.LogAsync(aiResponse.Content);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Background Error: {ex}");
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

    // Helper method to simulate the real webhook flow
    public async Task ExecuteQueryWithIdentity(string senderPhone, string messageText)
    {
        using var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var identityService = sp.GetRequiredService<IIdentityService>();
        var identity = await identityService.GetIdentityAsync(senderPhone);

        // Now call the execution logic
        await ExecuteQuery(messageText, identity);
    }

    public async static Task RunSecurityTests(WebApplicationBuilder builder)
    {
        // 1. Define your test data directly in code
        var testData = new Dictionary<string, IdentityContext>
        {
            ["919876543210"] = new IdentityContext
            {
                Role = "SalesPerson",
                InternalUserId = "277",
                AuthorizedModules = new List<string> { "Sales", "Person" },
                SessionContextKey = "SalesPersonID",
                WhatsAppNumber= "919876543210"
            },
            ["919000000000"] = new IdentityContext
            {
                Role = "Employee",
                InternalUserId = "16",
                AuthorizedModules = new List<string> { "HumanResources" },
                SessionContextKey = "EmployeeId",
                WhatsAppNumber = "919000000000"
            },
            ["911234567890"] = new IdentityContext
            {
                Role = "Customer",
                InternalUserId = "26564",
                AuthorizedModules = new List<string> { "Sales" },
                SessionContextKey = "CustomerID",
                WhatsAppNumber = "911234567890"
            },
            ["919092044002"] = new IdentityContext
            {
                Role = "Admin",
                InternalUserId = "1",
                AuthorizedModules = new List<string> { "*" },
                SessionContextKey = "EmployeeId",
                WhatsAppNumber = "919092044002"
            }
        };
        builder.Services.AddSingleton<IIdentityService>(new TestIdentityService(testData));

        var harness = new AdventureWorksTestHarness(builder.Services);

        Console.WriteLine("--- Running Sales Person Test ---");
        // Notice we only pass the phone and query, just like the real webhook
        //await harness.ExecuteQueryWithIdentity("919876543210", "What are my total sales?");
        //await harness.ExecuteQueryWithIdentity("919876543210", "कुल बिक्री कितनी है Incomparable Bicycle Store?");
        //await harness.ExecuteQueryWithIdentity("919876543210", "Incomparable Bicycle Store மொத்த விற்பனை எவ்வளவு?");
        await harness.ExecuteQueryWithIdentity("919876543210", "இன்கப்பேரபில்  பைசைக்கிள் ஸ்டோர் மொத்த விற்பனை எவ்வளவு?");
        //இன்கப்பேரபில்  பைசைக்கிள் ஸ்டோர் 
        //மொத்த விற்பனை எவ்வளவு
        //अतुलनीय साइकिल स्टोर की कुल बिक्री कितनी है?
        Console.WriteLine("\n--- Running Sales Person Test (trying to access HR module) Fail---");
        await harness.ExecuteQueryWithIdentity("919876543210", "Salary history of David Johnson");

        Console.WriteLine("\n--- Running Sales Person Test (Context aware) Expected Success---");
        //await harness.ExecuteQueryWithIdentity("919876543210", " என் பெயர் என்ன ");
        await harness.ExecuteQueryWithIdentity("919876543210", " मेरा फ़ोन नंबर और ईमेल पता क्या है");
        //        मेरा फ़ोन नंबर और ईमेल पता क्या है
        Console.WriteLine("******************** Sales Person Test ends ****************\n\n\n");
        
        if (testData.Count == 4)
        {            
            return;
        }

        Console.WriteLine("\n--- Running Employee Test accessing sales Fail---");
        await harness.ExecuteQueryWithIdentity("919000000000", "What are my total sales");

        Console.WriteLine("\n--- Running Employee Test (trying to access salary of another employee) Fail---");
        await harness.ExecuteQueryWithIdentity("919000000000", "Salary history of David Johnson");

        Console.WriteLine("\n--- Running Employee Test (trying to access salary of self) Success---");
        await harness.ExecuteQueryWithIdentity("919000000000", "Salary history of David Bradly");

        Console.WriteLine("******************** Employee Test ends ****************\n\n\n");

        Console.WriteLine("\n--- Running Customer Test (trying to access salary ) fail---");
        await harness.ExecuteQueryWithIdentity("911234567890", "Salary history of David Bradly");

        Console.WriteLine("\n--- Running Customer Test (trying to access sales of others) fail---");
        await harness.ExecuteQueryWithIdentity("911234567890", "total sales for store Rider Cycles");

        Console.WriteLine("\n--- Running Customer Test (trying to access sales of self) success---");
        await harness.ExecuteQueryWithIdentity("911234567890", "total sales for store Franklin Chen");

        Console.WriteLine("******************** Customer Test ends ****************\n\n\n");

        Console.WriteLine("--- Running Admin Test -- Should Succeed---");
        // Notice we only pass the phone and query, just like the real webhook
        await harness.ExecuteQueryWithIdentity("919092044002", "What are my total sales?");

        Console.WriteLine("\n--- Running Admin Test -- Should Succeed---");
        await harness.ExecuteQueryWithIdentity("919092044002", "Salary history of David Johnson");

        Console.WriteLine("\n--- Running Admin Test -- Should Succeed---");
        await harness.ExecuteQueryWithIdentity("919092044002", "Salary history of David Bradly");

        Console.WriteLine("******************** Admin Test ends ****************\n\n\n");

    }
}