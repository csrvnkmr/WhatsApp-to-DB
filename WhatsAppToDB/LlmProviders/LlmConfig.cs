namespace WhatsAppToDB.LlmProviders
{
    public class LlmConfig
    {
        public string Provider { get; set; }
        
        public bool Enabled { get; set; }

        public List<string> Models { get; set; } = new();
    }
}
