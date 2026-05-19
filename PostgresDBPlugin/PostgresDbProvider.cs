using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsAppToDB.Abstractions;

namespace PostgresDBPlugin
{
    public class PostgresDbProvider : IDbProvider
    {

        public string Name => "postgres";

        public IDbConnection GetConnection(string connectionString)
        {
            var conn = new Npgsql.NpgsqlConnection(connectionString);
            return conn;
        }

        public Task SetSessionContext(IDbConnection conn, IdentityContext identity)
        {
            return  Task.CompletedTask;
        }
    }
}
