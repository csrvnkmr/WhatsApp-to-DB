using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;

namespace WhatsAppToDB.Database
{
    public class MsSqlDbProvider : IDbProvider
    {
        public IDbConnection GetConnection(string connectionString)
        {
            
            var conn = new SqlConnection(connectionString);
            return conn;
        }

        public string Name => "mssql";

        public async Task SetSessionContext(IDbConnection conn, IdentityContext identity)
        {
            string contextKey = identity.GetActiveContextKey();

            // In SQL Server, we set the session context
            string sql = "EXEC sp_set_session_context @Key, @Value, @read_only = 1;";

            await conn.ExecuteAsync(sql, new
            {
                Key = contextKey,
                Value = identity.InternalUserId
            });
        }
    }
}
