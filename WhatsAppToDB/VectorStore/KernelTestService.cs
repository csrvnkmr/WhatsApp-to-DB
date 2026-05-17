using Microsoft.SemanticKernel;

public class KernelTestService
{
    private readonly Kernel _kernel;

    public KernelTestService(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task TestVectorFunctionsAsync()
    {
        Console.WriteLine("===== Kernel Plugins =====");

        foreach (var plugin in _kernel.Plugins)
        {
            Console.WriteLine($"Plugin: {plugin.Name}");

            foreach (var function in plugin)
            {
                Console.WriteLine($"  Function: {function.Name}");
                Console.WriteLine($"  Description: {function.Description}");
            }
        }

        Console.WriteLine("===== Testing Vector Function =====");

        var result =
            await _kernel.InvokeAsync(
                pluginName: "VectorSearch",
                functionName: "GetOITMItemCode",
                arguments: new KernelArguments
                {
                    ["query"] = "stabilizer bushing"
                });

        Console.WriteLine(result.GetValue<string>());
    }
}