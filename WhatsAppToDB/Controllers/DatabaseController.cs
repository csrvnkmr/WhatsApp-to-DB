using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Audit;
using WhatsAppToDB.Data;
using WhatsAppToDB.LlmProviders;
using WhatsAppToDB.Services;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.Controllers
{
    public class DatabaseController : Controller
    {
        private readonly ILogger _waLogger;
        private readonly Database.DatabaseRegistry _registry;
        private readonly DefaultSettings _defaultSettings;
        private readonly IUserAuditService _auditService;
        private readonly JsonConfigService _jsonConfigService;

        public DatabaseController(
            ILogger waLogger, Database.DatabaseRegistry registry,
            IUserAuditService auditService, JsonConfigService jsonConfigService)
        {
            _waLogger = waLogger;
            _auditService = auditService;
            _jsonConfigService = jsonConfigService;
            _defaultSettings = _jsonConfigService.GetDefaultSettings();
            _registry = registry;
        }

        [HttpGet("databases")]
        public IActionResult GetDatabases()
        {
            var databases = _registry.GetAll().Select(d => new
            {
                d.Name,
                d.Description
            });

            var activeDb =
                HttpContext.Session.GetString(Constants.SessionKeys.ActiveDb)
                ?? _defaultSettings.DefaultDatabase;
            var activeDbDescription = activeDb;
            var dbConfig = _registry.GetDatabaseConfig(activeDb); // validate active db exists, will throw if not
            if (dbConfig != null)
            {
                Console.WriteLine($"[DB GET] Active DB = {activeDb}");
                activeDbDescription = dbConfig.Description;
            }

            return Ok(new
            {
                databases,
                activeDb,
                activeDbDescription
            });
        }

        [HttpPost("databases/select")]
        public async Task<IActionResult> SelectDatabase([FromBody] string name)
        {
            HttpContext.Session.SetString(Constants.SessionKeys.ActiveDb, name);
            var saved = HttpContext.Session.GetString(Constants.SessionKeys.ActiveDb);
            var userName = HttpContext.Items["UserName"]?.ToString() ?? "";
            await _auditService.LogAsync(userName, AuditActions.DatabaseChanged, name);
            Console.WriteLine($"[DB SELECT] Saved DB = {saved}");
            Console.WriteLine($"[DB SELECT] Session={HttpContext.Session.Id}");
            return Ok();
        }

    }
}
