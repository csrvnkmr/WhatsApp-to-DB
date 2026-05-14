using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;
using WhatsAppToDB.LlmProviders;
using WhatsAppToDB.Services;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.Controllers
{
    [ApiController]
    public class BookmarkController : Controller
    {
        private readonly ILogger _waLogger;
        private readonly ChatDbRepository _repo;

        public BookmarkController(            
            ILogger waLogger, ChatDbRepository repo)
        {
            _waLogger = waLogger;
            _repo = repo;
        }

 

        [HttpGet("/addbookmark/{messageId}")]
        public async Task<IActionResult> AddBookmark(long messageId,[FromQuery] string? text)
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
        public async Task<IActionResult> RemoveBookmark(long messageId)
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

            Console.WriteLine($"Getting bookmarks for user {userName}");
            var rows =
                await _repo.GetBookmarksAsync(userName);

            return Ok(rows);
        }


    }
}
