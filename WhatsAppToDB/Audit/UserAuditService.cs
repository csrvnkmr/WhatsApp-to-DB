using Dapper;
using Microsoft.AspNetCore.Connections;
using Microsoft.Data.Sqlite;
using System.Data;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.Audit
{
    public class UserAuditService : IUserAuditService
    {
        private readonly ILogger logger;
        private readonly string _connectionString;
        private readonly FolderUtils _folderUtils;

        public UserAuditService(FolderUtils folderUtils, ILogger? logger = null, string filename = "chathistory.db")
        {
            _folderUtils = folderUtils;
            this.logger = logger ?? new AppLogger();

            var folder = _folderUtils.GetDataFolder();

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var dbPath = _folderUtils.GetSqliteDBPath();
            if (!string.IsNullOrWhiteSpace(filename))
            {
                dbPath = Path.Combine(folder, filename);
            }

            _connectionString = $"Data Source={dbPath}";
            this.logger.LogInfoAsync($"User Audit Database initialized at {dbPath}");
        }

        private SqliteConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        public async Task LogAsync(
            string userId,
            string actionType,
            string? actionValue = null)
        {
            using var conn = GetConnection();

            await conn.ExecuteAsync(AuditSqls.InsertAudit,
            new
            {
                UserId = userId,
                ActionType = actionType,
                ActionValue = actionValue
            });
        }

        public async Task<string?> GetLatestValueAsync(
            string userId,
            string actionType)
        {
            using var conn = GetConnection();

            return await conn.QueryFirstOrDefaultAsync<string>(@"
SELECT ActionValue
FROM UserAudit
WHERE UserId = @UserId
AND ActionType = @ActionType
ORDER BY CreatedOn DESC
LIMIT 1",
            new
            {
                UserId = userId,
                ActionType = actionType
            });
        }
    }
}
