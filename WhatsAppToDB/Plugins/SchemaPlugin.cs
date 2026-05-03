using Microsoft.Extensions.Options;
using Microsoft.Graph.Models;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Services;

namespace WhatsAppToDB.Plugin
{

    public class SchemaPlugin
    {

        private readonly SchemaService _schemaService;
        private readonly IModulePrompt? _promptExtension;
        private readonly ISqlInterceptor? _sqlExtension;
        private readonly ISqlTemplateExtension? _sqlTemplateExtension;
        private readonly DatabaseSettings _dbSettings;
        private readonly ILogger _logger;
        public SchemaPlugin(IOptions<DatabaseSettings> dbSettings,
           IModulePrompt? promptExtension = null,
           ISqlInterceptor? sqlExtension = null,
           ISqlTemplateExtension? sqlTemplateExtension = null,
           ILogger? logger = null)
        {
            _schemaService = new SchemaService(dbSettings.Value.SchemaDefinitionFile);
            _promptExtension = promptExtension;
            _sqlExtension = sqlExtension;
            _sqlTemplateExtension = sqlTemplateExtension;
            _logger = logger ?? new AppLogger();
            _dbSettings = dbSettings.Value;
        }

        [KernelFunction]
        [Description("Returns a list of all available modules to help decide which schema to load.")]
        public string GetAvailableModules() => _schemaService.GetAvailableModules(); // b1Modules.GetModules(); // string.Join(", ", moduleSchemas.Keys);


        [KernelFunction]
        [Description("Gets the detailed table and field schema for specific modules. " +
             "IMPORTANT: You must call GetAvailableModules first to identify the correct module names.")]
        public async Task<string> GetSchemaForModule(
            [Description("Comma-separated module names")] string modulename,
            Kernel kernel)
        {
            // 1. Resolve Identity
            var identity = kernel.Data["UserIdentity"] as IdentityContext;
            if (identity == null) return "Error: Security context missing.";

            // 2. Split and Clean Module Names
            var requestedModules = modulename.Split(',')
                                             .Select(m => m.Trim())
                                             .Where(m => !string.IsNullOrEmpty(m))
                                             .ToList();

            kernel.Data["LastRequestedModule"] = string.Join(", ", requestedModules);

            var schemaBuilder = new StringBuilder();
            var waNumber = kernel.Data["WhatsAppNumber"]?.ToString();                                                                                                                                                                                                                                                                                                                                                                                                       
            var userQuestion = kernel.Data["UserQuestion"]?.ToString();

            var isFirstModule = true;
            var connString = _dbSettings.ConnectionString;
            if (!string.IsNullOrEmpty(identity.ConnectionString))
            {
                connString = identity.ConnectionString;
            }
            var templService = new TemplateService(connString, this._logger);
            foreach (var module in requestedModules)
            {
                if (!isFirstModule)
                {
                    schemaBuilder.AppendLine("\n--- Next Module ---\n");
                }
                isFirstModule = false;
                // 3. Security Check: Role-Based Access Control
                // Allow if Admin OR if the specific module is in their authorized list
                bool isAuthorized = identity.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                                    (identity.AuthorizedModules != null &&
                                     identity.AuthorizedModules.Contains(module, StringComparer.OrdinalIgnoreCase));

                if (!isAuthorized)
                {
                    schemaBuilder.AppendLine($"[Access Denied: You do not have permission to view the {module} schema.]");
                    continue;
                }

                // 4. Fetch Base Schema
                var moduleSchema = _schemaService.GetModuleSchema(module);

                // 5. Apply Dynamic Constraints (Row Level Security / Prompt Extensions)
                if (_promptExtension != null )
                {
                    var xtensionText = await _promptExtension.GetModuleConstraintAsync(identity, module, userQuestion);
                    // Wrap the schema with the constraints to ensure the LLM prioritizes them
                    moduleSchema = $"--- {module} Security Constraints ---\n{xtensionText}\n\n--- {module} Schema ---\n{moduleSchema}";
                }
                if (_sqlTemplateExtension != null)
                {
                    var templateText = await _sqlTemplateExtension.GetSqlTemplateAsync(identity, module, userQuestion);
                    // Wrap the schema with the templates to ensure the LLM prioritizes them
                    moduleSchema = $"--- {module} SQL Templates ---\n{templateText}\n\n--- {module} Schema ---\n{moduleSchema}";
                }
                schemaBuilder.AppendLine(moduleSchema);
                /* instead of sending all queries, we will just send the most relevant ones in the SQL Template extension
                var lstQueries = await templService.GetTemplatesByModule(module);
                var queryText = "";
                if (lstQueries != null && lstQueries.Count > 0)
                {
                    queryText += $"--- {module} Few-Shot Query Examples ---\n";
                    queryText += $"*CRITICAL: Use these templates as the primary logic source for the matching intent.*";
                    foreach (var item in lstQueries)
                    {
                        queryText += $"Intent: {item.QueryDescription}\nTemplate:\n {item.QueryText}";
                    }
                }
                */
                //schemaBuilder.AppendLine(queryText);
            }


            var finalSchema = schemaBuilder.ToString();
            return string.IsNullOrWhiteSpace(finalSchema) ? "No authorized modules found." : finalSchema;
        }
    }
}
