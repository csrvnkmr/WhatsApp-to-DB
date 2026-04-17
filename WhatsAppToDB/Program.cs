using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.IO.Enumeration;
using System.Net.Http.Json;
using System.Runtime.Loader;
using System.Text.Json;
using WhatsAppToDB;
using WhatsAppToDB.Abstractions;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);
var metadata = new PluginMetadata();

// Centralize all service configurations in one method for better organization
// this is an extension method in ServiceCollectionExtension.cs
builder.Services.ConfigureAllServices(builder.Configuration, metadata);

// Enable planning
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

//await AdventureWorksTestHarness.RunTestMessages(builder);

await AdventureWorksTestHarness.RunSecurityTests(builder);

var app = builder.Build();

app.MapGet("/webhook", (HttpContext context,
    IOptions<WhatsAppSettings> waOptions) => {
    var verifyToken = waOptions.Value.VerifyToken;
    string mode = context.Request.Query["hub.mode"];
    string token = context.Request.Query["hub.verify_token"];
    string challenge = context.Request.Query["hub.challenge"];

    if (mode == "subscribe" && token == verifyToken) return Results.Ok(challenge);
    return Results.BadRequest();
});

app.MapPost("/webhook", async (
    HttpContext context,
    IServiceScopeFactory scopeFactory, // Inject the Singleton Factory
    IWhatsAppLogger waLogger) =>
{
    Console.WriteLine("Webhook received a message at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();

    var waService = new WhatsAppService();

    var result = await waService.GetWhatsAppMessage(body, waLogger);
    if (!result.isSuccess)
    {
        return Results.Ok();
    }

    var senderPhone = result.to;
    var messageText = result.message;

    _ = Task.Run(async () =>
    {
        await ExecuteQuery(scopeFactory, senderPhone, messageText, openAIPromptExecutionSettings, waLogger, waService);
    });

    return Results.Ok();

});

app.Run();

async static Task ExecuteQuery(IServiceScopeFactory scopeFactory,
    string senderPhone, string messageText, PromptExecutionSettings? pes, IWhatsAppLogger waLogger, WhatsAppService waService)
{
    WhatsAppSettings? waSettings = null;
    try
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var waOptions = sp.GetRequiredService<IOptions<WhatsAppSettings>>();
            waSettings = waOptions.Value; // Capture the actual settings object

            var identityService = sp.GetRequiredService<IIdentityService>();
            var identity = await identityService.GetIdentityAsync(senderPhone);

            await waService.SendWhatsAppResponse(senderPhone, "_Analyzing your request and querying SAP... Please wait a moment._ 🔍", waSettings, waLogger);
            var kernel = sp.GetRequiredService<Kernel>();
            var aiOptions = sp.GetRequiredService<IOptions<OpenAiSettings>>();

            kernel.Data["UserIdentity"] = identity;
            kernel.Data["WhatsAppNumber"] = senderPhone;
            kernel.Data["UserQuestion"] = messageText;

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
            var aiResponse = await chatService.GetChatMessageContentAsync(history,
                        executionSettings: pes, //openAIPromptExecutionSettings,
                        kernel: kernel
                        );
            history.Add(aiResponse);
            await waLogger.LogAsync(senderPhone, $"Sending response to {senderPhone} {aiResponse.Content}");

            await waService.SendWhatsAppResponse(senderPhone, aiResponse.Content, waSettings, waLogger);

        }
    }
    catch (Exception ex)
    {
        await waLogger.LogAsync(senderPhone, "Exception when querying and sending message " + ex.ToString());
        Console.WriteLine($"Background Error: {ex}");
        await waService.SendWhatsAppResponse(senderPhone, "Sorry, I encountered an error while accessing SAP. Please try again.", waSettings, waLogger);
    }
}



