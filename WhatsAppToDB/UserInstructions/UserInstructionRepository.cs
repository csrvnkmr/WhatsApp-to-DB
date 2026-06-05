using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using WhatsAppToDB.Data;
using WhatsAppToDB.Services;

public class UserInstructionRepository
{
    
    private readonly JsonConfigService _jsonConfigService;
    private readonly FolderUtils _folderUtils;

    public UserInstructionRepository(JsonConfigService jsonConfigService, FolderUtils folderUtils) 
    { 
        _jsonConfigService = jsonConfigService;
        _folderUtils = folderUtils;
        EnsureTablesExist();
    }

    private void EnsureTablesExist()
    {
        UserInstructionTableSqls.CreateTablesAsync(GetConnection()).GetAwaiter().GetResult();
    }
    
    private IDbConnection GetConnection()
    {
        var dbFile = _folderUtils.GetChatHistoryDBPath();
        var connString = $"Data Source={dbFile}";
        return new SqliteConnection(connString);
    }

    // ── Persistent ────────────────────────────────────────────────────────
    public async Task AddPersistentAsync(UserInstruction instruction)
    {
        using var _db = GetConnection();
        await _db.ExecuteAsync("""
            INSERT INTO user_instruction (username, database_id, instruction_text)
            VALUES (@username, @databaseId, @instruction)
        """, new { username = instruction.Username, databaseId = instruction.Database ?? "", instruction = instruction.InstructionText });
    }

    public async Task<List<string>> GetPersistentInstructionsAsync(string username, string databaseId)
    {
        using var _db = GetConnection();
        var rows = await _db.QueryAsync<string>("""
            SELECT instruction_text FROM user_instruction
            WHERE username = @username
              AND (database_id = @databaseId OR database_id = '' or database_id IS NULL)
              AND is_active = 1
            ORDER BY user_instruction_id
        """, new { username, databaseId });
        return rows.ToList();
    }

    public async Task<List<UserInstruction>> GetPersistentAsync(string username, string databaseId)
    {
        using var _db = GetConnection();
        var rows = await _db.QueryAsync<UserInstruction>("""
            SELECT user_instruction_id AS Id,
                   username,
                   database_id AS Database,
                   instruction_text AS InstructionText,
                   is_active AS IsActive
            FROM user_instruction
            WHERE username = @username
              AND (database_id = @databaseId OR database_id = '' OR database_id IS NULL)
              AND is_active = 1
            ORDER BY user_instruction_id
        """, new { username, databaseId });
        return rows.ToList();
    }

    public async Task<UserInstruction?> GetPersistentAsync(int instructionId)
    {
        using var _db = GetConnection();
        var row = await _db.QueryFirstOrDefaultAsync<UserInstruction>("""
            SELECT user_instruction_id AS Id,
                   username,
                   database_id AS Database,
                   instruction_text AS InstructionText,
                   is_active AS IsActive
            FROM user_instruction
            WHERE user_instruction_id = @instructionId
              AND is_active = 1
            LIMIT 1
        """, new { instructionId });
        return row;
    }

    public async Task UpdatePersistentAsync(UserInstruction instruction)
    {
        using var _db = GetConnection();
        await _db.ExecuteAsync("""
            UPDATE user_instruction
            SET database_id = @Database,
                instruction_text = @InstructionText
            WHERE user_instruction_id = @Id
        """, new
        {
            instruction.Username,
            Database = instruction.Database ?? string.Empty,
            instruction.InstructionText,
            instruction.IsActive,
            instruction.Id
        });
    }

    public async Task ClearPersistentAsync(string username, string databaseId)
    {
        using var _db = GetConnection();
        await _db.ExecuteAsync("""
            UPDATE user_instruction SET is_active = 0
            WHERE username = @username AND database_id = @databaseId
        """, new { username, databaseId });
    }

    public async Task RemovePersistentAsync(int instructionId)
    {
        using var _db = GetConnection();
        await _db.ExecuteAsync("""
            UPDATE user_instruction SET is_active = 0
            WHERE user_instruction_id = @instructionId
        """, new { instructionId });
    }

    // ── Session-level ─────────────────────────────────────────────────────
    public async Task AddSessionAsync(long sessionId, string username, string instruction)
    {
        using var _db = GetConnection();
        await _db.ExecuteAsync("""
            INSERT INTO user_session_instruction (session_id, username, instruction_text)
            VALUES (@sessionId, @username, @instruction)
        """, new { sessionId, username, instruction });
    }

    public async Task<List<string>> GetSessionAsync(long sessionId)
    {
        using var _db = GetConnection();
        var rows = await _db.QueryAsync<string>("""
            SELECT instruction_text FROM user_session_instruction
            WHERE session_id = @sessionId            
            ORDER BY id
        """, new { sessionId });
        return rows.ToList();
    }

    public async Task ClearSessionAsync(long sessionId)
    {
        using var _db = GetConnection();
        await _db.ExecuteAsync("""
            DELETE FROM user_session_instruction WHERE session_id = @sessionId
        """, new { sessionId });
    }

    // ── Combined (used at prompt assembly time) ───────────────────────────
    public async Task<List<string>> GetAllActiveAsync(
        long sessionId, string username, string databaseId)
    {
        using var _db = GetConnection();
        var persistent = await GetPersistentInstructionsAsync(username, databaseId);
        var session    = await GetSessionAsync(sessionId);
        return persistent.Concat(session).ToList();
    }
}