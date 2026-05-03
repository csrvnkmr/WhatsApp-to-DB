using Dapper;
using Microsoft.Data.SqlClient;
using WhatsAppToDB.Data;

namespace WhatsAppToDB.Services
{
    public class TemplateService
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;

        public TemplateService(string connectionString, ILogger? logger = null)
        {
            _connectionString = connectionString;
            _logger = logger ?? new AppLogger();
        }

        public async Task<List<FewShotQuery>> GetTemplatesByModule(string module)
        {
            using var db = DbConnectionFactory.CreateConnection(_connectionString);
            // We pull the descriptions so the AI can choose the right one
            return (await db.QueryAsync<FewShotQuery>(
                "SELECT id, Module, QueryDescription, QueryText FROM FewShotQueries WHERE Module = @module",
                new { module })).ToList();
        }
    }

    public class FewShotQuery
    {
        public int Id { get; set; }
        public string Module { get; set; } = "";
        public string QueryDescription { get; set; } = "";
        public string QueryText { get; set; } = "";
    }
}
