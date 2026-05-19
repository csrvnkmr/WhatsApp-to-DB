using WhatsAppToDB.Abstractions;

public class ExecutionContextService
{
    public string UserName { get; set; } = "";
    public string Database { get; set; } = "";
    public string LlmProvider { get; set; } = "";
    public string LlmModel { get; set; } = "";
    public bool IsWhatsAppRequest { get; set; }

    public void SetFromIdentity(IdentityContext identity)
    {
        UserName = identity.UserName;
        Database = identity.Database;
        LlmProvider = identity.LlmProvider;
        LlmModel = identity.LlmModel;
        IsWhatsAppRequest = identity.IsWhatsAppRequest;
    }
}