using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace WhatsAppToDB
{
    public class SchemaPlugin
    {
        private readonly SchemaService _schemaService;

        public SchemaPlugin(SchemaService schemaService)
        {
            _schemaService = schemaService;
        }

        [KernelFunction]
        [Description("Gets the database tables, fields, and join logic for a specific business module.")]
        public string GetSchemaForModule([Description("Module name like 'Sales' or 'Accounts'")] string moduleName)
        {
            return _schemaService.GetModuleSchema(moduleName);
        }

        [KernelFunction]
        [Description("Lists all available business modules in the system.")]
        public string GetAvailableModules()
        {
            return string.Join(", ", _schemaService.GetAvailableModules());
        }
    }
}
