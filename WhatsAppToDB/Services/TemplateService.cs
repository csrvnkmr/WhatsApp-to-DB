using Dapper;
using Microsoft.Data.SqlClient;
using WhatsAppToDB.Data;
using WhatsAppToDB.Database;

namespace WhatsAppToDB.Services
{
    public class TemplateService
    {
        private readonly ILogger _logger;
        private readonly DatabaseContextService _dbContextService;

        public TemplateService(DatabaseContextService databaseContextService, ILogger? logger = null)
        {
            _dbContextService = databaseContextService;
            _logger = logger ?? new AppLogger();
        }

        public async Task<List<FewShotQuery>> GetTemplatesByModule(string module)
        {
            using var db = _dbContextService.CreateConnection();
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
