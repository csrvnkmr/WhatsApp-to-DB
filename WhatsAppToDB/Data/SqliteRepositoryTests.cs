using Microsoft.Data.Sqlite;
using NPOI.SS.Formula.Functions;
using System;
using System.IO;
using System.Threading.Tasks;
using WhatsAppToDB.Data;
using Xunit;

namespace WhatsAppToDB.Tests
{
    public class SqliteRepositoryTests
    {
        private readonly string _dbPath;

        public SqliteRepositoryTests()
        {
            _dbPath = Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                $"{System.Guid.NewGuid().ToString().Substring(0, 6)}_InsightChat.db");
        }

        [Fact]
        public async Task Full_Sqlite_Test_Create_Insert_Select_Delete()
        {
            // ---------------------------------------------
            // Arrange
            // ---------------------------------------------
            DeleteDatabaseIfExists();

            var dbname = Path.GetFileName(_dbPath);

            var repo = new ChatDbRepository(dbname);

            // ---------------------------------------------
            // Create DB + Tables
            // ---------------------------------------------
            await repo.InitializeAsync();

            Assert.True(File.Exists(_dbPath));

            // ---------------------------------------------
            // Insert Session
            // ---------------------------------------------
            var sessionId = await repo.CreateSessionAsync(
                "admin",
                "What is my total sales?"
            );

            Assert.True(sessionId > 0);

            // ---------------------------------------------
            // Insert Messages
            // ---------------------------------------------
            await repo.InsertMessageAsync(
                sessionId,
                "user",
                "What is my total sales?", "", ""
            );

            await repo.InsertMessageAsync(
                sessionId,
                "assistant",
                "Your total sales is 48204665.21", "", ""
            );

            // ---------------------------------------------
            // Select Sessions
            // ---------------------------------------------
            var sessions = await repo.GetSessionsAsync("admin");

            Assert.NotNull(sessions);
            Assert.True(sessions.Count > 0);
            Assert.Equal("admin", sessions[0].UserName);

            // ---------------------------------------------
            // Select Messages
            // ---------------------------------------------
            var messages = await repo.GetMessagesAsync(sessionId);

            Assert.NotNull(messages);
            Assert.Equal(2, messages.Count);
            Assert.Equal("user", messages[0].Role);
            Assert.Equal("assistant", messages[1].Role);

            // ---------------------------------------------
            // Delete DB File
            // ---------------------------------------------
            DeleteDatabaseIfExists();

            Assert.False(File.Exists(_dbPath));
        }

        private void DeleteDatabaseIfExists()
        {
            if (!File.Exists(_dbPath))
                return;

            SqliteConnection.ClearAllPools();
            // Force release unmanaged handles
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            const int maxRetry = 10;

            for (int i = 0; i < maxRetry; i++)
            {
                try
                {
                    if (File.Exists(_dbPath))
                    {
                        File.SetAttributes(_dbPath, FileAttributes.Normal);
                        File.Delete(_dbPath);
                    }

                    return;
                }
                catch (IOException)
                {
                    Task.Delay(300).Wait();
                }
                catch (UnauthorizedAccessException)
                {
                    Task.Delay(300).Wait();
                }
            }

            throw new Exception("Unable to delete SQLite database file after retries.");
        }
    }
}