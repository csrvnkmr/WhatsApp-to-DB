using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WhatsAppToDB.Abstractions;

namespace WhatsAppToDB
{
    public class IdentityService : IIdentityService
    {
        private readonly string _defaultConnection;
        private readonly RoleSettings _roleSettings;
        private readonly IConfiguration _config;

        public IdentityService(IOptions<DatabaseSettings> dbSettings, IOptions<RoleSettings> roleSettings, IConfiguration config)
        {
            _roleSettings = roleSettings.Value;
            _defaultConnection = dbSettings.Value.ConnectionString ?? "";
            _config = config;
        }

        public async Task<IdentityContext> GetIdentityAsync(string mobileNumber)
        {
            var source = _roleSettings.MappingSource?.ToUpper() ?? "JSON";

            var jsonfile = _roleSettings.RoleMappingJsonFile;
            var rolemappingsql = _roleSettings.RoleMappingSqlQuery;

            IdentityContext identity = source.ToUpper() == "SQL"
                ? await GetFromSql(mobileNumber, rolemappingsql)
                : await GetFromJson(mobileNumber, jsonfile);

            // Hydrate Role-based Permissions and Connections
            HydrateRolePermissions(identity);

            return identity;
        }

        public void HydrateRolePermissions(IdentityContext identity)
        {
            // 1. Map Modules: RoleModules: { "SalesPerson": "Sales,Inventory" }
            var moduleMapping = _config.GetSection("RoleSettings:RoleModules").Get<Dictionary<string, string>>();
            if (moduleMapping?.TryGetValue(identity.Role, out var modules) == true)
            {
                identity.AuthorizedModules = modules.Split(',').Select(m => m.Trim()).ToList();
            }

            // 2. Map Connection: Use Role-specific string or fallback
            var roleConn = _config.GetConnectionString($"{identity.Role}Connection");
            identity.ConnectionString = !string.IsNullOrEmpty(roleConn) ? roleConn : _defaultConnection;
        }

        // SELECT RoleName as Role,EmpID as InternalUserId,'EmployeeID' as SessionContextKey FROM UserMapping WHERE Mobile = @Mobile
        private async Task<IdentityContext> GetFromSql(string mobileNumber, string roleMappingSql)
        {
            using var connection = new SqlConnection(_defaultConnection);

            // We pass mobileNumber to the SQL query as @Mobile
            var result = await connection.QueryFirstOrDefaultAsync<IdentityContext>(
                roleMappingSql,
                new { Mobile = mobileNumber }
            );

            if (result == null)
            {
                return new IdentityContext { WhatsAppNumber = mobileNumber, Role = "Guest" };
            }

            result.WhatsAppNumber = mobileNumber;
            return result;
        }
        /*
         * [
  { "WhatsAppNumber": "919876543210", "Role": "Admin", "InternalUserId": "ADM01", "SessionContextKey": "Admin" },
  { "WhatsAppNumber": "919000000000", "Role": "SalesPerson", "InternalUserId": "SLS42", "SessionContextKey": "EmployeeID" }
]
         */
        private async Task<IdentityContext> GetFromJson(string mobileNumber, string jsonFile)
        {
            if (!File.Exists(jsonFile))
            {
                return new IdentityContext { WhatsAppNumber = mobileNumber, Role = "Guest" };
            }

            try
            {
                var jsonString = await File.ReadAllTextAsync(jsonFile);
                var mappings = JsonSerializer.Deserialize<List<IdentityContext>>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var user = mappings?.FirstOrDefault(m => m.WhatsAppNumber == mobileNumber);

                return user ?? new IdentityContext { WhatsAppNumber = mobileNumber, Role = "Guest" };
            }
            catch
            {
                return new IdentityContext { WhatsAppNumber = mobileNumber, Role = "Guest" };
            }
        }

    }
}


