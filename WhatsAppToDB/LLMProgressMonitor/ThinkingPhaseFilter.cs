// Filters/ThinkingPhaseFilter.cs
using Microsoft.SemanticKernel;

public class ThinkingPhaseFilter : IPromptRenderFilter
{
    private readonly ProgressChannel _progress;

    public ThinkingPhaseFilter(ProgressChannel progress)
        => _progress = progress;

    public async Task OnPromptRenderAsync(
        PromptRenderContext context,
        Func<PromptRenderContext, Task> next)
    {
        await _progress.Writer.WriteAsync(new ProgressEvent
        {
            Phase   = "thinking",
            Message = "🧠 Thinking about your question..."
        });

        await next(context);

        await _progress.Writer.WriteAsync(new ProgressEvent
        {
            Phase   = "planning",
            Message = "📐 Planning the approach..."
        });
    }
}