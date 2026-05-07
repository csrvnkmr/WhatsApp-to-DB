// ==========================================================
// Admin/AdminController.cs
// ==========================================================
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WhatsAppToDB.Admin
{
    [ApiController]
    [Route("admin/api")]
    public class AdminController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        private readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions
            {
                WriteIndented = true
            };

        public AdminController(
            IWebHostEnvironment env)
        {
            _env = env;
        }

        // ======================================================
        // CONFIG ROOT
        // ======================================================

        private string ConfigRoot =>
            Path.Combine(
                AppContext.BaseDirectory,
                "config");

        private string MetadataRoot =>
            Path.Combine(
                ConfigRoot,
                "metadata");

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
            var file =
                GetDataFilePath(
                    entity,
                    database);

            if (!System.IO.File.Exists(file))
            {
                // auto create empty array
                System.IO.File.WriteAllText(
                    file,
                    "[]");

                return Content(
                    "[]",
                    "application/json");
            }

            var json =
                System.IO.File.ReadAllText(file);

            return Content(
                json,
                "application/json");
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

            var file =
                GetDataFilePath(
                    entity,
                    database);

            var folder =
                Path.GetDirectoryName(file)!;

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // pretty format
            var parsed =
                JsonSerializer.Deserialize<object>(
                    body);

            var pretty =
                JsonSerializer.Serialize(
                    parsed,
                    _jsonOptions);

            await System.IO.File.WriteAllTextAsync(
                file,
                pretty);

            return Ok(new
            {
                success = true
            });
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
            var dbSpecific =
                new[]
                {
                    "roles",
                    "schema",
                    "systemprompt",
                    "plugins",
                    "extensions",
                    "mailsettings"
                };

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
    }
}