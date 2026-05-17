    public class SyncTableInfo {
        public string TableName { get; set; }
        public string IdField { get; set; }
        public string ContentField { get; set; }
        public List<string> MetadataFields { get; set; } // Comma separated list of metadata fields
    }