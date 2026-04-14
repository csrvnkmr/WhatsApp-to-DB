using System.Text.Json;

namespace WhatsAppToDB
{
    public class SchemaService
    {
        private readonly DatabaseSchema _schema;

        public SchemaService(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            _schema = JsonSerializer.Deserialize<DatabaseSchema>(json) ?? new();
        }

        public string GetModuleSchema(string moduleNames)
        {
            var lstModules = moduleNames.Split(',').Select(m => m.Trim()).ToList();
            var finalResult = "";
            // 1. Find the module and its tables
            foreach (var moduleName in lstModules)
            {
                var moduleLine = _schema.Modules
                .FirstOrDefault(m => m.StartsWith(moduleName, StringComparison.OrdinalIgnoreCase));

                if (moduleLine == null)
                {
                    Console.WriteLine("Module not found. "+moduleName);
                    continue;
                }

                var tableList = moduleLine.Split('|')[1].Split(',');

                // 2. Extract Table Metadata
                var tableDetails = _schema.Tables
                    .Where(t => tableList.Any(name => t.StartsWith(name.Trim())))
                    .ToList();

                // 3. Extract relevant Joins
                var relevantJoins = _schema.TableJoins
                    .Where(j => tableList.Any(name => j.Contains(name.Trim())))
                    .ToList();

                // 4. Format for AI Context
                var moduleschema = $"### MODULE: {moduleName}\n" +
                       "**Tables & Fields:**\n" + string.Join("\n", tableDetails) + "\n\n" +
                       "**Suggested Joins:**\n" + string.Join("\n", relevantJoins);
                finalResult += moduleschema + "\n\n";
            }
            return finalResult;
        }

        public string GetAvailableModules() =>
            string.Join(", ", _schema.Modules.Select(m => m.Split('|')[0].Trim()).ToList());
    }

    public class DatabaseSchema
    {
        public List<string> Tables { get; set; } = new();
        public List<string> TableJoins { get; set; } = new();
        public List<string> Modules { get; set; } = new();
    }
}
