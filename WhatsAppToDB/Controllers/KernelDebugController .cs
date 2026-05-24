using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;

namespace WhatsAppToDB.Controllers
{
    [ApiController]
    [Route("debug/kernel")]
    public class KernelDebugController : ControllerBase
    {
        private readonly Kernel _kernel;
        private readonly ILogger _logger;

        public KernelDebugController(Kernel kernel, ILogger logger)
        {
            _kernel = kernel;
            _logger = logger;
        }

        private void DumpKernelFunctions(string source)
        {
            _logger.LogInfo($"[KernelDebugController] ===== Kernel Functions Dump: {source} =====");

            foreach (var plugin in _kernel.Plugins)
            {
                _logger.LogInfo($"[KernelDebugController] Plugin: {plugin.Name}");

                foreach (var function in plugin)
                {
                    _logger.LogInfo($"[KernelDebugController]   Function: {function.Name}");
                }
            }

            _logger.LogInfo("[KernelDebugController] ===========================================");
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