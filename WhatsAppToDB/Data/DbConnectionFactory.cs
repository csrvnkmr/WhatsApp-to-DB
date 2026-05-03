using Microsoft.Data.SqlClient;
using System.Data;

namespace WhatsAppToDB.Data
{
    public class DbConnectionFactory
    {
        public static IDbConnection CreateConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }
    }
}
