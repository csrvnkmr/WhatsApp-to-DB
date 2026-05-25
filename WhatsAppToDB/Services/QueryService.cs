using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Vml.Office;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Models.CallRecords;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;
using WhatsAppToDB.Database;
using WhatsAppToDB.LlmProviders;
using WhatsAppToDB.Models;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.Services
{



    public class QueryService : IQueryService
    {

        private readonly DatabaseRegistry _dbRegistry;
        private readonly DbProviderFactory _dbFactory;
        private readonly IHttpContextAccessor _http;
        private readonly LlmContextService _llmContext;
        private readonly JsonConfigService _jsonConfigService;

        public QueryService(DatabaseRegistry dbRegistry, DbProviderFactory dbFactory,
                IHttpContextAccessor http, LlmContextService llmContext, JsonConfigService jsonConfigService)
        {
            _dbRegistry = dbRegistry;
            _dbFactory = dbFactory;
            _http = http;
            _llmContext = llmContext;
            _jsonConfigService = jsonConfigService;
        }

        public async Task<ChatMessageDto> ExecuteQuery(IServiceScopeFactory scopeFactory,
            IdentityContext identity, string messageText, PromptExecutionSettings? pes, ILogger waLogger,
            ChatDbRepository repo, long sessionid)
        {
            //WhatsAppSettings? waSettings = null;
            try
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    //var dbName = _http.HttpContext?.Session?.GetString(Constants.SessionKeys.ActiveDb) ?? "chinook-sqlite";

                    //var dbName = httpContext.Session.GetString(Constants.SessionKeys.ActiveDb);
                    var dbName = identity.Database;

                    if (string.IsNullOrWhiteSpace(dbName))
                    {
                        throw new Exception(
                            $"[ExecuteQuery] ActiveDb is NULL/EMPTY. ");                   }

                    var execContext =
                        scope.ServiceProvider.GetRequiredService<ExecutionContextService>();

                    execContext.SetFromIdentity(identity);

                    var dbConfig = _dbRegistry.GetDatabaseConfig(dbName);

                    //var schema = File.ReadAllText(dbConfig.SchemaFile);
                    //var prompt = File.ReadAllText(dbConfig.PromptFile);

                    //Console.WriteLine($"[QUERY] Session={sessionId}");
                    Console.WriteLine($"[QUERY] DB={dbName}");
                    var sp = scope.ServiceProvider;
                    //var waOptions = sp.GetRequiredService<IOptions<WhatsAppSettings>>();
                    //waSettings = waOptions.Value; // Capture the actual settings object

                    var identityService = sp.GetRequiredService<IIdentityService>();
                    //var identity = await identityService.GetIdentityAsync(usernameorphonenumber);
                    var kernel = sp.GetRequiredService<Kernel>();

                    var ctx = sp.GetRequiredService<AiRequestContext>();
                    if (identity!=null)
                    {
                        identityService.HydrateRolePermissions(identity);
                    }
                    ctx.Identity = identity;
                    ctx.UserQuestion = messageText;
                    ctx.UserName = identity.UserName;
                    ctx.SessionId = sessionid;
                    var modules = ctx.ModuleName;
                    var modulePrompt = "";
                    if (!string.IsNullOrEmpty(modules))
                    {
                        var lstmodules = modules.Split(',').ToList();
                        var allmodules = _jsonConfigService.GetModules(dbName);
                        foreach (var modulename in lstmodules)
                        {
                            var currmodule = allmodules.Find(x => x.Name == modulename);
                            if (currmodule != null && !string.IsNullOrWhiteSpace(currmodule.Prompt))
                            {
                                modulePrompt += currmodule.Prompt + "\n\n";
                            }                               
                        }
                    }

                    kernel.Data["UserIdentity"] = identity;
                    kernel.Data[Constants.ContextItems.WhatsAppNumber] = identity.WhatsAppNumber;
                    kernel.Data[Constants.ContextItems.UserName] = identity.WhatsAppNumber;
                    kernel.Data["UserQuestion"] = messageText;

                    var history = new ChatHistory();
                    var prompt = _jsonConfigService.GetPrompt(dbName);
                    var systemPrompt = prompt; //aiOptions.Value.FullSystemPrompt;
                    if (!string.IsNullOrEmpty(modulePrompt))
                    {
                        systemPrompt = systemPrompt + "\n\n" + modulePrompt;
                    }

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
                    var provider = _llmContext.GetProvider();
                    string aiContent;
                    var model = _llmContext.GetModel();
                    await repo.InsertMessageAsync(sessionid, "User", messageText, "", "", 
                        dbName, provider.Name,model, ctx.ModuleName);
                    
                    

                    if (kernel.FunctionInvocationFilters?.Count == 0)
                    {
                        kernel.FunctionInvocationFilters.Add(new FunctionCallLogger());
                    }

                    if (provider.SupportsKernel)
                    {
                        var chatService = kernel.GetRequiredService<IChatCompletionService>();
                        var aiResponse = await chatService.GetChatMessageContentAsync(history,
                                    executionSettings: pes, //openAIPromptExecutionSettings,
                                    kernel: kernel
                                    );
                        aiContent = aiResponse.Content ?? "";
                    } else
                    {
                        aiContent = await provider.GenerateAsync(
                            history,
                            model, kernel);
                    }
                    Console.WriteLine($"[QUERY] [{identity.UserName}] AI Response received for {messageText}");
                    var whatsAppReplyText = aiContent;
                    var isWhatsAppRequest = identity != null && identity.IsWhatsAppRequest;
                    if (!string.IsNullOrEmpty(aiContent)    )
                    {
                        var parsedResponse = ParsedAiResponse.ParseAiResponse(aiContent);
                        if (parsedResponse.ChartConfig != null && parsedResponse.ChartData != null)
                        {
                            

                            if (isWhatsAppRequest)
                            {
                                whatsAppReplyText = parsedResponse.AnalysisText;
                                // For WhatsApp, we can only send text, so we can serialize the chart config and data as JSON strings
                                // and include them in the response text with special markers
                            } 

                            var chartConfigJson = System.Text.Json.JsonSerializer.Serialize(parsedResponse.ChartConfig);
                            var chartDataJson = System.Text.Json.JsonSerializer.Serialize(parsedResponse.ChartData);
                            var finalContent = new {
                                analysis_text = parsedResponse.AnalysisText, // Use the analysis text as the main content
                                chart_config = chartConfigJson,
                                chart_data = chartDataJson
                            };           
                            // aiContent = parsedResponse.AnalysisText; // Use the analysis text as the main content
                            // aiContent += $"\n\n[CHART_CONFIG]{chartConfigJson}[/CHART_CONFIG]";
                            // aiContent += $"\n\n[CHART_DATA]{chartDataJson}[/CHART_DATA]";
                            aiContent = System.Text.Json.JsonSerializer.Serialize(finalContent); // Serialize the entire content as JSON
                            
                        }
                    }
                    //history.Add(aiResponse);
                    history.AddAssistantMessage(aiContent);
                    var sql = ctx.LastExecutedSql;
                    var moduleName = ctx.ModuleName;
                    var datafilepath = ctx.DataFileName;
                    var msgid = await repo.InsertMessageAsync(sessionid, "Assistant", aiContent, sql, datafilepath, dbName, 
                        provider.Name, model, moduleName);
                    await waLogger.LogAsync(identity.UserName, $"Sending response to {identity.UserName} {aiContent}");
                    var response = new ChatMessageDto
                    {
                        Id = msgid,
                        MessageText = (isWhatsAppRequest ? whatsAppReplyText : aiContent),
                        CanShowSql = ctx.ShowSql,
                        CanShowData = ctx.ShowData,
                        CanShowChart = ctx.ShowChart,
                        SessionId = ctx.SessionId
                    };
                    return response;

                }
            }
            catch (Exception ex)
            {
                await waLogger.LogAsync(identity.UserName, "Exception when querying and sending message " + ex.ToString());
                Console.WriteLine($"Background Error: {ex}");
                var errmsg = "Sorry, I encountered an error while accessing Database. Please try again.";
                var response = new ChatMessageDto
                {
                    MessageText = errmsg,
                    CanShowSql = false,
                    CanShowData = false,
                    CanShowChart = false
                };
                return response;
            }
        }
    }
}
