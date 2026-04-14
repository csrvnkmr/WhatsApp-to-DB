namespace WhatsAppToDB
{
    public class WhatsAppSettings
    {
        public string Token { get; set; } = string.Empty;
        public string PhoneId { get; set; } = string.Empty;

        // used to verify, this is the token already set in the Meta webhook
        public string VerifyToken { get; set; } = string.Empty; 
    }

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ChromaUrl { get; set; } = string.Empty;
        public string SchemaDefinitionFile { get; set; } = string.Empty;
    }

    public class OpenAiSettings
    {
        public string Model { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string SystemPromptFile { get; set; } = string.Empty;

        // Helper property to get the full prompt
        public string FullSystemPrompt => File.ReadAllText(SystemPromptFile);// string.Join(" ", SystemPromptParts);
    }

    public class PluginSettings
    {
        public string AssemblyPath { get; set; } = string.Empty;
        public string PluginClassName { get; set; } = string.Empty;
    }

    public class PluginMetadata { public Type PluginType { get; set; } }
}
