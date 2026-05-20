namespace WhatsAppToDB.LlmProviders
{
    public class LlmConfig
    {
        public string Provider { get; set; }
        
        public bool Enabled { get; set; }
        
        public string ApiKey { get; set; }
        public string HttpEndPoint { get; set; }
        public List<string> Models { get; set; } = new();
    }
}
