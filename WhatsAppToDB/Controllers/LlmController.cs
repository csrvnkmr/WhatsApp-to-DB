using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Audit;
using WhatsAppToDB.Data;
using WhatsAppToDB.LlmProviders;
using WhatsAppToDB.Models;
using WhatsAppToDB.Services;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.Controllers
{
    [ApiController]
    public class LlmController : Controller
    {
        private readonly ILogger _waLogger;
        private readonly LlmRegistry _llmRegistry;
        private readonly DefaultSettings _defaultSettings;
        private readonly IUserAuditService _auditService;
        private readonly JsonConfigService _jsonConfigService;

        public LlmController(
            ILogger waLogger, LlmRegistry llmRegistry, 
            IUserAuditService auditService, JsonConfigService jsonConfigService)
        {
            _waLogger = waLogger;
            _auditService = auditService;
            _llmRegistry = llmRegistry;
            _jsonConfigService = jsonConfigService;
            _defaultSettings = _jsonConfigService.GetDefaultSettings();
        }

        [HttpGet("llms")]
        public IActionResult GetLlms()
        {
            var provider =
                HttpContext.Session.GetString(Constants.SessionKeys.ActiveLlmProvider)
                ?? _defaultSettings.DefaultLlmProvider;

            var model =
                HttpContext.Session.GetString(Constants.SessionKeys.ActiveLlmModel)
                ?? _defaultSettings.DefaultLlmModel;

            var lstProviders = _llmRegistry.GetAll();
            if (model.Split(',').Length > 1)
            {
                provider = model.Split(',')[0].Trim();
                model = model.Split(',')[1].Trim();
            }

            return Ok(new
            {
                selectedProvider = provider,
                selectedModel = model,
                providers = lstProviders
            });
        }

        [HttpPost("llms/select")]
        public async Task<IActionResult> SelectLlm([FromBody] SelectLlmRequest request)
        {
            HttpContext.Session.SetString(Constants.SessionKeys.ActiveLlmProvider , request.Provider);
            HttpContext.Session.SetString(Constants.SessionKeys.ActiveLlmModel, request.Model);
            var userName = HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? "";
            await _auditService.LogAsync(userName, AuditActions.ModelChanged, $"{request.Provider}, {request.Model}");
            _waLogger.LogInfo($"[LlmController] Llm SELECT: {request.Provider}, {request.Model}");

            return Ok();
        }

    }
}
