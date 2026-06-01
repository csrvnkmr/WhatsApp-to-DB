// Models/ProgressEvent.cs
public class ProgressEvent
{
    public string Phase { get; set; }      // "thinking" | "planning" | "executing" | "done" | "error"
    public string Message { get; set; }    // Human-readable description
    public string? Detail { get; set; }    // Optional extra info (function name, SQL snippet, etc.)
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}