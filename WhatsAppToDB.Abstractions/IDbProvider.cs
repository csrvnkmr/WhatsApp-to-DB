using System.Data;
using WhatsAppToDB.Abstractions;

namespace WhatsAppToDB.Abstractions
{
    public interface IDbProvider
    {
        string Name { get; }
        IDbConnection GetConnection(string connectionString);
        Task SetSessionContext(IDbConnection conn, IdentityContext identity);
    }
}
