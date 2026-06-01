// ==========================================================
// Controllers/ChatController.cs
// ==========================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Win32;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Threading;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Audit;
using WhatsAppToDB.Data;
using WhatsAppToDB.Database;
using WhatsAppToDB.LlmProviders;
using WhatsAppToDB.Models;
using WhatsAppToDB.Services;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.Controllers
{
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _waLogger;
        private readonly ChatDbRepository _repo;
        private readonly PromptExecutionSettings _promptSettings;
        private readonly IQueryService _queryService;
        private readonly IIdentityContextEnricher _identityContextEnricher;
        private readonly JsonConfigService _jsonConfigService;
        private readonly DatabaseContextService _databaseContextService;
        private readonly LlmCancellationService _cancellationService;


        public ChatController(
            IServiceScopeFactory scopeFactory,
            ILogger waLogger, ChatDbRepository repo, JsonConfigService jsonConfigService,
            IQueryService queryService, IIdentityContextEnricher identityContextEnricher, 
            DatabaseContextService databaseContextService,
            LlmCancellationService cancellationService)
        {
            _scopeFactory = scopeFactory;
            _waLogger = waLogger;
            _repo = repo;
            _jsonConfigService = jsonConfigService;
            _identityContextEnricher = identityContextEnricher;
            _promptSettings =
                new OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior =
                        FunctionChoiceBehavior.Auto()
                };
            _queryService = queryService;
            _databaseContextService = databaseContextService;
            _cancellationService = cancellationService;
        }
       

        [HttpGet("stream/{requestId}")]
        public async Task StreamProgress(string requestId, CancellationToken ct)
        {
            Response.Headers["Content-Type"]  = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no"; // important for nginx

            if (!ProgressStore.TryGet(requestId, out var channel))
            {
                await Response.WriteAsync("event: error\ndata: {\"message\":\"Not found\"}\n\n");
                return;
            }

            await foreach (var evt in channel.Reader.ReadAllAsync(ct))
            {
                var json = JsonSerializer.Serialize(evt);
                await Response.WriteAsync($"event: progress\ndata: {json}\n\n");
                await Response.Body.FlushAsync(ct);

                if (evt.Phase == "done" || evt.Phase == "error") break;
            }
        }

        [HttpPost("dontuse1")]
        public async Task<IActionResult> Asknew([FromBody] AskRequest request)
        {
            var userName = HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? "";
            var result = UserService.ValidateUserName(userName);

            if (!result.isSuccess)
                return Unauthorized();

            long sessionId = 0;
            if (request.SessionId.HasValue && request.SessionId.Value > 0)
                sessionId = request.SessionId.Value;
            else
                sessionId = await _repo.CreateSessionAsync(userName, request.Question);

            var identity = result.identity;
            _identityContextEnricher.EnrichFromHttpContext(identity, HttpContext);

            // Generate a requestId and register a channel for SSE streaming
            var requestId = Guid.NewGuid().ToString();
            var progressChannel = new ProgressChannel();
            ProgressStore.Register(requestId, progressChannel);

            var cts = new CancellationTokenSource();
            _cancellationService.Register(userName, cts);

            // Fire the query in background — SSE stream runs concurrently
            _ = Task.Run(async () =>
            {
                try
                {
                    await _queryService.ExecuteQuery(
                        _scopeFactory,
                        identity,
                        request.Question,
                        _promptSettings,
                        _waLogger,
                        _repo,
                        sessionId,
                        cts.Token);
                        //,                        progressChannel);   // ← pass the channel
                }
                catch (Exception ex)
                {
                    await progressChannel.Writer.WriteAsync(new ProgressEvent
                    {
                        Phase = "error",
                        Message = "❌ Unexpected error: " + ex.Message
                    });
                }
                finally
                {
                    progressChannel.Complete();
                    _cancellationService.Remove(userName);
                    // Clean up after a delay to allow SSE client to finish reading
                    _ = Task.Delay(TimeSpan.FromSeconds(30))
                            .ContinueWith(_ => ProgressStore.Remove(requestId));
                }
            }, cts.Token);

            // Return requestId immediately — Vue opens SSE with this ID
            return Ok(new { requestId, sessionId });
        }


        [HttpPost("/eval")]
        public async Task<IActionResult> AskEval(AskRequest request)
        {
            AskRequest askRequest = new AskRequest(request.Question, request.SessionId, true);
            return await Ask(askRequest);
        }

        [HttpPost("/ask")]
        public async Task<IActionResult> Ask(
            [FromBody] AskRequest request)
        {
            var userName =
                HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? "";
            var result =
                UserService.ValidateUserName(userName);

            if (!result.isSuccess)
                return Unauthorized();

            long sessionId = 0;

            if (request.SessionId.HasValue
                && request.SessionId.Value > 0)
            {
                sessionId =
                    request.SessionId.Value;
            }
            else
            {
                sessionId =
                    await _repo.CreateSessionAsync(
                        userName,
                        request.Question);
            }

            var identity =
                result.identity;
            _identityContextEnricher.EnrichFromHttpContext(
                identity,
                HttpContext);
            if (request.isEval)
            {
                identity.IsEvalRequest = true;
            }

            using var cts = new CancellationTokenSource();
            _cancellationService.Register(userName, cts);
            try
            {
                var response =
                    await _queryService.ExecuteQuery(
                        _scopeFactory,
                        identity,
                        request.Question,
                        _promptSettings,
                        _waLogger,
                        _repo,
                        sessionId,
                        cts.Token);

                return Ok(response);
            }
            finally
            {
                _cancellationService.Remove(userName);
            }
        }

        // ==================================================
        // GET /session
        // ==================================================
        [HttpGet("/session")]
        public async Task<IActionResult> GetSessions()
        {
            var userName =
                HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? "";
            _waLogger.LogInfo($"[ChatController] Getting sessions for user {userName}");
            var rows =
                await _repo.GetSessionsAsync(userName);

            return Ok(rows);
        }

        // ==================================================
        // GET /message/{sessionId}
        // ==================================================
        [HttpGet("/message/{sessionId}")]
        public async Task<IActionResult> GetMessages(
            long sessionId)
        {
            var rows =
                await _repo.GetMessagesAsync(sessionId);

            return Ok(rows);
        }

        // ==================================================
        // GET /messagesql/{messageId}
        // ==================================================
        [HttpGet("/messagesql/{messageId}")]
        public async Task<IActionResult> GetSql(
            long messageId)
        {
            var userName =
                HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? "";

            var row =
                await _repo.GetMessageExtrasAsync(
                    messageId,
                    userName);

            if (row == null)
                return NotFound();

            if (!row.CanShowSql)
                return Forbid();

            return Ok(new
            {
                messageId = row.Id,
                sql = row.SqlText
            });
        }

        // ==================================================
        // GET /messagedata/{messageId}
        // ==================================================
        [HttpGet("/messagedata/{messageId}")]
        public async Task<IActionResult> GetData(
            long messageId)
        {
            var userName =
                HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? "";

            var row =
                await _repo.GetMessageExtrasAsync(
                    messageId,
                    userName);

            if (row == null)
                return NotFound();

            if (!row.CanShowData)
                return Forbid();

            var filePath =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "Data",
                    "Results",
                    row.DataFileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var json =
                await System.IO.File.ReadAllTextAsync(
                    filePath);

            return Content(
                json,
                "application/json");
        }

        // ==================================================
        // POST /emailresult
        // ==================================================
        [HttpPost("/emailresult")]
        public async Task<IActionResult> EmailResult(
            [FromBody] EmailRequest request)
        {
            try
            {
                var userName =
                    HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? "";

                var msg =
                    await _repo.GetMessageExtrasAsync(
                        request.MessageId,
                        userName);

                if (msg == null)
                    return NotFound();

                var dbName = _databaseContextService.GetCurrentDatabaseName();

                var settings = _jsonConfigService.GetMailSettings(dbName);

                using var mail =
                    new MailMessage();

                mail.From =
                    new MailAddress(
                        settings.UserName,
                        request.From);

                foreach (var item in request.To.Split(
                             ',',
                             StringSplitOptions.RemoveEmptyEntries))
                {
                    mail.To.Add(item.Trim());
                }

                if (!string.IsNullOrWhiteSpace(request.Cc))
                {
                    foreach (var item in request.Cc.Split(
                                 ',',
                                 StringSplitOptions.RemoveEmptyEntries))
                    {
                        mail.CC.Add(item.Trim());
                    }
                }

                mail.Subject =
                    request.Subject;

                mail.Body =
                    request.Body;

                mail.IsBodyHtml = true;

                if (!string.IsNullOrWhiteSpace(request.ChartImage))
                {
                    var base64Data = request.ChartImage;
                    if (base64Data.Contains(","))
                        base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);

                    var imageBytes = Convert.FromBase64String(base64Data);
                    var imageStream = new MemoryStream(imageBytes);

                    var attachment = new System.Net.Mail.Attachment(imageStream, "chart.png", "image/png");
                    attachment.ContentId = "chartimage";
                    attachment.ContentDisposition.Inline = true;
                    mail.Attachments.Add(attachment);
                }

                using var client =
                    new SmtpClient(
                        settings.SmtpServer,
                        int.Parse(settings.Port))
                    {
                        EnableSsl =
                            settings.EnableSsl,

                        Credentials =
                            new NetworkCredential(
                                settings.UserName,
                                settings.Password)
                    };

                await client.SendMailAsync(mail);

                return Ok(new
                {
                    success = true,
                    message = "Email sent successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("/exportdata/{messageId}")]
        public async Task<IActionResult> ExportData(long messageId)
        {
            var userName =
                HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? "";

            var row =
                await _repo.GetMessageExtrasAsync(
                    messageId,
                    userName);

            if (row == null)
                return NotFound();

            if (!row.CanShowData)
                return Forbid();

            if (string.IsNullOrWhiteSpace(row.DataFileName))
                return NotFound("No data file.");

            var filePath =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "Data",
                    "Results",
                    row.DataFileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found.");

            // read JSON
            var json =
                await System.IO.File.ReadAllTextAsync(filePath);

            var data =
                System.Text.Json.JsonSerializer.Deserialize<
                    List<Dictionary<string, object>>>(json);

            if (data == null || data.Count == 0)
                return NotFound("No data.");

            // create Excel
            using var workbook =
                new ClosedXML.Excel.XLWorkbook();

            var ws =
                workbook.Worksheets.Add("Data");

            // headers
            var headers =
                data[0].Keys.ToList();

            for (int col = 0; col < headers.Count; col++)
            {
                ws.Cell(1, col + 1)
                  .Value = headers[col];
            }

            // rows
            for (int rowIndex = 0; rowIndex < data.Count; rowIndex++)
            {
                var rowData = data[rowIndex];

                for (int col = 0; col < headers.Count; col++)
                {
                    var key = headers[col];

                    ws.Cell(rowIndex + 2, col + 1)
                      .Value = rowData.ContainsKey(key)
                                ? rowData[key]?.ToString()
                                : "";
                }
            }

            ws.Columns().AdjustToContents();

            var stream = new MemoryStream();

            workbook.SaveAs(stream);

            stream.Position = 0;

            var fileName =
                $"export_{messageId}.xlsx";

            return File(
                stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        
        [HttpGet("/search")]
        public async Task<IActionResult> Search([FromQuery] string text)
        {
            var userName =
                HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? "";

            var rows =
                await _repo.SearchMessagesAsync(userName, text);

            return Ok(rows);
        }


        
        [HttpPost("sessions/filter")]
        public async Task<IActionResult> FilterSessions([FromBody] SessionFilterRequest request)
        {
            var userName = HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? "";
            var result =
                await _repo.GetSessionsByDatabasesAsync(
                    userName, request.Databases);

            return Ok(result);
        }

        // ==================================================
        // GET /message/{sessionId}
        // ==================================================
        [HttpPost("message/filter/{sessionId}")]
        public async Task<IActionResult> GetMessagesFilter(
            long sessionId, [FromBody] SessionFilterRequest request)
        {
            var userName = HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? "";
            var rows =
                await _repo.GetMessagesAsync(sessionId, request.Databases);
            return Ok(rows);
        }
    }

}