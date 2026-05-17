using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;

namespace WhatsAppToDB.Controllers
{
    [ApiController]
    [Route("debug/kernel")]
    public class KernelDebugController : ControllerBase
    {
        private readonly Kernel _kernel;

        public KernelDebugController(Kernel kernel)
        {
            _kernel = kernel;
        }

        private void DumpKernelFunctions(string source)
        {
            Console.WriteLine($"===== Kernel Functions Dump: {source} =====");

            foreach (var plugin in _kernel.Plugins)
            {
                Console.WriteLine($"Plugin: {plugin.Name}");

                foreach (var function in plugin)
                {
                    Console.WriteLine($"  Function: {function.Name}");
                }
            }

            Console.WriteLine("===========================================");
        }

        [HttpGet("functions")]
        public IActionResult GetFunctions()
        {
            var result =
                _kernel.Plugins
                    .Select(p => new
                    {
                        Plugin = p.Name,
                        Functions = p.Select(f => new
                        {
                            f.Name,
                            f.Description
                        })
                    });

            return Ok(result);
        }

        [HttpPost("invoke-vector")]
        public async Task<IActionResult> InvokeVector(
            [FromBody] VectorFunctionTestRequest request)
        {
            DumpKernelFunctions("Before InvokeVector");
            
            var result =
                await _kernel.InvokeAsync(
                    pluginName: "VectorSearch",
                    functionName: request.FunctionName,
                    arguments: new KernelArguments
                    {
                        ["query"] = request.Query
                    });

            return Ok(new
            {
                Function = request.FunctionName,
                Query = request.Query,
                Result = result.GetValue<string>()
            });
        }
    }

    public class VectorFunctionTestRequest
    {
        public string FunctionName { get; set; } = "";
        public string Query { get; set; } = "";
    }
}