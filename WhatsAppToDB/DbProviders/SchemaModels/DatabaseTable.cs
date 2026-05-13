namespace WhatsAppToDB.DbProviders.SchemaModels
{
    public class DatabaseTable
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Columns { get; set; }
    }

}
