namespace WhatsAppToDB.VectorStore
{
    public class VectorKernelSearchResult
    {
        public bool Success { get; set; }

        public string Database { get; set; } = string.Empty;

        public string CollectionName { get; set; } = string.Empty;

        public string TableName { get; set; } = string.Empty;

        public string KeyField { get; set; } = string.Empty;

        public string KeyValue { get; set; } = string.Empty;

        public string ContentField { get; set; } = string.Empty;

        public string ContentValue { get; set; } = string.Empty;

        public double Distance { get; set; }

        public Dictionary<string, object?> Metadata { get; set; } = new();

        public string Message { get; set; } = string.Empty;
    }
}