using System.Text.Json.Serialization;

namespace WhatsAppToDB.Abstractions
{
    public class VectorDBSettings
    {
        public string CacheFolder { get; set; } = string.Empty;

        public EmbeddingServiceSettings EmbeddingServiceSettings { get; set; } = new();

        [JsonPropertyName("VectorProviderSettings")]
        public VectorDBProviderSettings VectorDBProviderSettings { get; set; } = new();
        //public SqliteSettings SqliteSettings { get; set; } = new();
        public Logging Logging { get; set; } = new();
    }

    public class Logging
    {
        public LogLevel LogLevel { get; set; } = new();
    }

    public class LogLevel
    {
        public string Default { get; set; } = string.Empty;
        public string MicrosoftAspNetCore { get; set; } = string.Empty;
    }
}

