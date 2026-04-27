namespace WhatsAppToDB.Data
{
    public class SqliteSqls
    {
        public const string GetChatMessagesBySessionId = @"
SELECT Id,
       SessionId,
       Role,
       MessageText,
       CreatedOn, CanShowSql, CanShowData, CanShowChart
FROM ChatMessage
WHERE SessionId = $SessionId
ORDER BY Id;
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
        public const string InsertChatMessage = @"
INSERT INTO ChatMessage
(SessionId, Role, MessageText, CreatedOn, SqlText, DataFileName, ChartFileName, CanShowSql, CanShowData, CanShowChart)
VALUES
($SessionId, $Role, $MessageText, $CreatedOn, $SqlText, $DataFileName, null, $CanShowSql, $CanShowData, $CanShowChart);
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
";
    }
}
