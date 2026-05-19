using System.Data;
using Dapper;
using WhatsAppToDB.Abstractions;

namespace PostgresDBPlugin
{
    public class PostgresSchemaProvider : ISchemaProvider
    {
        public string ProviderName => "postgres";

        public async Task<List<string>> GetTablesAsync(IDbConnection connection, string filter)
        {
            var query = @"SELECT 
                            CASE 
                                WHEN table_schema = 'public' THEN table_name 
                                ELSE table_schema || '.' || table_name 
                            END AS FullTableName
                          FROM information_schema.tables 
                          WHERE table_schema NOT IN ('information_schema', 'pg_catalog') 
                          AND (@Filter IS NULL OR 
                              (CASE WHEN table_schema = 'public' THEN table_name ELSE table_schema || '.' || table_name END) ILIKE '%' || @Filter || '%')
                          ORDER BY table_schema, table_name";

            return (await connection.QueryAsync<string>(query, new { Filter = filter })).ToList();
        }

        public async Task<List<string>> GetColumnsAsync(IDbConnection connection, string tableName, string filter)
        {
            var parts = SplitTableName(tableName);

            var query = @"SELECT column_name FROM information_schema.columns 
                          WHERE table_schema = @SchemaName AND table_name = @TableName 
                          AND (@Filter IS NULL OR column_name ILIKE '%' || @Filter || '%')
                          ORDER BY ordinal_position";
            return (await connection.QueryAsync<string>(query, new { 
                SchemaName = parts.Schema, 
                TableName = parts.Table, 
                Filter = filter 
            })).ToList();
        }

        public async Task<Dictionary<string, Dictionary<string, string>>> GetForeignKeysAsync(IDbConnection connection, string tableName)
        {
            var parts = SplitTableName(tableName);

            var query = @"
                SELECT
                    CASE 
                        WHEN ccu.table_schema = 'public' THEN ccu.table_name
                        ELSE ccu.table_schema || '.' || ccu.table_name
                    END AS TargetTable,
                    kcu.column_name AS SourceColumn,
                    ccu.column_name AS TargetColumn
                FROM information_schema.table_constraints AS tc
                JOIN information_schema.key_column_usage AS kcu
                    ON tc.constraint_name = kcu.constraint_name AND tc.table_schema = kcu.table_schema
                JOIN information_schema.constraint_column_usage AS ccu
                    ON ccu.constraint_name = tc.constraint_name AND ccu.table_schema = tc.table_schema
                WHERE tc.constraint_type = 'FOREIGN KEY' 
                AND tc.table_schema = @SchemaName 
                AND tc.table_name = @TableName";

            var rows = await connection.QueryAsync<(string TargetTable, string SourceColumn, string TargetColumn)>(query, new { 
                SchemaName = parts.Schema, 
                TableName = parts.Table 
            });
            
            var result = new Dictionary<string, Dictionary<string, string>>();
            foreach (var row in rows)
            {
                if (!result.ContainsKey(row.TargetTable))
                    result[row.TargetTable] = new Dictionary<string, string>();
                
                result[row.TargetTable][row.SourceColumn] = row.TargetColumn;
            }
            return result;
        }

        private (string Schema, string Table) SplitTableName(string fullName)
        {
            var dotIndex = fullName.IndexOf('.');
            if (dotIndex == -1)
            {
                // No dot means it falls back to the default 'public' schema
                return ("public", fullName);
            }
            return (fullName.Substring(0, dotIndex), fullName.Substring(dotIndex + 1));
        }
    }
}