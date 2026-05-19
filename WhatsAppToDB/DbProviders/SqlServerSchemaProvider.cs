using System.Data;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WhatsAppToDB.Abstractions;

namespace WhatsAppToDB.DbProviders
{
    public class SqlServerSchemaProvider : ISchemaProvider
    {
        public string ProviderName => "mssql";

        public async Task<List<string>> GetTablesAsync(IDbConnection connection, string filter)
        {
            // If the schema is 'dbo', we only return the TABLE_NAME. 
            // If it's anything else, we return 'SCHEMA.TABLE_NAME'.
            var query = @"SELECT 
                            CASE 
                                WHEN TABLE_SCHEMA = 'dbo' THEN TABLE_NAME 
                                ELSE TABLE_SCHEMA + '.' + TABLE_NAME 
                            END AS FullTableName
                          FROM INFORMATION_SCHEMA.TABLES 
                          WHERE TABLE_TYPE = 'BASE TABLE'
                          AND (@Filter IS NULL OR 
                              (CASE WHEN TABLE_SCHEMA = 'dbo' THEN TABLE_NAME ELSE TABLE_SCHEMA + '.' + TABLE_NAME END) LIKE '%' + @Filter + '%')
                          ORDER BY TABLE_SCHEMA, TABLE_NAME";

            return (await connection.QueryAsync<string>(query, new { Filter = filter })).ToList();
        }

        public async Task<List<string>> GetColumnsAsync(IDbConnection connection, string tableName, string filter)
        {
            var parts = SplitTableName(tableName);

            var query = @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS 
                          WHERE TABLE_SCHEMA = @SchemaName AND TABLE_NAME = @TableName 
                          AND (@Filter IS NULL OR COLUMN_NAME LIKE '%' + @Filter + '%')
                          ORDER BY ORDINAL_POSITION";
                          
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
                        WHEN OBJECT_SCHEMA_NAME(f.referenced_object_id) = 'dbo' THEN OBJECT_NAME(f.referenced_object_id)
                        ELSE OBJECT_SCHEMA_NAME(f.referenced_object_id) + '.' + OBJECT_NAME(f.referenced_object_id)
                    END AS TargetTable,
                    col1.name AS SourceColumn,
                    col2.name AS TargetColumn
                FROM sys.foreign_keys AS f
                INNER JOIN sys.foreign_key_columns AS fc ON f.object_id = fc.constraint_object_id
                INNER JOIN sys.columns AS col1 ON fc.parent_object_id = col1.object_id AND fc.parent_column_id = col1.column_id
                INNER JOIN sys.columns AS col2 ON fc.referenced_object_id = col2.object_id AND fc.referenced_column_id = col2.column_id
                WHERE OBJECT_SCHEMA_NAME(f.parent_object_id) = @SchemaName 
                AND OBJECT_NAME(f.parent_object_id) = @TableName";

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
                // No dot means the UI sent a clean name, meaning it natively maps to the default 'dbo' schema
                return ("dbo", fullName); 
            }
            return (fullName.Substring(0, dotIndex), fullName.Substring(dotIndex + 1));
        }
    }
}