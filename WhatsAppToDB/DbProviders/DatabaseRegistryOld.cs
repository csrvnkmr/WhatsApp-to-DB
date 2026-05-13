using Microsoft.Extensions.Options;

namespace WhatsAppToDB.Database
{
    public class DatabaseRegistryOld
    {
        private readonly List<DatabaseConfig> _databases;

        public DatabaseRegistryOld(List<DatabaseConfig> lstConfig)
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
