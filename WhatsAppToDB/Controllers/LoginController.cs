using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Audit;
using WhatsAppToDB.Data;
using WhatsAppToDB.LlmProviders;
using WhatsAppToDB.Services;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.Controllers
{
    [ApiController]
    public class LoginController : Controller
    {
        private readonly ILogger _waLogger;
        private readonly DefaultSettings _defaultSettings;
        private readonly IUserAuditService _auditService;
        private readonly JsonConfigService _jsonConfigService;

        public LoginController(
            ILogger waLogger, 
            IUserAuditService auditService, JsonConfigService jsonConfigService )
        {
            _waLogger = waLogger;
            _auditService = auditService;
            _jsonConfigService = jsonConfigService;
            _defaultSettings = _jsonConfigService.GetDefaultSettings();
        }


        [HttpPost("/logout")]
        public async Task<IActionResult> Logout()
        {
            var userName = HttpContext.Items["UserName"]?.ToString() ?? "";
            HttpContext.Session.Clear();
            await _auditService.LogAsync(userName, AuditActions.Logout, "Successful");
            return Ok(new { Message = "Logout Successful" });
        }


        [HttpPost("/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = UserService.ValidateLogin(_jsonConfigService, request.Username, request.Password);
            if (!result.isSuccess)
            {
                return Unauthorized();
            }
            HttpContext.Session.Clear();

            var latestDb =
                await _auditService.GetLatestValueAsync(
                    request.Username,
                    AuditActions.DatabaseChanged);
            var latestProvider =
                await _auditService.GetLatestValueAsync(
                    request.Username,
                    AuditActions.ProviderChanged);
            var latestModel =
                await _auditService.GetLatestValueAsync(
                    request.Username,
                    AuditActions.ModelChanged);
            latestDb ??= (string.IsNullOrWhiteSpace(result.session.DefaultDatabase) ? _defaultSettings.DefaultDatabase : result.session.DefaultDatabase);
            latestProvider ??= _defaultSettings.DefaultLlmProvider;
            latestModel ??= _defaultSettings.DefaultLlmModel;

            if (latestModel.Split(',').Length > 1)
            {
                latestProvider = latestModel.Split(',')[0].Trim();
                latestModel = latestModel.Split(',')[1].Trim();
            }

            HttpContext.Session.SetString(Constants.SessionKeys.ActiveDb, latestDb);
            HttpContext.Session.SetString(Constants.SessionKeys.ActiveLlmProvider, latestProvider);
            HttpContext.Session.SetString(Constants.SessionKeys.ActiveLlmModel, latestModel);

            await _auditService.LogAsync(request.Username, AuditActions.Login, "Successful");

            return Ok(new { Token = result.session.Token, Message = "Login Successful" });
        }


 
    }
}
