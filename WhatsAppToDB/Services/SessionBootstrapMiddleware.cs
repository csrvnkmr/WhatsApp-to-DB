using Microsoft.Extensions.Options;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Audit;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.Services
{
    public class SessionBootstrapMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionBootstrapMiddleware(
            RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IUserAuditService audit,
            IOptions<DefaultSettings> defaultSettings)
        {
            // Skip if not authenticated
            var userName = context.Items["UserName"]?.ToString();
            if (!string.IsNullOrWhiteSpace(userName))
            {

                var activeDb =
                    context.Session.GetString(Constants.SessionKeys.ActiveDb);

                // Session missing -> restore
                if (string.IsNullOrWhiteSpace(activeDb))
                {
                    var db =
                        await audit.GetLatestValueAsync(
                            userName,
                            AuditActions.DatabaseChanged)
                        ?? defaultSettings.Value.DefaultDatabase;

                    var provider =
                        await audit.GetLatestValueAsync(userName, AuditActions.ProviderChanged)
                        ?? defaultSettings.Value.DefaultLlmProvider;

                    var model =
                        await audit.GetLatestValueAsync(userName, AuditActions.ModelChanged)
                        ?? defaultSettings.Value.DefaultLlmModel;
                    if (model.Split(',').Length > 1)
                    {
                        provider = model.Split(',')[0].Trim();
                        model = model.Split(',')[1].Trim();
                    }
                    context.Session.SetString(Constants.SessionKeys.ActiveDb, db);

                    context.Session.SetString(Constants.SessionKeys.ActiveLlmProvider, provider);

                    context.Session.SetString(Constants.SessionKeys.ActiveLlmModel, model);

                    Console.WriteLine($"[SessionBootstrap] Restored for {userName}");
                }
            }

            await _next(context);
        }
    }
}
