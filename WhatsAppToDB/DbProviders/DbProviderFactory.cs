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
        private readonly IEnumerable<ISchemaProvider> _schemaProviders;

        public DbProviderFactory(IEnumerable<IDbProvider> providers, IEnumerable<ISchemaProvider> schemaProviders)
        {
            _providers = providers;
            _schemaProviders = schemaProviders;
        }

        public IDbProvider GetDbProvider(string name)
        {
            return _providers.First(p =>
                p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public ISchemaProvider GetSchemaProvider(string name)
        {
            return _schemaProviders.First(p =>
                p.ProviderName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
