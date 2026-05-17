namespace WhatsAppToDB.Settings
{
    public class DefaultFolders
    {
        public string DatabaseProviderFolder { get; set; }

        public string LlmProviderFolder { get; set; }

        public string ChatHistoryFolder { get; set; }

        public string VectorDbFolder => Path.Combine(ChatHistoryFolder, "vector_db");
    }
}
