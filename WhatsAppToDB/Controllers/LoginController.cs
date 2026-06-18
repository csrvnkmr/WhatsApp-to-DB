using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NPOI.SS.Formula.Functions;
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
            var userName = HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? "";
            HttpContext.Session.Clear();
            await _auditService.LogAsync(userName, AuditActions.Logout, "Successful");
            return Ok(new { Message = "Logout Successful" });
        }

        [HttpPost("/addtempuser")]
        public async Task<IActionResult> AddTempUser([FromBody] Models.TempUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var user = _jsonConfigService.GetUser(request.Username);
            if (user != null)
            {
                return BadRequest("User already exists");
            }

            var allUsers = _jsonConfigService.GetUsers() ?? new List<LoginUser>();
            var newUser = new LoginUser
            {
                Username = request.Username,
                Password = request.Password,
                Role = "temp",
                Fullname = request.Username,
                InternalUserId = string.Empty,
                SessionContextKey = string.Empty,
                DefaultDatabase = string.Empty,
                WhatsAppNumber = string.Empty
            };

            allUsers.Add(newUser);
            _jsonConfigService.SaveGlobalConfig(Constants.ConfigFiles.Users, allUsers);

            var roles = _jsonConfigService.GetRoles("chinook-sqlite") ?? new List<Role>();
            var tempRole = roles.FirstOrDefault(r => r.Name.Equals("temp", StringComparison.OrdinalIgnoreCase));
            if (tempRole == null)
            {
                tempRole = new Role
                {
                    Name = "temp",
                    Description = "Temporary Users",
                    ConnectionString = string.Empty,
                    Modules = Array.Empty<string>(),
                    Users = new[] { request.Username }
                };
                roles.Add(tempRole);
            }
            else
            {
                var users = tempRole.Users?.ToList() ?? new List<string>();
                if (!users.Contains(request.Username, StringComparer.OrdinalIgnoreCase))
                {
                    users.Add(request.Username);
                    tempRole.Users = users.ToArray();
                }
            }

            _jsonConfigService.SaveDatabaseConfig("chinook-sqlite", Constants.ConfigFiles.Roles, roles);

            return Ok(new { success = true, message = "Temporary user added." });
        }

        [HttpPost("/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var validation = await UserService.ValidateLogin(_jsonConfigService, request.Username, request.Password);
            var result = validation;
            if (!result.isSuccess)
            {
                return Unauthorized("Invalid username or password");
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

            var allRoles = _jsonConfigService.GetRoles(latestDb);
            var userRole = allRoles.Find(x => x.Users.Contains(request.Username));
            var roleName = userRole != null ? userRole.Name : "Unknown";

            await _auditService.LogAsync(request.Username, AuditActions.Login, "Successful");

            return Ok(new { Token = result.session.Token, Message = "Login Successful", Role = roleName });
        }


 
    }
}
