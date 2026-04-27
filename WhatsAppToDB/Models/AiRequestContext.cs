using WhatsAppToDB.Abstractions;

namespace WhatsAppToDB.Models
{

    public class AiRequestContext
    {
        // Current user
        public IdentityContext? Identity { get; set; }

        public string UserQuestion { get; set; } = "";
        public string WhatsAppNumber { get; set; } = "";

        // SQL Execution Metadata
        public string LastExecutedSql { get; set; } = "";

        public string DataFileName { get; set; } = "";

        public string ChartFileName { get; set; } = "";

        public bool ShowSql { get; set; }
        public bool ShowData { get; set; }
        public bool ShowChart { get; set; }

        public int ResultRowCount { get; set; }

        public long SessionId { get; set; }
        public long MessageId { get; set; }
    }

}
