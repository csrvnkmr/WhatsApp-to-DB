public class ChartConfig
{
    public string ChartType { get; set; } = "table";
    public string? XKey { get; set; }
    public string? YKey { get; set; }
    public string? Title { get; set; }
    public string? SeriesKey { get; set; } // new — for multi-series charts
}

public class QueryResult
{
    public List<ColumnInfo> Columns { get; set; } = [];
    public List<Dictionary<string, object>> Rows { get; set; } = [];
}

public class ColumnInfo
{
    public string Name { get; set; } = "";
    public string DataType { get; set; } = ""; // "string", "number", "date"
}