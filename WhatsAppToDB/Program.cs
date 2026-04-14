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

// Bind JSON sections to objects
builder.Services.Configure<WhatsAppSettings>(builder.Configuration.GetSection("WhatsAppSettings"));
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("DatabaseSettings"));
builder.Services.Configure<OpenAiSettings>(builder.Configuration.GetSection("OpenAiSettings"));
builder.Services.AddDynamicExtensions(builder.Configuration);
var metadata = new PluginMetadata();

await AddPlugin( builder, metadata);

builder.Services.AddSingleton<IWhatsAppLogger, WhatsAppLogger>();

builder.Services.AddScoped<DatabaseQueryPlugin>();

// Enable planning
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};
await AddKernel(builder, metadata);

//await AdventureWorksTestHarness.RunTestMessages(builder);

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


async static Task AddPlugin(WebApplicationBuilder builder, PluginMetadata metadata)
{
    var pluginSettings = builder.Configuration.GetSection("PluginSettings").Get<PluginSettings>();
    if (pluginSettings != null && File.Exists(pluginSettings.AssemblyPath))
    {

        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(pluginSettings.AssemblyPath));

        var pluginDir = Path.GetDirectoryName(pluginSettings.AssemblyPath);

        AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
        {
            // 1. Force use of Host's version for ANY Infrastructure DLLs
            if (assemblyName.Name.StartsWith("System.") ||
                assemblyName.Name.StartsWith("Microsoft.") ||
                assemblyName.Name == "Newtonsoft.Json")
            {
                return null; // Returning null tells .NET "Look in the Host's bin folder instead"
            }

            // 2. Check if already loaded (The Bridge)
            var alreadyLoaded = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
            if (alreadyLoaded != null) return alreadyLoaded;

            // 3. Only load your custom logic from the Plugins folder
            string path = Path.Combine(pluginDir!, $"{assemblyName.Name}.dll");
            if (File.Exists(path))
            {
                return context.LoadFromAssemblyPath(path);
            }

            return null;
        };

        var pluginType = assembly.GetType(pluginSettings.PluginClassName);

        if (pluginType != null)
        {
            metadata.PluginType = pluginType;
            builder.Services.AddSingleton(metadata);

            // Register the type itself so DI knows how to create it
            if (metadata.PluginType != null) builder.Services.AddScoped(metadata.PluginType);
        }
    }
}


async static Task AddKernel(WebApplicationBuilder builder, PluginMetadata metadata)
    {
        builder.Services.AddScoped<Kernel>(sp =>
        {
            // Use a unique ID to track this specific resolution in the console
            var requestId = Guid.NewGuid().ToString().Substring(0, 4);
            Console.WriteLine($"[WhatsAppToDB] [{requestId}] Building Kernel...");

            var aiSettings = sp.GetRequiredService<IOptions<OpenAiSettings>>().Value;
            var kernelBuilder = Kernel.CreateBuilder();

            kernelBuilder.AddOpenAIChatCompletion(aiSettings.Model, aiSettings.ApiKey);

            var dbPlugin = sp.GetRequiredService<DatabaseQueryPlugin>();
            kernelBuilder.Plugins.AddFromObject(dbPlugin);

            // Dynamic Plugin
            if (metadata.PluginType != null)
            {
                var helper = sp.GetRequiredService(metadata.PluginType);
                kernelBuilder.Plugins.AddFromObject(helper);
            }

            return kernelBuilder.Build();
        });
    }

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

            await waService.SendWhatsAppResponse(senderPhone, "_Analyzing your request and querying SAP... Please wait a moment._ 🔍", waSettings, waLogger);
            var kernel = sp.GetRequiredService<Kernel>();
            var aiOptions = sp.GetRequiredService<IOptions<OpenAiSettings>>();
            var history = new ChatHistory();
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var systemPrompt = aiOptions.Value.FullSystemPrompt;

            history.AddSystemMessage(systemPrompt);

            history.AddUserMessage(messageText);            
            kernel.Data["WhatsAppNumber"] = senderPhone;
            kernel.Data["UserQuestion"] = messageText;
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



