using System.Text.Json.Serialization;

public class EvalComparisonResult
{
    [JsonPropertyName("match")]
    public bool Match { get; set; }

    [JsonPropertyName("answers_question")]
    public bool AnswersQuestion { get; set; }

    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = "";

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "";

    [JsonPropertyName("differences")]
    public string Differences { get; set; } = "";
}