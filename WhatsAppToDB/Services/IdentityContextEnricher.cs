using WhatsAppToDB.Abstractions;

namespace WhatsAppToDB.Services
{
public interface IIdentityContextEnricher
    {
        void EnrichFromHttpContext(
            IdentityContext identity,
            HttpContext httpContext);
    }

    public class IdentityContextEnricher : IIdentityContextEnricher
    {
        public void EnrichFromHttpContext(
            IdentityContext identity,
            HttpContext httpContext)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));

            if (httpContext == null)
                throw new ArgumentNullException(nameof(httpContext));

            var userName =
                httpContext.Items["UserName"]?.ToString();

            if (!string.IsNullOrWhiteSpace(userName))
            {
                identity.UserName = userName;
            }

            identity.Database =
                httpContext.Session.GetString(Constants.SessionKeys.ActiveDb)
                ?? "";

            identity.LlmProvider =
                httpContext.Session.GetString(Constants.SessionKeys.ActiveLlmProvider)
                ?? "";

            identity.LlmModel =
                httpContext.Session.GetString(Constants.SessionKeys.ActiveLlmModel)
                ?? "";

            identity.IsWhatsAppRequest =
                string.Equals(
                    httpContext.Session.GetString(Constants.SessionKeys.IsWhatsAppRequest),
                    "true",
                    StringComparison.OrdinalIgnoreCase);

            identity.WhatsAppNumber =
                httpContext.Items["WhatsAppNumber"]?.ToString()
                ?? identity.WhatsAppNumber
                ?? "";

            identity.WhatsAppProfileId =
                httpContext.Items["WhatsAppProfileId"]?.ToString()
                ?? identity.WhatsAppProfileId
                ?? "";

            Validate(identity);
        }

        private static void Validate(IdentityContext identity)
        {
            if (string.IsNullOrWhiteSpace(identity.UserName))
            {
                throw new Exception(
                    "[IdentityContextEnricher] UserName is empty.");
            }

            if (string.IsNullOrWhiteSpace(identity.Database))
            {
                throw new Exception(
                    $"[IdentityContextEnricher] Active database is empty. User={identity.UserName}");
            }

            if (string.IsNullOrWhiteSpace(identity.LlmProvider))
            {
                throw new Exception(
                    $"[IdentityContextEnricher] Active LLM provider is empty. User={identity.UserName}, Db={identity.Database}");
            }

            if (string.IsNullOrWhiteSpace(identity.LlmModel))
            {
                throw new Exception(
                    $"[IdentityContextEnricher] Active LLM model is empty. User={identity.UserName}, Db={identity.Database}");
            }
        }
    }
}