using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsAppToDB.Abstractions
{
    public class IdentityContext
    {
        public string UserName { get; set; } = string.Empty;

        public string Role { get; set; } = "Guest";
        public string InternalUserId { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public List<string> AuthorizedModules { get; set; } = new();

        public string Database { get; set; } = "";
        public string LlmProvider { get; set; } = "";
        public string LlmModel { get; set; } = "";

        public bool IsWhatsAppRequest { get; set; }

        public string WhatsAppNumber { get; set; } = "";
        public string WhatsAppProfileId { get; set; } = "";

        // The key used for DB session context (e.g., 'SalesPersonID', 'EmpID')
        public string SessionContextKey { get; set; } = string.Empty;

        // Logic: Use the mapped key, fallback to the Role name if empty
        public string GetActiveContextKey() =>
            string.IsNullOrWhiteSpace(SessionContextKey) ? Role : SessionContextKey;

        public bool IsAdministrator() =>
            Role.Split(',').Select(r => r.Trim()).Contains("Admin", StringComparer.OrdinalIgnoreCase);

        // Helper to check if a user can access a specific schema module
        public bool HasAccess(string moduleName)
        {
            return (IsAdministrator() ||
                    AuthorizedModules.Contains(moduleName, StringComparer.OrdinalIgnoreCase));
        }
    }
}
