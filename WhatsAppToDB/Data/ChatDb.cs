
using System;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;

namespace WhatsAppToDB.Data
{
    public static class ChatDb
    {
        private static readonly string DbFolder =
            Path.Combine(AppContext.BaseDirectory, "Data");

        private static readonly string DbPath =
            Path.Combine(DbFolder, "InsightChat.db");

        private static readonly string ConnectionString =
            $"Data Source={DbPath}";

        // =========================================================
        // CALL THIS ON API STARTUP
        // =========================================================
        public static void Initialize()
        {
            if (!Directory.Exists(DbFolder))
                Directory.CreateDirectory(DbFolder);

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var sql = @"
CREATE TABLE IF NOT EXISTS ChatSession
(
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    UserName        TEXT NOT NULL,
    Title           TEXT NOT NULL,
    CreatedOn       TEXT NOT NULL,
    UpdatedOn       TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ChatMessage
(
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    SessionId       INTEGER NOT NULL,
    Role            TEXT NOT NULL,      -- user / assistant
    MessageText     TEXT NOT NULL,
    CreatedOn       TEXT NOT NULL,
    FOREIGN KEY(SessionId) REFERENCES ChatSession(Id)
);

CREATE INDEX IF NOT EXISTS IX_ChatMessage_SessionId
ON ChatMessage(SessionId);

CREATE INDEX IF NOT EXISTS IX_ChatSession_UserName
ON ChatSession(UserName);
";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        // =========================================================
        // CREATE NEW CHAT SESSION
        // =========================================================
        public static long CreateSession(string userName, string firstQuestion)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string title = firstQuestion.Length > 60
                ? firstQuestion.Substring(0, 60)
                : firstQuestion;

            var sql = @"
INSERT INTO ChatSession
(UserName, Title, CreatedOn, UpdatedOn)
VALUES
($UserName, $Title, $CreatedOn, $UpdatedOn);

SELECT last_insert_rowid();
";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("$UserName", userName);
            cmd.Parameters.AddWithValue("$Title", title);
            cmd.Parameters.AddWithValue("$CreatedOn", now);
            cmd.Parameters.AddWithValue("$UpdatedOn", now);

            return (long)cmd.ExecuteScalar();
        }

        // =========================================================
        // INSERT MESSAGE
        // =========================================================
        public static void InsertMessage(
            long sessionId,
            string role,
            string messageText)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var sql = @"
INSERT INTO ChatMessage
(SessionId, Role, MessageText, CreatedOn)
VALUES
($SessionId, $Role, $MessageText, $CreatedOn);

UPDATE ChatSession
SET UpdatedOn = $UpdatedOn
WHERE Id = $SessionId;
";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("$SessionId", sessionId);
            cmd.Parameters.AddWithValue("$Role", role);
            cmd.Parameters.AddWithValue("$MessageText", messageText);
            cmd.Parameters.AddWithValue("$CreatedOn", now);
            cmd.Parameters.AddWithValue("$UpdatedOn", now);

            cmd.ExecuteNonQuery();
        }

        // =========================================================
        // GET LEFT PANEL HISTORY
        // =========================================================
        public static DataTable GetSessions(string userName)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var sql = @"
SELECT Id,
       Title,
       CreatedOn,
       UpdatedOn
FROM ChatSession
WHERE UserName = $UserName
ORDER BY UpdatedOn DESC;
";

            var sqliteCmd = new SqliteCommand(sql, conn);
            sqliteCmd.Parameters.AddWithValue("$UserName", userName);
            var reader = sqliteCmd.ExecuteReader();
            var dt = new DataTable();
            dt.Load(reader);
            return dt;
        }

        // =========================================================
        // GET CHAT DETAILS
        // =========================================================
        public static DataTable GetMessages(long sessionId)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var sql = @"SELECT Id,
                Role, MessageText, CreatedOn FROM ChatMessage
                WHERE SessionId = $SessionId ORDER BY Id;";
            
            var sqliteCmd = new SqliteCommand(sql, conn);
            sqliteCmd.Parameters.AddWithValue("$SessionId", sessionId);
            var reader = sqliteCmd.ExecuteReader();            
            var dt = new DataTable();
            dt.Load(reader);             
            return dt;
        }
    }
}
