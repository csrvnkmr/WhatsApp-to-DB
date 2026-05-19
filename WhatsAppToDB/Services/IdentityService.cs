using Dapper;
using Microsoft.Graph.Models.CallRecords;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Database;
using WhatsAppToDB.Models;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly JsonConfigService _jsonConfigService;
        private readonly ILogger _logger;
        private readonly DatabaseContextService _databaseContextService;

        public IdentityService(
            JsonConfigService jsonConfigService,
            DatabaseContextService databaseContextService,
            ILogger? logger = null)
        {
            _jsonConfigService = jsonConfigService;
            _logger = logger ?? new AppLogger();
            _databaseContextService = databaseContextService;
        }

        // ==================================================
        // GET IDENTITY
        // ==================================================

        public async Task<IdentityContext> GetIdentityAsync(
            string mobileNumber)
        {
            await _logger.LogInfoAsync(
                $"Fetching identity for mobile number: {mobileNumber}");
            
            var loginUser =  _jsonConfigService.GetUserByWhatsAppNumber(mobileNumber);

            var ic= new IdentityContext
            {
                UserName =  loginUser?.ToString(),                
            };
            HydrateRolePermissions( ic );
            return ic;

          
        }

        // ==================================================
        // OPTIONAL SQL BASED LOOKUP
        // ==================================================

        public async Task<IdentityContext?> GetFromSqlAsync(
            string sql,
            string mobileNumber)
        {
            await _logger.LogInfoAsync(
                $"Executing identity SQL for {mobileNumber}");

            using var connection =
                _databaseContextService
                    .CreateConnection();

            var result =
                await connection
                    .QueryFirstOrDefaultAsync<IdentityContext>(
                        sql,
                        new
                        {
                            Mobile = mobileNumber
                        });

            return result;
        }

        public void HydrateRolePermissions(IdentityContext identity)
        {
            _logger.LogInfo(
                $"Fetching Roles for name: {identity.UserName}");

            var database = _databaseContextService.GetCurrentDatabaseName();

            var roles = _jsonConfigService.GetRoles(database);

            // ============================================
            // FIND USER ROLES
            // ============================================

            var matchedRoles =
                roles
                    .Where(r =>
                        r.Users != null &&
                        r.Users.Any(u =>
                            u.Equals(
                                identity.UserName,
                                StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            if (!matchedRoles.Any())
            {
                identity.Role = "Guest";
                identity.AuthorizedModules = new List<string>();
                identity.ConnectionString = _databaseContextService
                    .GetCurrentConfig().ConnectionString;
                return ;
            }
            // ============================================
            // COMBINE MODULES
            // ============================================

            var modules =
                matchedRoles
                    .SelectMany(r => r.Modules ?? [])
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            // ============================================
            // CONNECTION STRING
            // first role wins
            // ============================================

            var connectionString =
                matchedRoles
                    .FirstOrDefault(r =>
                        !string.IsNullOrWhiteSpace(r.ConnectionString))?.ConnectionString
                ?? _databaseContextService.GetCurrentConfig().ConnectionString;

            // ============================================
            // BUILD IDENTITY
            // ============================================
            var userRoles = "";
            matchedRoles.ForEach(x => userRoles += "," + x.Name);
            if (userRoles.StartsWith(",")) {
                userRoles = userRoles.Substring(1);
            }
            identity.Role = userRoles;
            identity.AuthorizedModules = modules;
            identity.ConnectionString = connectionString;
        }
    }
}