using System.Data;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;

namespace WhatsAppToDB.Database
{
    public class SqliteDbProvider : IDbProvider
    {
        public SqliteDbProvider() { }

        public IDbConnection GetConnection(string connectionString)
        {
            var conn = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            return conn;
        }

        public string Name => "sqlite";

        public Task SetSessionContext(IDbConnection conn, IdentityContext identity)
        {
            return Task.CompletedTask;
        }
    }
}
