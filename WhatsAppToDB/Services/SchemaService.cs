using WhatsAppToDB.Database;
using WhatsAppToDB.Models;


namespace WhatsAppToDB.Services
{
    public class SchemaService
    {
        private readonly JsonConfigService _json;
        private readonly ILogger _logger;

        public SchemaService(
            JsonConfigService json,
            ILogger logger)
        {
            _json = json;
            _logger = logger;
        }

        // ==================================================
        // GET MODULE SCHEMA
        // ==================================================

        public string GetModuleSchema(
            string database,
            string moduleNames)
        {
            var modules =
                _json.GetModules(database);

            var tables =
                _json.GetTables(database);

            var joins =
                _json.GetTableJoins(database);

            var requestedModules =
                moduleNames
                    .Split(',')
                    .Select(x => x.Trim())
                    .ToList();

            var finalResult = "";
            _logger.LogInfo($"Schema for module requested {database} - {moduleNames}");
            // ============================================
            // EACH MODULE
            // ============================================

            foreach (var moduleName in requestedModules)
            {
                var module =
                    modules.FirstOrDefault(m =>
                        m.Name.Equals(
                            moduleName,
                            StringComparison.OrdinalIgnoreCase));

                if (module == null)
                {
                    Console.WriteLine(
                        $"Module not found: {moduleName}");

                    continue;
                }

                // ========================================
                // TABLES
                // ========================================

                var moduleTables =
                    tables
                        .Where(t =>
                            module.Tables.Contains(
                                t.Name,
                                StringComparer.OrdinalIgnoreCase))
                        .ToList();

                // ========================================
                // JOINS
                // ========================================

                var relevantJoins =
                    joins
                        .SelectMany(j => j.JoinConditions)
                        .Where(j =>
                            module.Tables.Any(t =>
                                j.Contains(
                                    t,
                                    StringComparison.OrdinalIgnoreCase)))
                        .Distinct()
                        .ToList();

                // ========================================
                // FORMAT TABLE DETAILS
                // ========================================

                var tableText =
                    moduleTables.Select(t =>
                        $"{t.Name} ({t.Description}) : " +
                        $"{string.Join(", ", t.Columns)}");

                // ========================================
                // BUILD MODULE SCHEMA
                // ========================================

                var moduleSchema =
                    $"### MODULE: {module.Name}\n" +
                    $"DESCRIPTION: {module.Details}\n\n" +

                    $"**Tables & Fields:**\n" +
                    $"{string.Join("\n", tableText)}\n\n" +

                    $"**Suggested Joins:**\n" +
                    $"{string.Join("\n", relevantJoins)}";

                finalResult +=
                    moduleSchema + "\n\n";
            }

            return finalResult;
        }

        // ==================================================
        // AVAILABLE MODULES
        // ==================================================

        public string GetAvailableModules(
            string database)
        {
            _logger.LogInfo($"module list requested {database}");
            var modules =
                _json.GetModules(database);
            var modulelist = string.Join(
                ", ", modules.Select(m => m.Name));
            return modulelist;
        }
    }
}
