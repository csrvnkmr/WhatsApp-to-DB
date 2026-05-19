using System.Data;
using Dapper;
using WhatsAppToDB.Abstractions;

namespace WhatsAppToDB.DbProviders
{
    public class SqliteSchemaProvider : ISchemaProvider
    {
        public string ProviderName => "sqlite";

        public async Task<List<string>> GetTablesAsync(IDbConnection connection, string filter)
        {
            var query = @"SELECT name FROM sqlite_master 
                          WHERE type='table' AND name NOT LIKE 'sqlite_%'
                          AND (@Filter IS NULL OR name LIKE '%' || @Filter || '%')
                          ORDER BY name";
            return (await connection.QueryAsync<string>(query, new { Filter = filter })).ToList();
        }

        public async Task<List<string>> GetColumnsAsync(IDbConnection connection, string tableName, string filter)
        {
            // SQLite pragma commands don't support standard WHERE clauses easily via parameters, 
            // so we filter the returned collection in-memory.
            var query = $"PRAGMA table_info({tableName})"; 
            var rows = await connection.QueryAsync(query);
            
            var columns = rows.Select(r => (string)r.name).ToList();
            if (!string.IsNullOrEmpty(filter))
            {
                columns = columns.Where(c => c.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            return columns;
        }

        public async Task<Dictionary<string, Dictionary<string, string>>> GetForeignKeysAsync(IDbConnection connection, string tableName)
        {
            var query = $"PRAGMA foreign_key_list({tableName})";
            var rows = await connection.QueryAsync(query);

            var result = new Dictionary<string, Dictionary<string, string>>();
            foreach (var row in rows)
            {
                string targetTable = (string)row.table;
                string sourceColumn = (string)row.from;
                string targetColumn = (string)row.to;

                if (!result.ContainsKey(targetTable))
                    result[targetTable] = new Dictionary<string, string>();

                result[targetTable][sourceColumn] = targetColumn;
            }
            return result;
        }
    }
}