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
            var username = HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? "";
            _waLogger.LogInfo($"[DatabaseController] Fetching databases for user: {username}");
            return GetDatabasesForUser(username);
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
                _waLogger.LogInfo($"[DatabaseController] DB GET: Active DB = {activeDb}");
                activeDbDescription = dbConfig.Description;
            }

            return Ok(new
            {
                databases,
                activeDb,
                activeDbDescription
            });
        }

        private (string dbname, string dbdescription) GetActiveDatabase(string username)
        {
            var currentUser = username;
            var userobj= _jsonConfigService.GetUser(currentUser);
            var userDb = userobj?.DefaultDatabase ?? _defaultSettings.DefaultDatabase;

            var activeDb =
                HttpContext.Session.GetString(Constants.SessionKeys.ActiveDb)
                ?? userDb;
            var dbConfig = _registry.GetDatabaseConfig(activeDb); // validate active db exists, will throw if not
            var activeDbDescription = dbConfig != null ? dbConfig.Description : activeDb;
            return (activeDb, activeDbDescription);
        }


        [HttpPost("databases/select")]
        public async Task<IActionResult> SelectDatabase([FromBody] string name)
        {
            HttpContext.Session.SetString(Constants.SessionKeys.ActiveDb, name);
            var saved = HttpContext.Session.GetString(Constants.SessionKeys.ActiveDb);
            var userName = HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? "";
            await _auditService.LogAsync(userName, AuditActions.DatabaseChanged, name);
            _waLogger.LogInfo($"[DatabaseController] DB SELECT: Saved DB = {saved}");
            _waLogger.LogInfo($"[DatabaseController] DB SELECT: Session={HttpContext.Session.Id}");
            return Ok();
        }

        [HttpGet("databases/{username}")]
        public IActionResult GetDatabasesForUser(string username)
        {
            _waLogger.LogInfo($"[DatabaseController] Fetching databases for user: {username}");

            var allDatabases = _registry.GetAll();
            var userDatabases = new List<object>();

            foreach (var database in allDatabases)
            {
                try
                {

                    var roles = _jsonConfigService.GetRoles(database.Name);
                    
                    // if the database has a default role, that database can be assigned to anyone
                    bool hasRole = (!string.IsNullOrWhiteSpace(database.DefaultRole));
                    // Check if user has any role in this database
                    if (!hasRole && roles != null)
                    {
                        hasRole = roles.Any(role => 
                            role.Users != null && role.Users.Contains(username, StringComparer.OrdinalIgnoreCase));
                    }

                    if (hasRole)
                    {
                        userDatabases.Add(new
                        {
                            database.Name,
                            database.Description
                        });
                        _waLogger.LogInfo($"[DatabaseController] User {username} has access to database: {database.Name}");
                    }
                }
                catch (Exception ex)
                {
                    _waLogger.LogWarning($"[DatabaseController] Error checking roles for database {database.Name}: {ex.Message}");
                }
            }
            var currentDb= GetActiveDatabase(username);

            _waLogger.LogInfo($"[DatabaseController] Found {userDatabases.Count} databases for user {username}");
            return Ok(new
            {
                databases = userDatabases,
                activeDb = currentDb.dbname,
                activeDbDescription = currentDb.dbdescription
            });
        }

    }
}
