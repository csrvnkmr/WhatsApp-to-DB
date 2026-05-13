using Dapper;
using Microsoft.Extensions.Options;
using System.Data;
using WhatsAppToDB.Data;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.Database
{
    public class DatabaseContextService
    {
        private readonly IHttpContextAccessor _http;
        private readonly DatabaseRegistry _registry;
        private readonly DbProviderFactory _factory;
        private readonly DefaultSettings _defaultSettings;

        public DatabaseContextService(
            IHttpContextAccessor http,
            DatabaseRegistry registry,
            DbProviderFactory factory,
            IOptions<DefaultSettings> defaultSettings)
        {
            _http = http;
            _registry = registry;
            _factory = factory;
            _defaultSettings = defaultSettings.Value;
        }   

        public DatabaseConfig GetCurrentConfig()
        {
            var dbName = _http.HttpContext?.Session?.GetString(Constants.SessionKeys.ActiveDb)
                         ?? _defaultSettings.DefaultDatabase;

            return _registry.GetDatabaseConfig(dbName);
        }

        public IDbProvider GetProvider()
        {
            var config = GetCurrentConfig();
            return _factory.GetDbProvider(config.DbProvider);
        }

        public IDbConnection CreateConnection()
        {
            var config = GetCurrentConfig();
            var provider = _factory.GetDbProvider(config.DbProvider);

            return provider.GetConnection(config.ConnectionString);
        }

        public string GetCurrentDatabaseName()
        {
            return GetCurrentConfig().Name;
        }

        public string GetProviderName()
        {
            return GetCurrentConfig().DbProvider;
        }
        //public async Task<IEnumerable<dynamic>> ExecuteAsync(string sql)
        //{
        //    using var conn = CreateConnection();

        //    return await conn.QueryAsync(sql);
        //}
    }
}
