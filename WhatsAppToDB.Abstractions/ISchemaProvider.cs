using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WhatsAppToDB.Abstractions
{
    public interface ISchemaProvider
    {
        string ProviderName { get; } // Matches the {database} route parameter (e.g., "sqlserver", "postgres", "sqlite")
        
        Task<List<string>> GetTablesAsync(IDbConnection connection, string filter);
        
        Task<List<string>> GetColumnsAsync(IDbConnection connection, string tableName, string filter);
        
        Task<Dictionary<string, Dictionary<string, string>>> GetForeignKeysAsync(IDbConnection connection, string tableName);
    }
}