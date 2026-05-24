    public class VectorRecord
    {
        public string Id { get; set; }        // SAP ItemCode or CardCode
        public string? ChromaId { get; set; }  // UUIDv4 used by Chroma
        public string Content { get; set; }   // Text for embedding
        public Dictionary<string, object> Metadata { get; set; }
    }
