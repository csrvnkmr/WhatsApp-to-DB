namespace WhatsAppToDB.Settings
{
    public class DefaultSettings
    {
        public string DefaultDatabase { get; set; }

        public string DefaultLlmProvider { get; set; }

        public string DefaultLlmModel { get; set; }

        public int MaxTurns { get; set; } = 5;
    }
}
