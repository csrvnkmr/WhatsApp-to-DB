namespace WhatsAppToDB.Database
{
    public class DatabaseConfig
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ConnectionString { get; set; }
        public string Provider { get; set; }
        public string PromptFile { get; set; }
        public string SchemaFile { get; set; }
    }
}
