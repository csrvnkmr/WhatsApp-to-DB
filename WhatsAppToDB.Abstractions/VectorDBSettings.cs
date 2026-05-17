using System.Text.Json.Serialization;

namespace WhatsAppToDB.Abstractions
{
    public class VectorDBSettings
    {
        public string CacheFolder { get; set; } = string.Empty;
        public DatabaseSettings DatabaseSettings { get; set; } = new();
        public EmbeddingServiceSettings EmbeddingServiceSettings { get; set; } = new();
        public JinaAiSettings JinaAiSettings { get; set; } = new();
        public OllamaSettings OllamaSettings { get; set; } = new();
        public OpenAiSettings OpenAiSettings { get; set; } = new();
        [JsonPropertyName("VectorProviderSettings")]
        public VectorDBProviderSettings VectorDBProvider { get; set; } = new();
        public SqliteSettings SqliteSettings { get; set; } = new();
        public ChromaSettings ChromaSettings { get; set; } = new();
        public Logging Logging { get; set; } = new();
    }

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ChromaUrl { get; set; } = string.Empty;
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

    public class EmbeddingServiceSettings
    {
        public string Type { get; set; } = string.Empty;
    }

    public class JinaAiSettings
    {
        public string LocalModel { get; set; } = string.Empty;
        public string ApiModel { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        [JsonPropertyName("URL")]
        public string URL { get; set; } = string.Empty;
    }

    public class OllamaSettings
    {
        public string Model { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }

    public class OpenAiSettings
    {
        public string Model { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }

    public class VectorDBProviderSettings
    {
        public string Type { get; set; } = string.Empty;
    }

    public class ChromaSettings
    {
        public string ChromaUrl { get; set; } = string.Empty;
    }

    public class SqliteSettings
    {
        [JsonPropertyName("VectorDBFolder")]
        public string VectorDBFolder { get; set; } = string.Empty;

        [JsonPropertyName("Folder")]
        public string Folder
        {
            get => VectorDBFolder;
            set => VectorDBFolder = value;
        }
    }
}

