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
    }
}
