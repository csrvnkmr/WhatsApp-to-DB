using Dapper;
using Microsoft.Extensions.Options;
using System.Data;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;
using WhatsAppToDB.Models;
using WhatsAppToDB.Services;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.Database
{
    public class DatabaseContextService
    {
        private readonly IHttpContextAccessor _http;
        private readonly DatabaseRegistry _registry;
        private readonly DbProviderFactory _factory;
        private readonly DefaultSettings _defaultSettings;

        private readonly JsonConfigService _jsonConfigService;
        private readonly AiRequestContext _requestContext;

        public DatabaseContextService(
            IHttpContextAccessor http,
            DatabaseRegistry registry,
            DbProviderFactory factory,
            JsonConfigService jsonConfigService,
            AiRequestContext requestContext)
        {
            _http = http;
            _registry = registry;
            _factory = factory;
            _jsonConfigService = jsonConfigService;
            _requestContext = requestContext;
            _defaultSettings = _jsonConfigService.GetDefaultSettings();
        }   

        public DatabaseConfig GetCurrentConfig()
        {

            string dbName;
            if (!string.IsNullOrWhiteSpace(_requestContext?.Database))
            {
                dbName = _requestContext.Database;
            }
            else
            {
                dbName = _http.HttpContext?.Session?.GetString(Constants.SessionKeys.ActiveDb)
                         ?? _defaultSettings.DefaultDatabase;
            }

            return _registry.GetDatabaseConfig(dbName);
        }

        public IDbProvider GetProvider()
        {
            var config = GetCurrentConfig();
            return _factory.GetDbProvider(config.DbProvider);
        }

        public IDbConnection CreateConnection(string connectionString = null)
        {
            var config = GetCurrentConfig();
            var provider = _factory.GetDbProvider(config.DbProvider);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = config.ConnectionString;
            }

            return provider.GetConnection(connectionString);
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
