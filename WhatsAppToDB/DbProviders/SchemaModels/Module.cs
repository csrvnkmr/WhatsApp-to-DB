namespace WhatsAppToDB.DbProviders.SchemaModels
{

    public class Module
    {
        public string Name { get; set; }
        public string Details { get; set; }
        public string Prompt { get; set; }
        
        public string[] Tables { get; set; }
    }

}
