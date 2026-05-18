using System.Text.Json;
using System.Text.RegularExpressions;

public class ParsedAiResponse
{
    public string AnalysisText { get; set; } = "";
    public ChartConfig? ChartConfig { get; set; }
    public List<Dictionary<string, JsonElement>>? ChartData { get; set; }


public static ParsedAiResponse ParseAiResponse(string raw)
{
    var result = new ParsedAiResponse();

    var match = Regex.Match(raw, @"<chart_data>(.*?)</chart_data>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase);

    if (!match.Success)
    {
        // No chart tag at all — pure analysis response
        result.AnalysisText = raw.Trim();
        return result;
    }

    // Text before the tag is the analysis
    result.AnalysisText = raw[..match.Index].Trim();

    try
    {
        var json = match.Groups[1].Value.Trim();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        result.ChartConfig = new ChartConfig
        {
            ChartType = root.GetProperty("chartType").GetString() ?? "table",
            Title     = root.TryGetProperty("title", out var t) ? t.GetString() : null,
            XKey      = root.TryGetProperty("xKey",  out var x) ? x.GetString() : null,
            YKey      = root.TryGetProperty("yKey",  out var y) ? y.GetString() : null,
            SeriesKey = root.TryGetProperty("seriesKey", out var sk) ? sk.GetString() : null,
        };

        if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
        {
            result.ChartData = dataEl.EnumerateArray()
                .Select(row => row.EnumerateObject()
                    .ToDictionary(p => p.Name, p => p.Value))
                .ToList();
        }
    }
    catch
    {
        // Malformed JSON — still return the analysis text, just no chart
        result.ChartConfig = new ChartConfig { ChartType = "table" };
    }

    return result;
}

}