// ==========================================================
// Controllers/ChatController.cs
// ==========================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Net;
using System.Net.Mail;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;
using WhatsAppToDB.Models;
using WhatsAppToDB.Services;

namespace WhatsAppToDB.Controllers
{
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _waLogger;
        private readonly ChatDbRepository _repo;
        private readonly IOptions<MailSettings> _mailOptions;
        private readonly PromptExecutionSettings _promptSettings;
        private readonly IQueryService _queryService;

        public ChatController(
            IServiceScopeFactory scopeFactory,
            ILogger waLogger,
            ChatDbRepository repo,
            IOptions<MailSettings> mailOptions,
            IQueryService queryService)
        {
            _scopeFactory = scopeFactory;
            _waLogger = waLogger;
            _repo = repo;
            _mailOptions = mailOptions;

            _promptSettings =
                new OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior =
                        FunctionChoiceBehavior.Auto()
                };
            _queryService = queryService;
        }

        [HttpPost("/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request) 
        {
            var result = UserService.ValidateLogin(request.Username, request.Password);
            if (!result.isSuccess)
            {
                return Unauthorized();
            }
            return Ok(new { Token = result.session.Token, Message = "Login Successful" });
        }

        // ==================================================
        // POST /ask
        // ==================================================
        [HttpPost("/ask")]
        public async Task<IActionResult> Ask(
            [FromBody] AskRequest request)
        {
            var userName =
                HttpContext.Items["UserName"]?.ToString() ?? "";
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

            var response =
                await _queryService.ExecuteQuery(
                    _scopeFactory,
                    identity,
                    request.Question,
                    _promptSettings,
                    _waLogger,
                    _repo,
                    sessionId);

            return Ok(response);
        }

        // ==================================================
        // GET /session
        // ==================================================
        [HttpGet("/session")]
        public async Task<IActionResult> GetSessions()
        {
            var userName =
                HttpContext.Items["UserName"]?.ToString() ?? "";

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
                HttpContext.Items["UserName"]?.ToString() ?? "";

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
                HttpContext.Items["UserName"]?.ToString() ?? "";

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
                    HttpContext.Items["UserName"]?.ToString() ?? "";

                var msg =
                    await _repo.GetMessageExtrasAsync(
                        request.MessageId,
                        userName);

                if (msg == null)
                    return NotFound();

                var settings =
                    _mailOptions.Value;

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

                using var client =
                    new SmtpClient(
                        settings.SmtpServer,
                        settings.Port)
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
                HttpContext.Items["UserName"]?.ToString() ?? "";

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

        [HttpGet("/addbookmark/{messageId}")]
        public async Task<IActionResult> AddBookmark(
    long messageId,
    [FromQuery] string? text)
        {
            var userName =
                HttpContext.Items["UserName"]?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(text))
                text = "Saved bookmark";

            await _repo.AddBookmarkAsync(
                userName,
                messageId,
                text);

            return Ok(new { success = true });
        }

        [HttpGet("/removebookmark/{messageId}")]
        public async Task<IActionResult> RemoveBookmark(
    long messageId)
        {
            var userName =
                HttpContext.Items["UserName"]?.ToString() ?? "";

            await _repo.RemoveBookmarkAsync(
                userName,
                messageId);

            return Ok(new { success = true });
        }

        [HttpGet("/bookmarks")]
        public async Task<IActionResult> GetBookmarks()
        {
            var userName =
                HttpContext.Items["UserName"]?.ToString() ?? "";

            var rows =
                await _repo.GetBookmarksAsync(userName);

            return Ok(rows);
        }

        [HttpGet("/search")]
        public async Task<IActionResult> Search([FromQuery] string text)
        {
            var userName =
                HttpContext.Items["UserName"]?.ToString() ?? "";

            var rows =
                await _repo.SearchMessagesAsync(userName, text);

            return Ok(rows);
        }
    }
}