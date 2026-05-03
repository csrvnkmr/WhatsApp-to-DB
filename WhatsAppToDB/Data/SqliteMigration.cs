using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace WhatsAppToDB.Data
{
    public static class SqliteMigration
    {
        // =====================================================
        // CALL THIS DURING APP STARTUP
        // =====================================================
        public static async Task ApplyMigrationsAsync(string connectionString, ILogger logger)
        {
            using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync();
            await ApplyMigrationsAsync(conn, logger);
        }

        public static async Task ApplyMigrationsAsync(SqliteConnection conn, ILogger logger)
        {

            await AddColumnIfMissing(
                conn,
                "ChatMessage",
                "SqlText",
                "TEXT NULL");

            await AddColumnIfMissing(
                conn,
                "ChatMessage",
                "DataFileName",
                "TEXT NULL");

            await AddColumnIfMissing(
                conn,
                "ChatMessage",
                "ChartFileName",
                "TEXT NULL");

            await AddColumnIfMissing(
                conn,
                "ChatMessage",
                "CanShowSql",
                "INTEGER NOT NULL DEFAULT 0");

            await AddColumnIfMissing(
                conn,
                "ChatMessage",
                "CanShowData",
                "INTEGER NOT NULL DEFAULT 0");

            await AddColumnIfMissing(
                conn,
                "ChatMessage",
                "CanShowChart",
                "INTEGER NOT NULL DEFAULT 0");
        }

        // =====================================================
        // ADD COLUMN ONLY IF NOT EXISTS
        // =====================================================
        private static async Task AddColumnIfMissing(
            SqliteConnection conn,
            string tableName,
            string columnName,
            string definition)
        {
            var exists = await ColumnExists(
                conn,
                tableName,
                columnName);

            if (exists)
                return;

            var sql =
                $"ALTER TABLE {tableName} " +
                $"ADD COLUMN {columnName} {definition};";

            await conn.ExecuteAsync(sql);
        }

        // =====================================================
        // CHECK COLUMN EXISTS
        // =====================================================
        private static async Task<bool> ColumnExists(
            SqliteConnection conn,
            string tableName,
            string columnName)
        {
            var sql =
                $"PRAGMA table_info({tableName});";

            var rows = await conn.QueryAsync<TableInfoRow>(sql);

            foreach (var row in rows)
            {
                if (row.name.Equals(
                    columnName,
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private class TableInfoRow
        {
            public int cid { get; set; }
            public string name { get; set; } = "";
            public string type { get; set; } = "";
            public int notnull { get; set; }
            public string dflt_value { get; set; } = "";
            public int pk { get; set; }
        }
    }
}