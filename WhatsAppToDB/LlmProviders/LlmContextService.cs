using Microsoft.Extensions.Options;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.LlmProviders
{
    

    public class LlmContextService
    {
        private readonly IHttpContextAccessor _http;
        private readonly LlmProviderFactory _factory;
        private readonly DefaultSettings _defaultSettings;
        private readonly IUserAuditService _userAuditService;

        public LlmContextService(
            IHttpContextAccessor http,
            LlmProviderFactory factory,
            IOptions<DefaultSettings> defaultSettings,
            IUserAuditService userAuditService  )
        {
            _http = http;
            _factory = factory;
            _defaultSettings = defaultSettings.Value;
            _userAuditService = userAuditService;
        }

        public string GetProviderName()
        {                
            return _http.HttpContext?.Session?.GetString(Constants.SessionKeys.ActiveLlmProvider)
                   ?? _defaultSettings.DefaultLlmProvider;
        }

        public string GetModel()
        {
            return _http.HttpContext?.Session?.GetString(Constants.SessionKeys.ActiveLlmModel)
                   ?? _defaultSettings.DefaultLlmModel;
        }

        public ILlmProvider GetProvider()
        {
            return _factory.Get(GetProviderName());
        }
    }
}
