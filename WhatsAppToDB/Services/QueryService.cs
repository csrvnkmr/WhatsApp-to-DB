using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;
using WhatsAppToDB.Models;

namespace WhatsAppToDB.Services
{
    public class QueryService : IQueryService
    {
        public async Task<ChatMessageDto> ExecuteQuery(IServiceScopeFactory scopeFactory,
            IdentityContext identity, string messageText, PromptExecutionSettings? pes, ILogger waLogger,
            ChatDbRepository repo, long sessionid)
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

                    await repo.InsertMessageAsync(sessionid, "User", messageText, "", "");
                    history.AddSystemMessage(systemPrompt);

                    history.AddUserMessage(messageText);
                    var aiResponse = await chatService.GetChatMessageContentAsync(history,
                                executionSettings: pes, //openAIPromptExecutionSettings,
                                kernel: kernel
                                );
                    history.Add(aiResponse);
                    var sql = ctx.LastExecutedSql;
                    var datafilepath = ctx.DataFileName;
                    var msgid = await repo.InsertMessageAsync(sessionid, "Assistant", aiResponse.Content, sql, datafilepath);
                    await waLogger.LogAsync(identity.WhatsAppNumber, $"Sending response to {identity.WhatsAppNumber} {aiResponse.Content}");
                    var response = new ChatMessageDto
                    {
                        Id = msgid,
                        MessageText = aiResponse.Content,
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
