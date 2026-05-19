using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WhatsAppToDB.Constants;
using WhatsAppToDB.Models;

namespace WhatsAppToDB.Services
{
    // ==========================================================
    // TOKEN AUTH MIDDLEWARE
    // Reads:
    // Authorization: Bearer xxxxx
    // Validates token
    // ==========================================================
    public class TokenAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public TokenAuthMiddleware(RequestDelegate next, ILogger? logger = null)
        {
            _next = next;
            _logger = logger ?? new AppLogger();
        }

        public async Task InvokeAsync(HttpContext context,
            JsonConfigService jsonConfigService)
        {

            var path = context.Request.Path.Value?.ToLower();
            var method = context.Request.Method.ToUpperInvariant();
            // --------------------------------------------------
            // Always allow OPTIONS
            // --------------------------------------------------
            if (method == HttpMethods.Options)
            {
                await _next(context);
                return;
            }

            // --------------------------------------------------
            // WhatsApp webhook GET verification should be public
            // --------------------------------------------------
            if (path.StartsWith("/webhook") && method == HttpMethods.Get)
            {
                await _next(context);
                return;
            }

            // public routes
            if (
                path.StartsWith("/swagger") ||
                path == "/login" ||
                path.StartsWith("/css") ||
                path.StartsWith("/debug") ||
                path.StartsWith("/api/schema") ||
                path.StartsWith("/js")
            )
            {
                await _next(context);
                return;
            }

            // --------------------------------------------------
            // WhatsApp webhook POST:
            // authenticate by WhatsApp sender number + profile phone id
            // --------------------------------------------------
            if (path.StartsWith("/webhook") && method == HttpMethods.Post)
            {

                if (!await AssignWhatsAppContext(context, jsonConfigService))
                {
                    // AssignWhatsAppContext handles response in case of failure
                    return;
                }
                /*
                var whatsappContext =
                    await TryResolveWhatsAppContextAsync(
                        context,
                        jsonConfigService);

                if (!whatsappContext.Success)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync(whatsappContext.ErrorMessage);
                    return;
                }

                context.Items[Constants.HttpContextItems.IsWhatsAppRequest] = "true";
                context.Items[Constants.HttpContextItems.UserName] = whatsappContext.User!.Username;
                context.Items[Constants.HttpContextItems.WhatsAppNumber] = whatsappContext.WhatsAppNumber!;
                context.Items[Constants.HttpContextItems.WhatsAppPhoneId] = whatsappContext.PhoneId!;
                context.Items[Constants.HttpContextItems.WhatsAppProfileId] = whatsappContext.Profile!.ProfileId;
                context.Items[Constants.HttpContextItems.ForcedDatabase] = whatsappContext.Profile.Database;
                */
                await _next(context);
                return;
            }

            // ----------------------------------------------
            // Read Authorization header
            // ----------------------------------------------
            var authHeader =
                context.Request.Headers["Authorization"]
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(authHeader) ||
                !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing token");
                return;
            }

            var token =
                authHeader.Substring("Bearer ".Length).Trim();

            // ----------------------------------------------
            // Validate token
            // Replace with your real validation method
            // ----------------------------------------------
            var (isValid,session) =
                UserService.ValidateToken(token);

            if (!isValid || session == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid token");
                return;
            }

            // ----------------------------------------------
            // Store values for endpoint use
            // ----------------------------------------------
            context.Items[ContextItems.UserName] = session.UserName;
            context.Items[ContextItems.Token] = token;

            await _next(context);
        }

        private async Task<bool> AssignWhatsAppContext(HttpContext context, 
            JsonConfigService jsonConfigService)
        {
            
            var whatsappContext =
                await TryResolveWhatsAppContextAsync(
                    context,                        jsonConfigService);

            if (!whatsappContext.Success)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync(whatsappContext.ErrorMessage);
                return false;
            }

            context.Items[Constants.SessionKeys.IsWhatsAppRequest] = "true";
            context.Items[Constants.ContextItems.UserName] = whatsappContext.User!.Username;
            context.Items[Constants.ContextItems.WhatsAppNumber] = whatsappContext.WhatsAppNumber!;
            context.Items[Constants.ContextItems.WhatsAppDatabase] = whatsappContext.Profile!.Database;
            context.Items[Constants.ContextItems.WhatsAppProfileId] = whatsappContext.Profile!.ProfileId;
            
            return true;
        }

        private async Task<WhatsAppResolveResult> TryResolveWhatsAppContextAsync(
            HttpContext context,
            JsonConfigService jsonConfigService)
        {
            try
            {
                context.Request.EnableBuffering();

                using var reader =
                    new StreamReader(
                        context.Request.Body,
                        leaveOpen: true);

                var body = await reader.ReadToEndAsync();

                context.Request.Body.Position = 0;

                if (string.IsNullOrWhiteSpace(body))
                {
                    return WhatsAppResolveResult.Fail("Empty WhatsApp webhook body");
                }

                var parsed = ExtractWhatsAppFields(body);

                if (string.IsNullOrWhiteSpace(parsed.WhatsAppNumber))
                {
                    return WhatsAppResolveResult.Fail("WhatsApp sender number not found");
                }

                if (string.IsNullOrWhiteSpace(parsed.PhoneId))
                {
                    return WhatsAppResolveResult.Fail("WhatsApp phone id not found");
                }

                var user =
                    jsonConfigService.GetUserByWhatsAppNumber(parsed.WhatsAppNumber);

                if (user == null)
                {
                    return WhatsAppResolveResult.Fail(
                        $"WhatsApp number is not registered: {parsed.WhatsAppNumber}");
                }

                var profile =
                    jsonConfigService.GetWhatsAppProfile(parsed.PhoneId);

                if (profile == null)
                {
                    return WhatsAppResolveResult.Fail(
                        $"WhatsApp profile not found for phone id: {parsed.PhoneId}");
                }

                if (string.IsNullOrWhiteSpace(profile.Database))
                {
                    return WhatsAppResolveResult.Fail(
                        $"WhatsApp profile has no database configured: {profile.ProfileId}");
                }

                return WhatsAppResolveResult.Ok(
                    parsed.WhatsAppNumber,
                    parsed.PhoneId,
                    user,
                    profile);
            }
            catch (Exception ex)
            {
                await _logger.LogInfoAsync(
                    $"Exception resolving WhatsApp context in TokenAuthMiddleware: {ex}");

                return WhatsAppResolveResult.Fail(
                    "Error resolving WhatsApp user");
            }
        }

        private static WhatsAppPayloadFields ExtractWhatsAppFields(string body)
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            string? from = null;
            string? phoneId = null;

            if (root.TryGetProperty("entry", out var entries) &&
                entries.ValueKind == JsonValueKind.Array &&
                entries.GetArrayLength() > 0)
            {
                var entry = entries[0];

                if (entry.TryGetProperty("changes", out var changes) &&
                    changes.ValueKind == JsonValueKind.Array &&
                    changes.GetArrayLength() > 0)
                {
                    var change = changes[0];

                    if (change.TryGetProperty("value", out var value))
                    {
                        if (value.TryGetProperty("metadata", out var metadata))
                        {
                            if (metadata.TryGetProperty("phone_number_id", out var phoneIdElement))
                            {
                                phoneId = phoneIdElement.GetString();
                            }
                        }

                        if (value.TryGetProperty("messages", out var messages) &&
                            messages.ValueKind == JsonValueKind.Array &&
                            messages.GetArrayLength() > 0)
                        {
                            var message = messages[0];

                            if (message.TryGetProperty("from", out var fromElement))
                            {
                                from = fromElement.GetString();
                            }
                        }
                    }
                }
            }

            return new WhatsAppPayloadFields
            {
                WhatsAppNumber = NormalizeWhatsAppNumber(from),
                PhoneId = phoneId
            };
        }

        private static string? NormalizeWhatsAppNumber(string? number)
        {
            if (string.IsNullOrWhiteSpace(number))
                return null;

            number = number.Trim();

            // WhatsApp Cloud API usually sends without '+'
            // Store consistently. If your users.json stores with '+',
            // add it here.
            if (!number.StartsWith("+"))
                number = "+" + number;

            return number;
        }

        private sealed class WhatsAppPayloadFields
        {
            public string? WhatsAppNumber { get; set; }
            public string? PhoneId { get; set; }
        }

        private sealed class WhatsAppResolveResult
        {
            public bool Success { get; private set; }
            public string ErrorMessage { get; private set; } = "";
            public string? WhatsAppNumber { get; private set; }
            public string? PhoneId { get; private set; }
            public LoginUser? User { get; private set; }
            public WhatsAppProfile? Profile { get; private set; }

            public static WhatsAppResolveResult Ok(
                string whatsAppNumber,
                string phoneId,
                LoginUser user,
                WhatsAppProfile profile)
            {
                return new WhatsAppResolveResult
                {
                    Success = true,
                    WhatsAppNumber = whatsAppNumber,
                    PhoneId = phoneId,
                    User = user,
                    Profile = profile
                };
            }

            public static WhatsAppResolveResult Fail(string error)
            {
                return new WhatsAppResolveResult
                {
                    Success = false,
                    ErrorMessage = error
                };
            }
        }
    }
}