// ==========================================================
// Controllers/WhatsAppController.cs
// ==========================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;
using WhatsAppToDB.Models;
using WhatsAppToDB.Services;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.Controllers
{
    [ApiController]
    public class WhatsAppController : ControllerBase
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _waLogger;
        private readonly IOptions<WhatsAppSettings> _waOptions;
        private readonly ChatDbRepository _repo;
        private readonly PromptExecutionSettings _promptSettings;
        private readonly IQueryService _queryService;
        private readonly IIdentityContextEnricher _identityContextEnricher;
        
        public WhatsAppController(
            IServiceScopeFactory scopeFactory,
            ILogger waLogger,
            IOptions<WhatsAppSettings> waOptions,
            ChatDbRepository repo,
            IQueryService queryService,
            IIdentityContextEnricher identityContextEnricher)
        {
            _scopeFactory = scopeFactory;
            _waLogger = waLogger;
            _waOptions = waOptions;
            _repo = repo;
            _queryService = queryService;
            _identityContextEnricher = identityContextEnricher;
            _promptSettings =
                new OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior =
                        FunctionChoiceBehavior.Auto()
                };
        }

        // ==================================================
        // GET /webhook
        // ==================================================
        [HttpGet("/webhook")]
        public IActionResult VerifyWebhook()
        {
            var verifyToken =
                _waOptions.Value.VerifyToken;

            string mode =
                Request.Query["hub.mode"];

            string token =
                Request.Query["hub.verify_token"];

            string challenge =
                Request.Query["hub.challenge"];

            if (mode == "subscribe"
                && token == verifyToken)
            {
                return Ok(challenge);
            }

            return BadRequest();
        }

        // ==================================================
        // POST /webhook
        // ==================================================
        [HttpPost("/webhook")]
        public async Task<IActionResult> ReceiveWebhook()
        {
            using var reader = new StreamReader(Request.Body);

            var body = await reader.ReadToEndAsync();

            var waService = new WhatsAppService();

            var result = await waService.GetWhatsAppMessage(
                    body, _waLogger);

            if (!result.isSuccess)
                return Ok();

            var senderPhone = result.to;

            var messageText = result.message;

            using var scope = _scopeFactory.CreateScope();

            var sp = scope.ServiceProvider;

            var identityService = sp.GetRequiredService<IIdentityService>();

            var identity = await identityService
                    .GetIdentityAsync(senderPhone);
                _identityContextEnricher.EnrichFromHttpContext(
                identity,
                HttpContext);

            _ = Task.Run(async () =>
            {
                using var bgScope =
                _scopeFactory.CreateScope();

                var queryService =
                    bgScope.ServiceProvider.GetRequiredService<IQueryService>();

                var repo =
                bgScope.ServiceProvider.GetRequiredService<ChatDbRepository>();

                await waService.SendWhatsAppResponse(
                    senderPhone,
                    "_Analyzing your request and querying database... Please wait a moment._ 🔍",
                    _waOptions.Value,
                    _waLogger);


                var sessionId =
                    await repo.GetWhatsAppSessionIdAsync(identity.UserName);


                var response =
                    await queryService.ExecuteQuery(
                        _scopeFactory,
                        identity,
                        messageText,
                        _promptSettings,
                        _waLogger,
                        _repo,
                        sessionId);

                await waService.SendWhatsAppResponse(
                    senderPhone,
                    response.MessageText,
                    _waOptions.Value,
                    _waLogger);
            });

            return Ok();
        }
    }
}