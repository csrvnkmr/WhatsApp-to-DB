using System.Data;
using Dapper;

public class UserInstructionTableSqls
{

    public static async Task CreateTablesAsync(IDbConnection db)
    {
        var sql = $@"
            {Createuser_instructionTable}
            {Createuser_instructionIndex}
            {Createuser_session_instructionTable}
            {Createuser_session_instructionIndex}
        ";
        await db.ExecuteAsync(sql);
        return;
    }


    public const string Createuser_instructionTable = @"
    -- Persistent instructions (survive logout)
    CREATE TABLE if not exists user_instruction (
        user_instruction_id  INTEGER PRIMARY KEY AUTOINCREMENT,
        username             TEXT    NOT NULL,
        database_id          TEXT    NOT NULL DEFAULT '',
        instruction_text     TEXT    NOT NULL,
        created_at           DATETIME NOT NULL DEFAULT (datetime('now')),
        is_active            INTEGER NOT NULL DEFAULT 1
    );";
    public const string Createuser_instructionIndex = @"
    CREATE INDEX if not exists idx_user_instruction_user ON user_instruction(username, database_id);";
    public const string Createuser_session_instructionTable = @"

    -- Session-level instructions (cleared on new session)
    CREATE TABLE if not exists user_session_instruction (
        id          INTEGER PRIMARY KEY AUTOINCREMENT,
        session_id  INTEGER NOT NULL,
        username    TEXT    NOT NULL,
        instruction_text TEXT NOT NULL,
        created_at  DATETIME NOT NULL DEFAULT (datetime('now'))
    );";
    public const string Createuser_session_instructionIndex = @"
    CREATE INDEX if not exists idx_session_instruction ON user_session_instruction(session_id);
    ";
}
