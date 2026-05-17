using Microsoft.Data.SqlClient;
using System.Data;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;

namespace VectorDBSync
{
    /// <summary>
    /// Simple SQL Server database provider implementation for VectorDBSync standalone usage.
    /// This provides an IDbProvider implementation that creates SQL Server connections.
    /// </summary>
    public class SqlServerDbProvider : IDbProvider
    {
        public string Name => "mssql";

        public IDbConnection GetConnection(string connectionString)
        {
            var conn = new SqlConnection(connectionString);
            return conn;
        }

        public async Task SetSessionContext(IDbConnection conn, IdentityContext identity)
        {
            // VectorDBSync doesn't require session context setting
            // This is a no-op implementation for standalone usage
            await Task.CompletedTask;
        }
    }
}
