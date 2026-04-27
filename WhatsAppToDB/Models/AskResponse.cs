namespace WhatsAppToDB.Models
{
    public class AskResponse
    {
        public string LlmResponse { get; set; } = "";
        public bool ShowSql { get; set; } = false;
        public bool ShowData { get; set; } = false;
        public bool ShowChart { get; set; } = false;
        public long SessionId { get; set; } = 0;
    }
}
