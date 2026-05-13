using Microsoft.Extensions.Options;
using WhatsAppToDB.Services;

namespace WhatsAppToDB.Database
{
    public class DatabaseRegistry
    {
        //private readonly List<DatabaseConfig> _databases;
        private readonly JsonConfigService _jsonConfigService;

        public DatabaseRegistry(JsonConfigService jsonConfigService)
        {
            _jsonConfigService = jsonConfigService;
        }

        public List<DatabaseConfig> GetAll()
        {
            List<DatabaseConfig> databases = _jsonConfigService.GetDatabaseConfigs();
            return databases;
        }

        public DatabaseConfig GetDatabaseConfig(string name)
        {
            List<DatabaseConfig> databases = GetAll();
            return databases.First(d =>
                d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
