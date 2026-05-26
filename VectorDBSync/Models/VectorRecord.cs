    public class VectorRecord
    {
        public string Id { get; set; }        // SAP ItemCode or CardCode
        public string? VectorId { get; set; }  // External vector DB identifier
        public string Content { get; set; }   // Text for embedding
        public Dictionary<string, object> Metadata { get; set; }
    }
