using System.Data;
using Microsoft.Data.SqlClient; // Or Microsoft.Data.Sqlite / Npgsql depending on your DB
using Dapper; 
using WhatsAppToDB.Abstractions;

public class SqlDbStorageProvider : IConfigStorageProvider
{
    /*
    -- 1. Replaces your global files (Llms.json, Users.json, WhatsAppProfiles.json)
    -- Also replaces the "metadata/" subfolder files.
    CREATE TABLE GlobalConfigs (
        EntityName VARCHAR(100) PRIMARY KEY, -- e.g., "llms", "users", "metadata/users"
        ConfigJson TEXT NOT NULL,            -- The raw JSON text payload
        LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP
    );

    -- 2. Replaces your "databases/{databaseName}/{fileName}.json" layout
    CREATE TABLE DatabaseConfigs (
        DatabaseContext VARCHAR(100) NOT NULL, -- e.g., "chinook-postgres", "arm_welders_prod"
        EntityName      VARCHAR(100) NOT NULL, -- e.g., "tables", "modules", "systemprompt"
        ConfigJson      TEXT NOT NULL,         -- The raw JSON text payload
        LastUpdated     DATETIME DEFAULT CURRENT_TIMESTAMP,
        PRIMARY KEY (DatabaseContext, EntityName) -- Ensures uniqueness per database environment
    );
    */
    private readonly string _connectionString;

    public SqlDbStorageProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public string ReadGlobalConfigRaw(string entityName)
    {
        const string sql = "SELECT ConfigJson FROM GlobalConfigs WHERE EntityName = @EntityName";
        using var db = CreateConnection();
        return db.QueryFirstOrDefault<string>(sql, new { EntityName = entityName }) ?? "[]";
    }

    public string ReadDatabaseConfigRaw(string database, string entityName)
    {
        const string sql = "SELECT ConfigJson FROM DatabaseConfigs WHERE DatabaseContext = @Database AND EntityName = @EntityName";
        using var db = CreateConnection();
        return db.QueryFirstOrDefault<string>(sql, new { Database = database, EntityName = entityName }) ?? "[]";
    }

    public void WriteConfigRaw(string? database, string entityName, string rawContent)
    {
        using var db = CreateConnection();

        if (string.IsNullOrEmpty(database))
        {
            const string sql = @"
                MERGE GlobalConfigs AS Target
                USING (SELECT @EntityName AS EntityName) AS Source
                ON (Target.EntityName = Source.EntityName)
                WHEN MATCHED THEN UPDATE SET ConfigJson = @RawContent, LastUpdated = GETDATE()
                WHEN NOT MATCHED THEN INSERT (EntityName, ConfigJson) VALUES (@EntityName, @RawContent);";
                
            db.Execute(sql, new { EntityName = entityName, RawContent = rawContent });
        }
        else
        {
            const string sql = @"
                MERGE DatabaseConfigs AS Target
                USING (SELECT @Database AS DatabaseContext, @EntityName AS EntityName) AS Source
                ON (Target.DatabaseContext = Source.DatabaseContext AND Target.EntityName = Source.EntityName)
                WHEN MATCHED THEN UPDATE SET ConfigJson = @RawContent, LastUpdated = GETDATE()
                WHEN NOT MATCHED THEN INSERT (DatabaseContext, EntityName, ConfigJson) VALUES (@Database, @EntityName, @RawContent);";

            db.Execute(sql, new { Database = database, EntityName = entityName, RawContent = rawContent });
        }
    }
}