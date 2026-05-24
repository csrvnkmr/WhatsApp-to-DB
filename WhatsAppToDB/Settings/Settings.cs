namespace WhatsAppToDB.Settings
{
    public class WhatsAppSettings
    {
        public string Token { get; set; } = string.Empty;
        public string PhoneId { get; set; } = string.Empty;

        // used to verify, this is the token already set in the Meta webhook
        public string VerifyToken { get; set; } = string.Empty; 
    }

    public class PluginSettings
    {
        public string Name { get; set; } = string.Empty;
        public string AssemblyPath { get; set; } = string.Empty;
        public string PluginClassName { get; set; } = string.Empty;
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
