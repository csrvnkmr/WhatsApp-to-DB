namespace WhatsAppToDB.Models
{

    public class Role
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ConnectionString { get; set; }
        public string[] Users { get; set; }
        public string[] Modules { get; set; }
    }

}
