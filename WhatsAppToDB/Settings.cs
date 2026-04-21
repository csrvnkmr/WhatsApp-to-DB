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

    public class CommonAiSettings
    {
        public string SystemPromptFile { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        // Helper property to get the full prompt
        public string FullSystemPrompt => File.ReadAllText(SystemPromptFile);// string.Join(" ", SystemPromptParts);

    }
    public class LocalAiSettings
    {
        public string Model { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string HttpEndPoint { get; set; } = string.Empty;
    }

    public class OpenAiSettings
    {
        public string Model { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;

    }

    public class PluginSettings
    {
        public string AssemblyPath { get; set; } = string.Empty;
        public string PluginClassName { get; set; } = string.Empty;
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class ConnectionStrings
    {
        public string AdminConnection { get; set; }
        public string SalesPersonConnection { get; set; }
        public string EmployeeConnection { get; set; }
        public string FinanceConnection { get; set; }
        public string GuestConnection { get; set; }
    }


    public class RoleSettings
    {
        public string DefaultRole { get; set; }
        public string MappingSource { get; set; }
        public string RoleMappingJsonFile { get; set; }
        public string RoleMappingSqlQuery { get; set; }
        
    }



    public class PluginMetadata
    {
        // Now stores all loaded plugin types
        public List<Type> PluginTypes { get; set; } = new();
    }
}
