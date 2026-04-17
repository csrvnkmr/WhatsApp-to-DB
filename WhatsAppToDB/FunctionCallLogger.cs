using Microsoft.SemanticKernel;
using System.Text.Json;

namespace WhatsAppToDB
{
    // Program.cs or ChatService.cs - add a function filter to trace all calls
    public class FunctionCallLogger : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(
            FunctionInvocationContext context,
            Func<FunctionInvocationContext, Task> next)
        {
            Console.WriteLine($"\n>>> CALLING: {context.Function.PluginName}.{context.Function.Name}");
            Console.WriteLine($">>> INPUT:   {JsonSerializer.Serialize(context.Arguments)}");

            await next(context);

            Console.WriteLine($"<<< OUTPUT:  {context.Result?.GetValue<string>()?.Substring(0, Math.Min(500, context.Result?.GetValue<string>()?.Length ?? 0))}");
        }
    }

}
