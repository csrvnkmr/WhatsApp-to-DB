using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Audit;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.Services
{
    public class SessionBootstrapMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly JsonConfigService _jsonConfigService;

        public SessionBootstrapMiddleware(
            RequestDelegate next,
            JsonConfigService jsonConfigService)
        {
            _next = next;
            _jsonConfigService = jsonConfigService;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IUserAuditService audit)
        {
            var defaultSettings =
                _jsonConfigService.GetDefaultSettings();

            var userName =
                context.Items[Constants.ContextItems.UserName]?.ToString();

            if (!string.IsNullOrWhiteSpace(userName))
            {
                var isWhatsAppRequest =
                    string.Equals(
                        context.Items[Constants.SessionKeys.IsWhatsAppRequest]?.ToString(),
                        "true",
                        StringComparison.OrdinalIgnoreCase);

                if (isWhatsAppRequest)
                {
                    BootstrapWhatsAppSession(
                        context,
                        defaultSettings);
                }
                else
                {
                    await BootstrapNormalSessionAsync(
                        context,
                        audit,
                        userName,
                        defaultSettings);
                }
            }

            await _next(context);
        }

        private void BootstrapWhatsAppSession(
            HttpContext context,
            DefaultSettings defaultSettings)
        {
            var database =
                context.Items[Constants.ContextItems.WhatsAppDatabase]?.ToString();

            if (string.IsNullOrWhiteSpace(database))
            {
                throw new InvalidOperationException(
                    "WhatsApp database was not set in request context.");
            }

            var provider =
                defaultSettings.DefaultLlmProvider;

            var model =
                defaultSettings.DefaultLlmModel;

            if (!string.IsNullOrWhiteSpace(model) &&
                model.Split(',').Length > 1)
            {
                provider = model.Split(',')[0].Trim();
                model = model.Split(',')[1].Trim();
            }
            context.Session.SetString(
                Constants.SessionKeys.ActiveDb,
                database);

            var verifyDb =
                context.Session.GetString(Constants.SessionKeys.ActiveDb);

            Console.WriteLine(
                $"[SessionBootstrap] SET ActiveDb={database}, "+ 
                $"VERIFY={verifyDb}, SessionId={context.Session.Id}, Path={context.Request.Path}");
            context.Session.SetString(
                Constants.SessionKeys.ActiveLlmProvider,
                provider);

            context.Session.SetString(
                Constants.SessionKeys.ActiveLlmModel,
                model);

            context.Session.SetString(
                Constants.SessionKeys.IsWhatsAppRequest,
                "true");

            Console.WriteLine(
                $"[SessionBootstrap] WhatsApp session set. Db={database}, Provider={provider}, Model={model}");
        }

        private async Task BootstrapNormalSessionAsync(
            HttpContext context,
            IUserAuditService audit,
            string userName,
            DefaultSettings defaultSettings)
        {
            context.Session.SetString(
                Constants.SessionKeys.IsWhatsAppRequest,
                "false");

            var activeDb =
                context.Session.GetString(Constants.SessionKeys.ActiveDb);

            // Existing normal flow
            if (!string.IsNullOrWhiteSpace(activeDb))
                return;

            var loginUser =
                _jsonConfigService.GetUser(userName);

            var defaultDb =
                loginUser?.DefaultDatabase
                ?? defaultSettings.DefaultDatabase;

            var db =
                await audit.GetLatestValueAsync(
                    userName,
                    AuditActions.DatabaseChanged)
                ?? defaultDb;

            var provider =
                await audit.GetLatestValueAsync(
                    userName,
                    AuditActions.ProviderChanged)
                ?? defaultSettings.DefaultLlmProvider;

            var model =
                await audit.GetLatestValueAsync(
                    userName,
                    AuditActions.ModelChanged)
                ?? defaultSettings.DefaultLlmModel;

            if (!string.IsNullOrWhiteSpace(model) &&
                model.Split(',').Length > 1)
            {
                provider = model.Split(',')[0].Trim();
                model = model.Split(',')[1].Trim();
            }

            context.Session.SetString(
                Constants.SessionKeys.ActiveDb,
                db);

            context.Session.SetString(
                Constants.SessionKeys.ActiveLlmProvider,
                provider);

            context.Session.SetString(
                Constants.SessionKeys.ActiveLlmModel,
                model);

            Console.WriteLine(
                $"[SessionBootstrap] Restored for {userName}");
        }
    }
}