
    public class SearchResult
    {
        public string Id { get; set; }
        public string Document { get; set; }
        public float? Distance { get; set; } // Lower is better (more similar)
        public Dictionary<string, object> Metadata { get; set; }
    }