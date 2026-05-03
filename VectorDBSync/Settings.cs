using System.Text.Json;
using System.Text.Json.Serialization;

namespace VectorDBSync;
public class Settings
{
    [JsonPropertyName("Logging")]
    public Logging Logging { get; set; } = new();

    [JsonPropertyName("DatabaseSettings")]
    public DatabaseSettings DatabaseSettings { get; set; } = new();

    [JsonPropertyName("EmbeddingServiceSettings")]
    public EmbeddingServiceSettings EmbeddingServiceSettings { get; set; } = new();

    [JsonPropertyName("JinaAiSettings")]
    public JinaAiSettings JinaAiSettings { get; set; } = new();

    [JsonPropertyName("OpenAiSettings")]
    public OpenAiSettings OpenAiSettings { get; set; } = new();

    [JsonPropertyName("SqliteSettings")]
    public SqliteSettings SqliteSettings { get; set; } = new();

    [JsonPropertyName("VectorDBSettings")]
    public VectorDBSettings VectorDBSettings  { get; set; } = new();
    public static Settings LoadFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<Settings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new JsonException("Failed to deserialize settings from file.");
    }
}

public class Logging
{
    [JsonPropertyName("LogLevel")]
    public LogLevel LogLevel { get; set; } = new();
}

public class LogLevel
{
    [JsonPropertyName("Default")]
    public string Default { get; set; } = string.Empty;

    [JsonPropertyName("Microsoft.AspNetCore")]
    public string MicrosoftAspNetCore { get; set; } = string.Empty;
}

public class DatabaseSettings
{
    [JsonPropertyName("ConnectionString")]
    public string ConnectionString { get; set; } = string.Empty;

    [JsonPropertyName("ChromaUrl")]
    public string ChromaUrl { get; set; } = string.Empty;
}

public class EmbeddingServiceSettings
{
    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;
}

public class OpenAiSettings
{
    [JsonPropertyName("Model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("ApiKey")]
    public string ApiKey { get; set; } = string.Empty;
}

public class VectorDBSettings
{
    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;

}

    public class SqliteSettings
{
    [JsonPropertyName("Folder")]
    public string Folder { get; set; } = string.Empty;

    [JsonPropertyName("Model")]
    public string Model { get; set; } = string.Empty;
}

public class JinaAiSettings
{
    [JsonPropertyName("LocalModel")]
    public string LocalModel { get; set; } = string.Empty;
    
    [JsonPropertyName("ApiModel")]
    public string ApiModel { get; set; } = string.Empty;

    [JsonPropertyName("ApiKey")]
    public string ApiKey { get; set; } = string.Empty;
}