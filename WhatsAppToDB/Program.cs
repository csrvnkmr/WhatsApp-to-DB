using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NPOI.OpenXmlFormats.Spreadsheet;
using NPOI.SS.Formula.PTG;
using System.IO.Enumeration;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Runtime.Loader;
using System.Text.Json;
using WhatsAppToDB;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;
using WhatsAppToDB.Models;

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


//await TestSqliteExecution();

//await AdventureWorksTestHarness.RunTestMessages(builder);

//await AdventureWorksTestHarness.RunSecurityTests(builder);
var app = builder.Build();
app.UseCors("AllowAll");
using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<ChatDbRepository>();
    await repo.InitializeAsync();
}
app.UseMiddleware<TokenAuthMiddleware>();

app.UseStaticFiles(); // This will serve index.html if it's in a folder named wwwroot


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
    IWhatsAppLogger waLogger,
    IOptions<WhatsAppSettings> waOptions, ChatDbRepository repo) =>
{

    Console.WriteLine("Webhook received a message at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    var waSettings = waOptions.Value;
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

    var scope = scopeFactory.CreateScope();
    var sp = scope.ServiceProvider;
    var identityService = sp.GetRequiredService<IIdentityService>();
    var identity = await identityService.GetIdentityAsync(senderPhone);

    _ = Task.Run(async () =>
    {
        await waService.SendWhatsAppResponse(senderPhone, "_Analyzing your request and querying database... Please wait a moment._ 🔍", waSettings, waLogger);
        var response = await ExecuteQuery(scopeFactory, identity, messageText, openAIPromptExecutionSettings, waLogger, repo, -1);
        await waService.SendWhatsAppResponse(senderPhone, response.MessageText, waSettings, waLogger);
    });

    return Results.Ok();

});

app.MapPost("/login", async (LoginRequest request) =>
{
    var result = UserService.ValidateLogin(request.Username, request.Password);
    if (!result.isSuccess)
    {
        return Results.Unauthorized();
    }
    return Results.Ok(new { Token = result.session.Token, Message = "Login Successful" });
});

app.MapPost("/ask", async (
    AskRequest request,
    IServiceScopeFactory scopeFactory,
    IWhatsAppLogger waLogger, HttpContext ctx, ChatDbRepository repo) =>
{
    var scope = scopeFactory.CreateScope();
    var sp = scope.ServiceProvider;
    var identityService = sp.GetRequiredService<IIdentityService>();

    var userName =
        ctx.Items["UserName"]?.ToString() ?? "";
    var result = UserService.ValidateUserName(userName);
    if (!result.isSuccess) { return Results.Unauthorized(); }
    long sessionid = 0;
    if (request.SessionId.HasValue && request.SessionId.Value > 0)
    {
        sessionid = request.SessionId.Value;
    } else
    {
       sessionid= await repo.CreateSessionAsync(userName, request.Question);
    }

    var identity = result.identity;
    identityService.HydrateRolePermissions(identity);

    // We run this and wait for result for the Web API response
    var aiResponse = await ExecuteQuery(scopeFactory, identity, request.Question, openAIPromptExecutionSettings, waLogger, repo, sessionid);

    //return Results.Ok(new { sessionId = sessionid, Response = aiResponse });
    return Results.Ok(aiResponse);
});

app.MapGet("/session", async (ChatDbRepository repo, HttpContext ctx) =>
{
    var userName =
        ctx.Items["UserName"]?.ToString() ?? "";

    var rows =
        await repo.GetSessionsAsync(userName);

    return Results.Ok(rows);
});

app.MapGet("/message/{sessionId}",
async (
    long sessionId,
    ChatDbRepository repo,
    HttpContext ctx) =>
{
    var userName =
        ctx.Items["UserName"]?.ToString() ?? "";

    // Optional ownership check here

    var rows =
        await repo.GetMessagesAsync(sessionId);

    return Results.Ok(rows);
});

app.MapGet("/messagesql/{messageId:long}",
async (
    long messageId,
    HttpContext ctx,
    ChatDbRepository repo) =>
{
    // user already validated by middleware
    var userName =
        ctx.Items["UserName"]?.ToString() ?? "";

    // ownership + data
    var row =
        await repo.GetMessageExtrasAsync(
            messageId,
            userName);

    if (row == null)
        return Results.NotFound("Message not found.");

    if (!row.CanShowSql)
        return Results.Forbid();

    return Results.Ok(new
    {
        messageId = row.Id,
        sql = row.SqlText ?? ""
    });
});


app.MapGet("/messagedata/{messageId:long}",
async (
    long messageId,
    HttpContext ctx,
    ChatDbRepository repo) =>
{
    var userName =
        ctx.Items["UserName"]?.ToString() ?? "";

    var row =
        await repo.GetMessageExtrasAsync(
            messageId,
            userName);

    if (row == null)
        return Results.NotFound("Message not found.");

    if (!row.CanShowData)
        return Results.Forbid();

    if (string.IsNullOrWhiteSpace(row.DataFileName))
        return Results.NotFound("Data file missing.");

    var folder =
        Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "Results");

    var filePath =
        Path.Combine(
            folder,
            row.DataFileName);

    if (!File.Exists(filePath))
        return Results.NotFound("Data file not found.");

    var json =
        await File.ReadAllTextAsync(filePath);

    // return raw json rows
    return Results.Content(
        json,
        "application/json");
});

// ==========================================================
// POST /emailresult
// Requires Bearer token via middleware
// ==========================================================
app.MapPost("/emailresult",
async (
    EmailRequest request,
    HttpContext ctx,
    IOptions<MailSettings> mailOptions,
    ChatDbRepository repo) =>
{
    try
    {
        var userName =
            ctx.Items["UserName"]?.ToString() ?? "";

        // Optional ownership validation
        var msg =
            await repo.GetMessageExtrasAsync(
                request.MessageId,
                userName);

        if (msg == null)
            return Results.NotFound(
                "Message not found.");

        var settings =
            mailOptions.Value;

        using var mail =
            new MailMessage();

        // ---------------------------------
        // FROM
        // ---------------------------------
        mail.From =
            new MailAddress(settings.UserName, request.From);

        // ---------------------------------
        // TO (supports comma separated)
        // ---------------------------------
        foreach (var item in request.To.Split(
                     ',',
                     StringSplitOptions.RemoveEmptyEntries))
        {
            mail.To.Add(item.Trim());
        }

        // ---------------------------------
        // CC
        // ---------------------------------
        if (!string.IsNullOrWhiteSpace(request.Cc))
        {
            foreach (var item in request.Cc.Split(
                         ',',
                         StringSplitOptions.RemoveEmptyEntries))
            {
                mail.CC.Add(item.Trim());
            }
        }

        // ---------------------------------
        // SUBJECT / BODY
        // ---------------------------------
        mail.Subject =
            request.Subject;

        mail.Body =
            request.Body;

        mail.IsBodyHtml = false;

        // ---------------------------------
        // SMTP
        // ---------------------------------
        using var client = new SmtpClient(settings.SmtpServer, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            Credentials = new NetworkCredential(settings.UserName, settings.Password)
        };
        await client.SendMailAsync(mail);       

        return Results.Ok(new
        {
            success = true,
            message = "Email sent successfully."
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new
        {
            success = false,
            message = ex.Message
        });
    }
});

app.Run();



async static Task<ChatMessageDto> ExecuteQuery(IServiceScopeFactory scopeFactory,
    IdentityContext identity, string messageText, PromptExecutionSettings? pes, IWhatsAppLogger waLogger,
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

async Task TestEmail()
{
    var er = new EmailRequest();
    er.From = "Saravana - AI";
    er.To = "srvnkmr@gmail.com";
    er.Subject = "Test mail";
    er.Body = "Test";
    er.Cc = "srvnkmr@hotmail.com";
    
}

async Task TestSqliteExecution()
{
    var repo = new WhatsAppToDB.Tests.SqliteRepositoryTests();
    await repo.Full_Sqlite_Test_Create_Insert_Select_Delete();
}

 