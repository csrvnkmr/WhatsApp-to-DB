using System.Threading.Tasks;

namespace WhatsAppToDB.Abstractions
{
    public interface IConfigStorageProvider
    {
        /// <summary>Reads a global configuration asset into a string.</summary>
        string ReadGlobalConfigRaw(string entityName);

        /// <summary>Reads a database-specific configuration asset into a string.</summary>
        string ReadDatabaseConfigRaw(string database, string entityName);

        /// <summary>Writes an updated asset string back to targeted global or database storage spaces.</summary>
        void WriteConfigRaw(string? database, string entityName, string rawContent);
    }
}