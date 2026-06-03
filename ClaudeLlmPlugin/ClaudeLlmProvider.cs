using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.LlmProviders;

// Resolve ambiguous references
using AnthropicTool = Anthropic.SDK.Common.Tool;
using AnthropicMessage = Anthropic.SDK.Messaging.Message;
using AnthropicTextContent = Anthropic.SDK.Messaging.TextContent;
using AnthropicToolResult = Anthropic.SDK.Messaging.ToolResultContent;

namespace ClaudeLlmPlugin
{
    public class ClaudeLlmProvider : ILlmProvider
    {
        private AnthropicClient _client;
        private string _model;

        private readonly IServiceProvider _sp;
        private static string _apiKey;
        private static string _defaultModel;

        public string Name => "Claude";
        public bool SupportsKernel => false;

         // Inject IServiceProvider so we can resolve config later
        public ClaudeLlmProvider(IServiceProvider sp)
        {
            _sp = sp;
        }

        public void Register(IKernelBuilder builder,
            LlmConfig config,
            string model)
        {

            _apiKey = config.ApiKey ?? throw new ArgumentNullException(nameof(config.ApiKey), "API key is required for ClaudeLlmProvider");
            _defaultModel = model ; // default to Claude 2
                
            _model = model;
            _client = new AnthropicClient(config.ApiKey);

            var chatCompletionService = new ChatClientBuilder(_client.Messages)
                .UseFunctionInvocation()
                .ConfigureOptions(o => o.ModelId = model)
                .Build()
                .AsChatCompletionService();

            builder.Services.AddSingleton<IChatCompletionService>(chatCompletionService);
        }

        public async Task<string> GenerateAsync(
            ChatHistory history,
            string model,
            Kernel kernel)
        {
            if (_apiKey == null)
            throw new InvalidOperationException(
                "ClaudeLlmProvider.Register must be called before GenerateAsync");
            // Resolve config from DI — works even on a fresh instance
            _client = new AnthropicClient(_apiKey);
            _model = model ?? _defaultModel;
            
            // 1. Build tools from SK kernel plugins
            var tools = kernel?.Plugins
                .SelectMany(p => p.GetFunctionsMetadata()
                    .Select(f =>
                    {
                        var inputSchema = JsonNode.Parse(BuildSchema(f));
                        return new Anthropic.SDK.Common.Tool(
                            new Anthropic.SDK.Common.Function(
                                $"{p.Name}__{f.Name}",
                                f.Description ?? f.Name,
                                inputSchema));
                    }))
                .ToList();

            // 2. Convert SK ChatHistory to Anthropic messages
            var messages = history
                .Where(m => m.Role != AuthorRole.System)
                .Select(m => new AnthropicMessage(
                    m.Role == AuthorRole.User ? RoleType.User : RoleType.Assistant,
                    m.Content ?? ""))
                .ToList();

            var systemPrompt = string.Join("\n", history
                .Where(m => m.Role == AuthorRole.System)
                .Select(m => m.Content));

            var identity = kernel?.Data.ContainsKey("UserIdentity") == true ? kernel.Data["UserIdentity"] as IdentityContext : null;
            var isEval = identity?.IsEvalRequest == true;

            // 3. Agentic loop
            while (true)
            {
                var parameters = new MessageParameters
                {
                    Messages   = messages,
                    Model      = model ?? _model,
                    MaxTokens  = 4096,
                    Stream     = false,
                    Temperature = isEval ? 0.0m : 1.0m,
                    System     = string.IsNullOrEmpty(systemPrompt)
                                    ? null
                                    : new List<SystemMessage> { new SystemMessage(systemPrompt) },
                    Tools      = tools.Any() ? tools : null
                };

                var response = await _client.Messages.GetClaudeMessageAsync(parameters);
                messages.Add(response.Message);

                // 4. No tool calls → return final text
                if (response.ToolCalls == null || !response.ToolCalls.Any())
                    return response.Message.ToString() ?? "";

                // 5. Invoke each tool call and add result using SDK's built-in pattern
               foreach (var toolCall in response.ToolCalls)
                {
                    var toolUseContent = response.Content
                        .OfType<ToolUseContent>()
                        .FirstOrDefault(t => t.Id == toolCall.Id);

                    if (toolUseContent == null) continue;

                    // Parse "PluginName__FunctionName"
                    var parts        = toolUseContent.Name.Split("__", 2);
                    var pluginName   = parts[0];
                    var functionName = parts.Length > 1 ? parts[1] : parts[0];

                    string toolResult;
                    try
                    {
                        var args = new KernelArguments();
                        if (toolUseContent.Input != null)
                        {
                            foreach (var kvp in toolUseContent.Input.AsObject())
                                args[kvp.Key] = kvp.Value?.ToString();
                        }

                        var result = await kernel.InvokeAsync(pluginName, functionName, args);
                        toolResult = result?.ToString() ?? "";
                    }
                    catch (Exception ex)
                    {
                        toolResult = $"Error: {ex.Message}";
                    }

                    messages.Add(new AnthropicMessage
                    {
                        Role = RoleType.User,
                        Content = new List<ContentBase>
                        {
                            new AnthropicToolResult
                            {
                                ToolUseId = toolUseContent.Id,
                                Content   = new List<ContentBase>
                                {
                                    new AnthropicTextContent { Text = toolResult }
                                }
                            }
                        }
                    });

                }
            }
        }

        // Wrap a KernelFunction as a Func<string> delegate for Tool.FromFunc
        private Func<string, string> BuildDelegate(
            KernelFunctionMetadata meta,
            string pluginName,
            Kernel kernel)
        {
            return (input) =>
            {
                // Run sync; for async plugins consider .GetAwaiter().GetResult()
                var args = new KernelArguments();
                // Single-param shortcut — put input as first param
                var firstParam = meta.Parameters.FirstOrDefault();
                if (firstParam != null)
                    args[firstParam.Name] = input;

                var result = kernel.InvokeAsync(pluginName,
                                                meta.Name,
                                                args)
                                   .GetAwaiter()
                                   .GetResult();
                return result?.ToString() ?? "";
            };
        }

        public Task<string> GenerateAsync(ChatHistory history, string model)
            => GenerateAsync(history, model, null!);

        private string BuildSchema(KernelFunctionMetadata f)
        {
            var props = f.Parameters.ToDictionary(
                p => p.Name,
                p =>
                {
                    var typeName = p.ParameterType?.Name?.ToLower() ?? "string";
                    var jsonType = typeName switch
                    {
                        "int32" or "int64" or "int" => "integer",
                        "double" or "float" or "decimal" => "number",
                        "boolean" or "bool" => "boolean",
                        _ => "string"
                    };
                    return (object)new { type = jsonType, description = p.Description ?? p.Name };
                });

            var required = f.Parameters
                .Where(p => p.IsRequired)
                .Select(p => p.Name)
                .ToList();

            return JsonSerializer.Serialize(new { type = "object", properties = props, required });
        }
    }

    public class ClaudeConfig
    {
        public string ApiKey { get; set; }
        public string Model  { get; set; }
    }
}