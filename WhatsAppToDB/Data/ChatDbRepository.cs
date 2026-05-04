using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace WhatsAppToDB.Data
{
    // ==========================================================
    // DTOs
    // ==========================================================
    public class ChatSessionDto
    {
        public long Id { get; set; }
        public string UserName { get; set; } = "";
        public string Title { get; set; } = "";
        public string CreatedOn { get; set; } = "";
        public string UpdatedOn { get; set; } = "";
    }

    public class ChatMessageDto
    {
        public long Id { get; set; }
        public long SessionId { get; set; }
        public string Role { get; set; } = "";
        public string MessageText { get; set; } = "";
        public string CreatedOn { get; set; } = "";
        public bool CanShowSql { get; set; } = false;
        public bool CanShowChart { get; set; } = false;
        public bool CanShowData { get; set; } = false;
        public bool isBookmarked { get; set; } = false;

    }

    public class ChatMessageExtraDto
    {
        public long Id { get; set; }

        public string SqlText { get; set; } = "";

        public string DataFileName { get; set; } = "";

        public bool CanShowSql { get; set; }

        public bool CanShowData { get; set; }
    }

    // ==========================================================
    // REPOSITORY
    // ==========================================================
    public class ChatDbRepository
    {
        private readonly string _connectionString;

        private readonly ILogger logger;

        public ChatDbRepository(ILogger? logger = null, string filename="chathistory.db")
        {
            this.logger = logger ?? new AppLogger();

            var folder = FolderUtls.GetDataFolder() ;

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var dbPath = FolderUtls.GetSqliteDBPath(); 
            if (!string.IsNullOrWhiteSpace( filename))
            {
                dbPath = Path.Combine(folder, filename);
            }

            _connectionString = $"Data Source={dbPath}";
            this.logger.LogInfoAsync($"Database initialized at {dbPath}");
        }

        private SqliteConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        // ======================================================
        // CREATE TABLES
        // ======================================================
        public async Task InitializeAsync()
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            await conn.ExecuteAsync(SqliteSqls.CreateTables);
            await SqliteMigration.ApplyMigrationsAsync(conn, logger);

        }

        public async Task<ChatMessageExtraDto?> GetMessageExtrasAsync(long messageId, string userName)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var row =
                await conn.QueryFirstOrDefaultAsync<ChatMessageExtraDto>(
                    SqliteSqls.GetMessageExtrasById,
                    new
                    {
                        MessageId = messageId,
                        UserName = userName
                    });

            return row;
        }

        // ======================================================
        // CREATE NEW SESSION
        // ======================================================
        public async Task<long> CreateSessionAsync(
            string userName,
            string firstQuestion)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var title = firstQuestion.Length > 80
                ? firstQuestion.Substring(0, 80)
                : firstQuestion;

            var id = await conn.ExecuteScalarAsync<long>(
                SqliteSqls.InsertChatSession,
                new
                {
                    UserName = userName,
                    Title = title,
                    CreatedOn = now,
                    UpdatedOn = now
                });

            return id;
        }

        // ======================================================
        // INSERT MESSAGE
        // ======================================================
        public async Task<long> InsertMessageAsync(
            long sessionId,
            string role,
            string messageText, string sql, string datapath)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var msgid = await conn.ExecuteScalarAsync<long>(
                SqliteSqls.InsertChatMessage,
                new
                {
                    SessionId = sessionId,
                    Role = role,
                    MessageText = messageText,
                    CreatedOn = now,
                    UpdatedOn = now,
                    SqlText = sql,
                    DataFileName = datapath,
                    CanShowSql = 1,
                    CanShowData = 1,
                    CanShowChart = 0
                });
            return msgid;
        }

        // ======================================================
        // GET CHAT HISTORY (LEFT PANEL)
        // ======================================================
        public async Task<List<ChatSessionDto>> GetSessionsAsync(
            string userName)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var rows = await conn.QueryAsync<ChatSessionDto>(
                SqliteSqls.GetChatSessionsByUserName,
                new { UserName = userName });

            return rows.AsList();
        }

        // ======================================================
        // GET CHAT DETAILS
        // ======================================================
        public async Task<List<ChatMessageDto>> GetMessagesAsync(
            long sessionId)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var rows = await conn.QueryAsync<ChatMessageDto>(
                SqliteSqls.GetChatMessagesBySessionId,
                new { SessionId = sessionId });

            return rows.AsList();
        }

        public async Task AddBookmarkAsync(
    string userName,
    long messageId,
    string text)
        {
            using var con = GetConnection();

            await con.ExecuteAsync(
                SqliteSqls.AddBookmark,
                new
                {
                    UserName = userName,
                    MessageId = messageId,
                    BookmarkText = text,
                    CreatedOn = DateTime.UtcNow.ToString("s")
                });
        }

        public async Task RemoveBookmarkAsync(
            string userName,
            long messageId)
        {
            using var con = GetConnection();

            await con.ExecuteAsync(
                SqliteSqls.RemoveBookmark,
                new
                {
                    UserName = userName,
                    MessageId = messageId
                });
        }

        public async Task<IEnumerable<dynamic>> GetBookmarksAsync(
            string userName)
        {
            using var con = GetConnection();

            return await con.QueryAsync(
                SqliteSqls.GetBookmarks,
                new { UserName = userName });
        }

        public async Task<IEnumerable<dynamic>> SearchMessagesAsync(
            string userName, string text)
        {
            using var con = GetConnection();

            return await con.QueryAsync(
                SqliteSqls.SearchMessages,
                new
                {
                    UserName = userName,
                    Text = text
                });
        }
    }
}