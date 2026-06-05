using System.Text.Json;

public static class HistoryCompressor
{
    // Tokens are roughly 4 chars each. We allow ~300 tokens per assistant message.
    private const int MaxAssistantChars = 1200;

    public static string CompressAssistantMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        // If it looks like a JSON envelope (chart/data response), extract analysis_text only
        if (content.TrimStart().StartsWith("{"))
        {
            try
            {
                var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("analysis_text", out var analysis))
                {
                    var text = analysis.GetString() ?? "";
                    return Truncate(text, MaxAssistantChars);
                }
            }
            catch { /* not valid JSON — fall through */ }
        }

        return Truncate(content, MaxAssistantChars);
    }

    private static string Truncate(string text, int maxChars)
    {
        if (text.Length <= maxChars)
            return text;

        // Cut at a sentence boundary if possible
        var cutPoint = text.LastIndexOfAny(new[] { '.', '\n' }, maxChars);
        if (cutPoint > maxChars / 2)
            return text[..cutPoint] + " [...]";

        return text[..maxChars] + " [...]";
    }
}