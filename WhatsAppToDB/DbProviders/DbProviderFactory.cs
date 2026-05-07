using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;

namespace WhatsAppToDB.Database
{
    public class DbProviderFactory
    {
        private readonly IEnumerable<IDbProvider> _providers;

        public DbProviderFactory(IEnumerable<IDbProvider> providers)
        {
            _providers = providers;
        }

        public IDbProvider GetDbProvider(string name)
        {
            return _providers.First(p =>
                p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
