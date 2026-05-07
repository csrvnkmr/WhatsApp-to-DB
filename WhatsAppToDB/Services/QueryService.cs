using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Vml.Office;
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
        public QueryService(DatabaseRegistry dbRegistry, DbProviderFactory dbFactory,
                IHttpContextAccessor http, LlmContextService llmContext)
        {
            _dbRegistry = dbRegistry;
            _dbFactory = dbFactory;
            _http = http;
            _llmContext = llmContext;
        }

        public async Task<ChatMessageDto> ExecuteQuery(IServiceScopeFactory scopeFactory,
            IdentityContext identity, string messageText, PromptExecutionSettings? pes, ILogger waLogger,
            ChatDbRepository repo, long sessionid)
        {
            WhatsAppSettings? waSettings = null;
            try
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var dbName = _http.HttpContext?.Session?.GetString("activeDb") ?? "chinook-sqlite";

                    var dbConfig = _dbRegistry.GetDatabaseConfig(dbName);
                    var sessionId = _http.HttpContext?.Session?.Id;

                    var schema = File.ReadAllText(dbConfig.SchemaFile);
                    var prompt = File.ReadAllText(dbConfig.PromptFile);
                    Console.WriteLine($"[QUERY] Session={sessionId}");
                    Console.WriteLine($"[QUERY] DB={dbName}");
                    var sp = scope.ServiceProvider;
                    var waOptions = sp.GetRequiredService<IOptions<WhatsAppSettings>>();
                    waSettings = waOptions.Value; // Capture the actual settings object

                    var identityService = sp.GetRequiredService<IIdentityService>();
                    //var identity = await identityService.GetIdentityAsync(usernameorphonenumber);
                    var kernel = sp.GetRequiredService<Kernel>();
                    var aiOptions = sp.GetRequiredService<IOptions<CommonAiSettings>>();

                    var ctx = sp.GetRequiredService<AiRequestContext>();
                    if (identity!=null)
                    {
                        identityService.HydrateRolePermissions(identity);
                    }
                    ctx.Identity = identity;
                    ctx.UserQuestion = messageText;
                    ctx.WhatsAppNumber = identity.WhatsAppNumber;
                    ctx.SessionId = sessionid;
                    

                    kernel.Data["UserIdentity"] = identity;
                    kernel.Data["WhatsAppNumber"] = identity.WhatsAppNumber;
                    kernel.Data["UserQuestion"] = messageText;

                    var history = new ChatHistory();
                    var systemPrompt = prompt; //aiOptions.Value.FullSystemPrompt;

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
                    await repo.InsertMessageAsync(sessionid, "User", messageText, "", "", dbName, provider.Name,model, ctx.ModuleName);
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
                            model);
                    }

                    //history.Add(aiResponse);
                    history.AddAssistantMessage(aiContent);
                    var sql = ctx.LastExecutedSql;
                    var moduleName = ctx.ModuleName;
                    var datafilepath = ctx.DataFileName;
                    var msgid = await repo.InsertMessageAsync(sessionid, "Assistant", aiContent, sql, datafilepath, dbName, 
                        provider.Name, model, moduleName);
                    await waLogger.LogAsync(identity.WhatsAppNumber, $"Sending response to {identity.WhatsAppNumber} {aiContent}");
                    var response = new ChatMessageDto
                    {
                        Id = msgid,
                        MessageText = aiContent,
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
                await waLogger.LogAsync(identity.WhatsAppNumber, "Exception when querying and sending message " + ex.ToString());
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
