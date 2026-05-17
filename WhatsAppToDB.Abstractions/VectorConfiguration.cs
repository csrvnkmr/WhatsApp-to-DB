using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WhatsAppToDB.Abstractions
{
    public class VectorConfiguration
    {
        public string CollectionName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string KeyField { get; set; } = string.Empty;
        public string ContentField { get; set; } = string.Empty;
        public List<string> MetadataFields { get; set; } = new();
        public string FunctionName { get ; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Threshold { get; set; } = 0.6;

       public string GetFunctionName()
        {
            if (!string.IsNullOrWhiteSpace(FunctionName))
                return ToSafeIdentifier(FunctionName);

            var table =
                ToSafeIdentifier(TableName);

            var key =
                ToSafeIdentifier(KeyField);

            if (!string.IsNullOrWhiteSpace(table) &&
                !string.IsNullOrWhiteSpace(key))
            {
                return $"Get{table}{key}";
            }

            var collection =
                ToSafeIdentifier(CollectionName);

            if (!string.IsNullOrWhiteSpace(collection))
            {
                return $"Search{collection}";
            }

            return "VectorSearch";
        }

        public string GetDescription()
        {
            if (!string.IsNullOrWhiteSpace(Description))
                return Description;

            var tableName =
                string.IsNullOrWhiteSpace(TableName)
                    ? "configured table"
                    : TableName;

            var collectionName =
                string.IsNullOrWhiteSpace(CollectionName)
                    ? "configured vector collection"
                    : CollectionName;

            var keyField =
                string.IsNullOrWhiteSpace(KeyField)
                    ? "key value"
                    : KeyField;

            var contentField =
                string.IsNullOrWhiteSpace(ContentField)
                    ? "name or description"
                    : ContentField;

            return
                $"Searches vector collection '{collectionName}' " +
                $"to find the official {keyField} from a fuzzy {contentField}. " +
                $"Use this before generating SQL when the user provides an approximate name for " +
                $"{tableName}.{contentField}. " +
                $"Returns JSON containing {keyField}, {contentField}, distance, and metadata.";
        }

        private static string ToSafeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var cleaned = Regex.Replace(
                    value, @"[^A-Za-z0-9_]", string.Empty);

            if (string.IsNullOrWhiteSpace(cleaned))
                return string.Empty;

            if (char.IsDigit(cleaned[0]))
                cleaned = "_" + cleaned;

            return cleaned;
        }

    }

    public class VectorSyncConfig : VectorConfiguration
    {
        public string SyncSql { get; set; } = string.Empty;
        public string UpdateTrackerSql { get; set; } = string.Empty;
        public bool DeleteAndCreate { get; set; } = false;
    }

    public class VectorSyncRoot
    {
        public List<VectorSyncConfig> SyncCollections { get; set; } = new();
    }
}