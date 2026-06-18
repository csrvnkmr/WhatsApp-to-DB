namespace WhatsAppToDB.Data
{
    public class SqliteSqls
    {
        public const string GetChatMessagesBySessionId = @"
SELECT cm.Id,
       cm.SessionId,
       cm.Role,
       cm.MessageText,
       cm.CreatedOn, cm.CanShowSql, cm.CanShowData, cm.CanShowChart, case when cb.id is not null then true else false end as isBookmarked
FROM ChatMessage cm left join ChatBookmark cb on cm.Id=cb.MessageId and cb.isActive=1
WHERE cm.SessionId = $SessionId
ORDER BY cm.Id;
";

        public const string GetChatMessagesBySessionIdDatabases = @"
SELECT cm.Id,
       cm.SessionId,
       cm.Role,
       cm.MessageText,
       cm.CreatedOn, cm.CanShowSql, cm.CanShowData, cm.CanShowChart, case when cb.id is not null then true else false end as isBookmarked
FROM ChatMessage cm left join ChatBookmark cb on cm.Id=cb.MessageId and cb.isActive=1
WHERE cm.SessionId = $SessionId and cm.DatabaseName IN @Databases
ORDER BY cm.Id;
";

        public const string GetChatSessionsByUserName = @"
SELECT Id,
       UserName,
       Title,
       CreatedOn,
       UpdatedOn
FROM ChatSession
WHERE UserName = $UserName
ORDER BY UpdatedOn DESC;
";

        public const string GetChatSessionsByUserNameAndDatabases = @"
SELECT distinct s.Id,
       s.UserName,
       s.Title,
       s.CreatedOn,
       s.UpdatedOn
FROM ChatSession s
INNER JOIN ChatMessage m
    ON s.Id = m.SessionId
WHERE s.UserName = $UserName
and m.DatabaseName IN @Databases
ORDER BY UpdatedOn DESC;
";

        public const string GetChatHistoryForLlm = """
                WITH ForwardLookedMessages AS (
                    SELECT 
                        Id, 
                        Role, 
                        MessageText,
                        -- Look FORWARD exactly 1 row to see what comes NEXT
                        LEAD(Role) OVER (PARTITION BY SessionId ORDER BY Id) as NextRole,
                        -- Get the ID of the message that comes NEXT
                        LEAD(Id) OVER (PARTITION BY SessionId ORDER BY Id) as NextId
                    FROM ChatMessage
                    WHERE SessionId = @SessionId
                ),
                ExcludedAssistantIds AS (
                    -- Collect the IDs of assistant responses that immediately follow a command
                    SELECT NextId
                    FROM ForwardLookedMessages
                    WHERE MessageText LIKE '!%' 
                    AND LOWER(NextRole) = 'assistant' -- Case-insensitive protection
                    AND NextId IS NOT NULL
                )
                SELECT Id, Role, MessageText
                FROM ChatMessage
                WHERE SessionId = @SessionId
                -- 1. Exclude the user instructions
                AND MessageText NOT LIKE '!%'
                -- 2. Exclude the assistant responses to those instructions
                AND Id NOT IN (SELECT NextId FROM ExcludedAssistantIds)
                ORDER BY Id DESC
                LIMIT @Limit
            """;

        public const string InsertChatMessage = @"
INSERT INTO ChatMessage
(SessionId, Role, MessageText, CreatedOn, SqlText, DataFileName, ChartFileName, CanShowSql, CanShowData, CanShowChart, 
DatabaseName, LlmProvider, LlmModel, ModuleName)
VALUES
($SessionId, $Role, $MessageText, $CreatedOn, $SqlText, $DataFileName, null, $CanShowSql, $CanShowData, $CanShowChart, 
$DatabaseName, $LlmProvider, $LlmModel, $ModuleName);
SELECT last_insert_rowid();
UPDATE ChatSession SET UpdatedOn = $UpdatedOn WHERE Id = $SessionId;
";
        public const string InsertChatSession = @"
INSERT INTO ChatSession
(UserName, Title, CreatedOn, UpdatedOn)
VALUES
($UserName, $Title, $CreatedOn, $UpdatedOn);

SELECT last_insert_rowid();
";

        public const string GetSessionIdForTitle = @"
        SELECT Id
        FROM ChatSession
        WHERE UserName = @UserName
          AND Title = @Title
        ORDER BY Id DESC LIMIT 1 ";


        public const string GetMessageExtrasById = @"
SELECT
    m.Id,
    m.SqlText,
    m.DataFileName,
    m.CanShowSql,
    m.CanShowData
FROM ChatMessage m
INNER JOIN ChatSession s
    ON m.SessionId = s.Id
WHERE
    m.Id = $MessageId
    AND s.UserName = $UserName
LIMIT 1;
";

        public const string CreateTables = @"
CREATE TABLE IF NOT EXISTS ChatSession
(
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    UserName    TEXT NOT NULL,
    Title       TEXT NOT NULL,
    CreatedOn   TEXT NOT NULL,
    UpdatedOn   TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ChatMessage
(
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    SessionId   INTEGER NOT NULL,
    Role        TEXT NOT NULL,
    MessageText TEXT NOT NULL,
    CreatedOn   TEXT NOT NULL,
    FOREIGN KEY(SessionId) REFERENCES ChatSession(Id)
);

CREATE INDEX IF NOT EXISTS IX_ChatSession_UserName
ON ChatSession(UserName);

CREATE INDEX IF NOT EXISTS IX_ChatMessage_SessionId
ON ChatMessage(SessionId);

CREATE TABLE IF NOT EXISTS ChatBookmark
(
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    UserName      TEXT NOT NULL,
    MessageId     INTEGER NOT NULL,
    BookmarkText  TEXT NOT NULL,
    CreatedOn     TEXT NOT NULL,
    IsActive      INTEGER NOT NULL DEFAULT 1
);

CREATE INDEX IF NOT EXISTS IX_ChatBookmark_User
ON ChatBookmark(UserName);

CREATE INDEX IF NOT EXISTS IX_ChatBookmark_Message
ON ChatBookmark(MessageId);

CREATE TABLE IF NOT EXISTS UserAudit
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,

    UserId TEXT NOT NULL,

    ActionType TEXT NOT NULL,

    ActionValue TEXT NULL,

    CreatedOn DATETIME NOT NULL
);

";
        public const string AddBookmark = @"
INSERT INTO ChatBookmark
(UserName, MessageId, BookmarkText, CreatedOn, IsActive)
SELECT $UserName, $MessageId, $BookmarkText, $CreatedOn, 1
WHERE NOT EXISTS (
    SELECT 1 FROM ChatBookmark
    WHERE UserName = $UserName
      AND MessageId = $MessageId
      AND IsActive = 1
);
";

        public const string RemoveBookmark = @"
UPDATE ChatBookmark
SET IsActive = 0
WHERE UserName = $UserName
  AND MessageId = $MessageId;
";

        public const string GetBookmarks = @"
SELECT m.Id,
       m.SessionId,
       m.Role,
       m.MessageText,
       m.CreatedOn,
       m.CanShowSql,
       m.CanShowData,
       m.CanShowChart,
       b.BookmarkText
FROM ChatBookmark b
JOIN ChatMessage m ON m.Id = b.MessageId
WHERE b.UserName = $UserName
  AND b.IsActive = 1
ORDER BY b.CreatedOn DESC;
";
        public const string SearchMessages = @"
SELECT s.Id          AS SessionId,
       s.Title       AS SessionTitle,
       m.Id          AS MessageId,
       m.MessageText,
       m.Role,
       m.CreatedOn
FROM ChatSession s
LEFT JOIN ChatMessage m
    ON m.SessionId = s.Id
WHERE s.UserName = $UserName and m.Role= 'User'
  AND (
        s.Title LIKE '%' || $Text || '%'
     OR m.MessageText LIKE '%' || $Text || '%'
  )
ORDER BY s.UpdatedOn DESC, m.Id;
";
        public const string CreateUserTokensSql = @"
            CREATE TABLE IF NOT EXISTS UserTokens (
                Token TEXT PRIMARY KEY,
                Username TEXT NOT NULL,
                Role TEXT,
                InternalUserId TEXT,
                SessionContextKey TEXT,
                DefaultDatabase TEXT
            );

        ";

        public const string InsertUserToken = @"
            INSERT INTO UserTokens
            (Token, Username, Role, InternalUserId, SessionContextKey, DefaultDatabase)
            VALUES
            ($Token, $Username, $Role, $InternalUserId, $SessionContextKey, $DefaultDatabase);            
            ";

        public const string GetUserToken = @"
            SELECT * FROM UserTokens
            WHERE Token = $Token;
            ";
        
        public const string GetUserTokenByUsername = @"
            SELECT * FROM UserTokens
            WHERE Username = $Username
            LIMIT 1;
            ";
    }
}
