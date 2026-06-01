// Filters/ProgressFunctionFilter.cs
//using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

public class ProgressFunctionFilter : IFunctionInvocationFilter
{
    private readonly ProgressChannel _progress;

    public ProgressFunctionFilter(ProgressChannel progress)
    {
        _progress = progress;
    }

    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        var name = context.Function.Name;
        var plugin = context.Function?.UnderlyingMethod?.Module?.Name;

        // --- Map function names to meaningful user-facing phases ---
        var (phase, message) = name switch
        {
            var n when n.Contains("Schema") || n.Contains("Table")
                => ("planning",   $"📋 Analysing database schema..."),
            var n when n.Contains("Sql") || n.Contains("Query") || n.Contains("Execute")
                => ("executing",  $"⚙️ Executing SQL query..."),
            var n when n.Contains("Validate") || n.Contains("Check")
                => ("thinking",   $"🔍 Validating query..."),
            _   => ("thinking",   $"🤔 Calling {plugin}.{name}...")
        };

        await _progress.Writer.WriteAsync(new ProgressEvent
        {
            Phase   = phase,
            Message = message,
            Detail  = $"{plugin}.{name}({System.Text.Json.JsonSerializer.Serialize(context.Arguments)})"
        });

        await next(context);  // ← actual function runs here

        // Post-execution feedback
        var resultSnippet = context.Result?.GetValue<string>()
            ?.Substring(0, Math.Min(200, context.Result?.GetValue<string>()?.Length ?? 0));

        await _progress.Writer.WriteAsync(new ProgressEvent
        {
            Phase   = "done_step",
            Message = $"✅ {name} completed",
            Detail  = resultSnippet
        });
    }
}