namespace WhatsAppToDB.Abstractions
{
    public class EmbeddingServiceSettings
    {
        public string Type { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;

        public int BatchSize { get; set; } = 100;
    }
}
