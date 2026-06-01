// ==========================================================
// Controllers/WhatsAppController.cs
// ==========================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Threading;
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
        private readonly ChatDbRepository _repo;
        private readonly PromptExecutionSettings _promptSettings;
        private readonly IQueryService _queryService;
        private readonly IIdentityContextEnricher _identityContextEnricher;
        private readonly JsonConfigService _jsonConfigService;
        private readonly LlmCancellationService _cancellationService;
        
        public WhatsAppController(
            IServiceScopeFactory scopeFactory,
            ILogger waLogger,
            ChatDbRepository repo,
            IQueryService queryService,
            IIdentityContextEnricher identityContextEnricher,
            JsonConfigService jsonConfigService,
            LlmCancellationService cancellationService)
        {
            _scopeFactory = scopeFactory;
            _waLogger = waLogger;
            _repo = repo;
            _queryService = queryService;
            _identityContextEnricher = identityContextEnricher;
            _jsonConfigService = jsonConfigService;
            _cancellationService = cancellationService;
            _promptSettings =
                new OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior =
                        FunctionChoiceBehavior.Auto()
                };
        }

        private bool isTokenValid(string verify_token)
        {
            var lst = _jsonConfigService.GetWhatsAppProfiles();
            if (lst == null || lst.Count==0)
            {
                return false;
            }
            if (lst.Any(x=>x.VerifyToken==verify_token))
            {
                return true;
            }
            return false;
        }

        // ==================================================
        // GET /webhook
        // ==================================================
        [HttpGet("/webhook")]
        public IActionResult VerifyWebhook()
        {

            string mode =
                Request.Query["hub.mode"];

            string token =
                Request.Query["hub.verify_token"];

            string challenge =
                Request.Query["hub.challenge"];

            if (mode == "subscribe"
                && isTokenValid(token))
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

            var cts = new CancellationTokenSource();
            _cancellationService.Register(identity.UserName, cts);

            _ = Task.Run(async () =>
            {
                using var bgScope =
                _scopeFactory.CreateScope();

                var queryService =
                    bgScope.ServiceProvider.GetRequiredService<IQueryService>();

                var repo =
                bgScope.ServiceProvider.GetRequiredService<ChatDbRepository>();

                var waProfile =  _jsonConfigService.GetWhatsAppProfile(identity.WhatsAppProfileId);

                await waService.SendWhatsAppResponse(
                    senderPhone,
                    "_Analyzing your request and querying database... Please wait a moment._ 🔍",
                    waProfile,
                    _waLogger);


                var sessionId =
                    await repo.GetWhatsAppSessionIdAsync(identity.UserName);

                try
                {
                    var response =
                        await queryService.ExecuteQuery(
                            _scopeFactory,
                            identity,
                            messageText,
                            _promptSettings,
                            _waLogger,
                            _repo,
                            sessionId,
                            cts.Token);

                    await waService.SendWhatsAppResponse(
                        senderPhone,
                        response.MessageText,
                        waProfile,
                        _waLogger);
                }
                finally
                {
                    _cancellationService.Remove(identity.UserName);
                }
            }, cts.Token);

            return Ok();
        }
    }
}