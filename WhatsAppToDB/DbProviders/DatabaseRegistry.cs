using Microsoft.Extensions.Options;

namespace WhatsAppToDB.Database
{
    public class DatabaseRegistry
    {
        private readonly List<DatabaseConfig> _databases;

        public DatabaseRegistry(List<DatabaseConfig> lstConfig)
        {
            _databases = lstConfig;
        }

        public List<DatabaseConfig> GetAll() => _databases;

        public DatabaseConfig GetDatabaseConfig(string name)
        {
            return _databases.First(d =>
                d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
