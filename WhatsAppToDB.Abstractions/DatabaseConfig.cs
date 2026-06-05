namespace WhatsAppToDB.Database
{
    public class DatabaseConfig
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ConnectionString { get; set; }
        public string DbProvider { get; set; }

        public string DefaultRole { get; set; }
        public string PromptFile { get; set; }
        public string SchemaFile { get; set; }

        public bool AddChart { get; set; } = false;
    }
}
