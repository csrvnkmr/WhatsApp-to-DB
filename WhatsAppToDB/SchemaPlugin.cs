using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;
using WhatsAppToDB.Abstractions;

namespace WhatsAppToDB
{

    public class SchemaPlugin
    {

        private readonly SchemaService _schemaService;
        private readonly IModulePrompt? _promptExtension;
        private readonly ISqlInterceptor? _sqlExtension;

        public SchemaPlugin(IOptions<DatabaseSettings> dbSettings,
           IModulePrompt? promptExtension = null,
           ISqlInterceptor? sqlExtension = null)
        {
            _schemaService = new SchemaService(dbSettings.Value.SchemaDefinitionFile);
            _promptExtension = promptExtension;
            _sqlExtension = sqlExtension;
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
                if (_promptExtension != null && !string.IsNullOrWhiteSpace(waNumber))
                {
                    var xtensionText = await _promptExtension.GetModuleConstraintAsync(waNumber, module, userQuestion);
                    // Wrap the schema with the constraints to ensure the LLM prioritizes them
                    moduleSchema = $"--- {module} Security Constraints ---\n{xtensionText}\n\n--- {module} Schema ---\n{moduleSchema}";
                }

                schemaBuilder.AppendLine(moduleSchema);
            }

            var finalSchema = schemaBuilder.ToString();
            return string.IsNullOrWhiteSpace(finalSchema) ? "No authorized modules found." : finalSchema;
        }
    }
}
