namespace WhatsAppToDB.Audit
{
    public class AuditSqls
    {
        public const string InsertAudit = @"
INSERT INTO UserAudit
(
    UserId,
    ActionType,
    ActionValue,
    CreatedOn
)
VALUES
(
    @UserId,
    @ActionType,
    @ActionValue,
    CURRENT_TIMESTAMP
)";
        public const string GetLatestValue = @"
SELECT ActionValue
FROM UserAudit
WHERE UserId = @UserId
AND ActionType = @ActionType
ORDER BY CreatedOn DESC
LIMIT 1";
    }
}
