// ==========================================================
// Admin/AdminController.cs
// ==========================================================
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;
using WhatsAppToDB.Services;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace WhatsAppToDB.Admin
{
    [ApiController]
    [Route("admin/api")]
    public class AdminController : ControllerBase
    {
        private readonly ILogger _logger;

        string[] dbSpecific =
                {
                    "roles",
                    "schema",
                    "systemprompt",
                    "plugins",
                    "extensions",
                    "mailsettings",
                    "tables",
                    "modules",
                    "tablejoins",
                    "fewshotqueries",
                    "vectorconfigurations",
                    "vectordbsettings",
                    "extensions",
                    "testsuitequeries"
                };
        private readonly IWebHostEnvironment _env;
        private readonly string ConfigRoot;
        private readonly JsonConfigService _configService;
        private readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions
            {
                WriteIndented = true
            };

        public AdminController(
            IWebHostEnvironment env, IConfiguration config,
            JsonConfigService configService, ILogger logger)
        {
            _logger = logger;
            ConfigRoot = config.GetValue<string>("ConfigRootFolder");
            if (string.IsNullOrWhiteSpace(ConfigRoot))
            {
                var appRoot = Path.Combine(AppContext.BaseDirectory, "config");
                ConfigRoot = appRoot;
                _logger.LogInfo($"[AdminController] Default config folder not in the appsettings.json. Using {appRoot}");
            }
            _env = env;
            _configService = configService;
        }

        // ======================================================
        // CONFIG ROOT
        // ======================================================

        //private string ConfigRoot => Path.Combine( AppContext.BaseDirectory, "config");

        private string MetadataRoot =>
            Path.Combine(ConfigRoot, "metadata");

        // ======================================================
        // GET METADATA
        // ======================================================
        // GET /admin/api/metadata/databases
        // ======================================================

        [HttpGet("metadata/{entity}")]
        public IActionResult GetMetadata(
            string entity)
        {
            var file =
                Path.Combine(
                    MetadataRoot,
                    $"{entity}.json");

            if (!System.IO.File.Exists(file))
            {
                return NotFound(new
                {
                    message =
                        $"Metadata not found for '{entity}'"
                });
            }

            var json =
                System.IO.File.ReadAllText(file);

            return Content(
                json,
                "application/json");
        }

        // ======================================================
        // GET DATA
        // ======================================================
        // GET /admin/api/data/databases
        // GET /admin/api/data/schema?database=chinook
        // ======================================================

        [HttpGet("data/{entity}")]
        public IActionResult GetData(
            string entity,
            [FromQuery] string? database = null)
        {
            var filePath       = GetDataFilePath(entity, database);
            var sensitiveFields = _configService.GetSensitiveFields(entity);
 
            // Auto-create empty file if missing
            if (!System.IO.File.Exists(filePath))
            {
            /* create folder only in SaveData, not here. 
            Otherwise, we end up with empty files for all entities in the root folder.
                var folder = System.IO.Path.GetDirectoryName(filePath)!;
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
 
                System.IO.File.WriteAllText(filePath, "[]");

            */                
                return Content("[]", "application/json");
            }
 
            // Load & decrypt
            var node   = _configService.LoadAndDecrypt(filePath, sensitiveFields);
            var result = node?.ToJsonString(_jsonOptions) ?? "[]";
            return Content(result, "application/json");
        }


        // ======================================================
        // SAVE DATA
        // ======================================================
        // POST /admin/api/data/databases
        // POST /admin/api/data/schema?database=chinook
        // ======================================================

        [HttpPost("data/{entity}")]
        public async Task<IActionResult> SaveData(
            string entity,
            [FromQuery] string? database = null)
        {
            using var reader =
                new StreamReader(Request.Body);

            var body =
                await reader.ReadToEndAsync();

            // validate json
            try
            {
                JsonDocument.Parse(body);
            }
            catch
            {
                return BadRequest(new
                {
                    message = "Invalid JSON"
                });
            }

            var filePath =
                GetDataFilePath(
                    entity,
                    database);

            var folder = Path.GetDirectoryName(filePath)!;

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var sensitiveFields = _configService.GetSensitiveFields(entity);            
            _configService.EncryptAndSave(filePath, body, sensitiveFields, _jsonOptions);

            
            //var parsed =JsonSerializer.Deserialize<object>(body);
            //var pretty =JsonSerializer.Serialize(parsed, _jsonOptions);
            //await System.IO.File.WriteAllTextAsync(filePath, pretty);

            return Ok(new
            {
                success = true
            });
        }

        
        // ======================================================
        // SAVE METADATA
        // POST /admin/api/metadata/{entity}
        // Always save directly into the metadata folder. Before
        // overwriting, take (backup) the current file.
        // ======================================================

        [HttpPost("metadata/{entity}")]
        public async Task<IActionResult> SaveMetadata(
            string entity)
        {
            using var reader = new StreamReader(Request.Body);

            var body = await reader.ReadToEndAsync();

            // validate json
            try
            {
                JsonDocument.Parse(body);
            }
            catch
            {
                return BadRequest(new
                {
                    message = "Invalid JSON"
                });
            }

            var folder = MetadataRoot;

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var file = Path.Combine(folder, $"{entity}.json");

            // Backup current file (take the current file) before updating
            if (System.IO.File.Exists(file))
            {
                var backups = Path.Combine(folder, "backups");
                if (!Directory.Exists(backups))
                    Directory.CreateDirectory(backups);

                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var backupFile = Path.Combine(backups, $"{entity}.{timestamp}.json");
                System.IO.File.Copy(file, backupFile);
            }

            // Pretty-print and save directly into metadata
            var parsed = JsonSerializer.Deserialize<object>(body);
            var pretty = JsonSerializer.Serialize(parsed, _jsonOptions);
            await System.IO.File.WriteAllTextAsync(file, pretty);

            return Ok(new { success = true });
        }

        // ======================================================
        // PATH RESOLUTION
        // ======================================================

        private string GetDataFilePath(
            string entity,
            string? database)
        {
            // -----------------------------
            // DATABASE-SPECIFIC
            // -----------------------------


            if (dbSpecific.Contains(
                entity,
                StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(database))
                {
                    throw new Exception(
                        $"Database is required for '{entity}'");
                }

                var dbFolder =
                    Path.Combine(
                        ConfigRoot,
                        "databases",
                        database);

                return Path.Combine(
                    dbFolder,
                    $"{entity}.json");
            }

            // -----------------------------
            // GLOBAL CONFIG
            // -----------------------------
            return Path.Combine(
                ConfigRoot,
                $"{entity}.json");
        }

        [HttpPost("compile-metadata-string")]
        public IActionResult CompileMetadataString([FromBody] MetadataCompileRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TargetEngine))
            {
                return BadRequest("Invalid payload structure or missing TargetEngine.");
            }

            string compiledOutputString;

            // 1. If it's a known database provider, handle formatting using standard DB rules
            if (request.TargetEngine.Equals("mssql", StringComparison.OrdinalIgnoreCase) || 
                request.TargetEngine.Equals("postgres", StringComparison.OrdinalIgnoreCase))
            {
                var dbBuilder = new System.Data.Common.DbConnectionStringBuilder();
                
                foreach (var kvp in request.Parameters)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Value))
                    {
                        dbBuilder[kvp.Key] = kvp.Value;
                    }
                }
                compiledOutputString = dbBuilder.ConnectionString;
            }
            // Add this logic into your existing TargetEngine condition checking blocks
            else if (request.TargetEngine.Equals("mysql", StringComparison.OrdinalIgnoreCase))
            {
                var mysqlBuilder = new System.Data.Common.DbConnectionStringBuilder();
                foreach (var kvp in request.Parameters)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Value))
                    {
                        // Normalize potential lowercase inputs back to official driver casing expectations
                        string normKey = kvp.Key.ToLower() switch {
                            "userid"   => "User ID",
                            "username" => "User ID",
                            _          => kvp.Key
                        };
                        mysqlBuilder[normKey] = kvp.Value;
                    }
                }
                compiledOutputString = mysqlBuilder.ConnectionString;
            }
            else
            {
                // 2. Generic fallback for everything else (LLM configs, API targets, prompts configurations)
                // Outputs a clean, uniform: "ApiKey=sk_abc;Endpoint=https://api.com;"
                compiledOutputString = string.Join(";", request.Parameters
                    .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
                    .Select(kvp => $"{kvp.Key}={kvp.Value}"));
            }

            return Ok(new { resultString = compiledOutputString });
        }

        
        // ======================================================
        // SENSITIVE FIELD RESOLUTION
        // Now handled by JsonConfigService.GetSensitiveFields()
        // ======================================================

    }

    public record MetadataCompileRequest
    {
        // E.g., "SqlServer", "PostgreSQL", "OpenAI", "SMTP"
        public string TargetEngine { get; init; } = string.Empty;

        // A flat list of keys and values collected from your dynamic form UI
        public Dictionary<string, string> Parameters { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }
}