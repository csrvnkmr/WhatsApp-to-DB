using WhatsAppToDB.Abstractions;

namespace WhatsAppToDB.Services
{
    public class JsonFileStorageProvider : IConfigStorageProvider
    {
        private readonly string _configRoot;

        public JsonFileStorageProvider(IConfiguration config)
        {
            _configRoot = config.GetValue<string>("ConfigRootFolder")!;
            if (string.IsNullOrWhiteSpace(_configRoot))
                _configRoot = Path.Combine(AppContext.BaseDirectory, "Config");

            if (!Directory.Exists(_configRoot)) Directory.CreateDirectory(_configRoot);
        }

        public string ReadGlobalConfigRaw(string entityName)
        {
            var path = Path.Combine(_configRoot, $"{entityName}.json");
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }

        public string ReadDatabaseConfigRaw(string database, string entityName)
        {
            var path = Path.Combine(_configRoot, "databases", database, $"{entityName}.json");
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }

        public void WriteConfigRaw(string? database, string entityName, string rawContent)
        {
            var path = string.IsNullOrEmpty(database)
                ? Path.Combine(_configRoot, $"{entityName}.json")
                : Path.Combine(_configRoot, "databases", database, $"{entityName}.json");

            var folder = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            File.WriteAllText(path, rawContent);
        }
    }

}