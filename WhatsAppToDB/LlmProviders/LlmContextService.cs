using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Services;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.LlmProviders
{
    

    public class LlmContextService
    {
        private readonly IHttpContextAccessor _http;
        private readonly LlmProviderFactory _factory;
        private readonly DefaultSettings _defaultSettings;
        private readonly IUserAuditService _userAuditService;
        private readonly ExecutionContextService _executionContext;

        public LlmContextService(
            IHttpContextAccessor http,
            LlmProviderFactory factory,
            JsonConfigService jsonConfigService,
            IUserAuditService userAuditService,
            ExecutionContextService executionContext)
        {
            _http = http;
            _factory = factory;
            _defaultSettings = jsonConfigService.GetDefaultSettings();
            _userAuditService = userAuditService;
            _executionContext = executionContext;
        }

        public string GetProviderName()
        {                
            if (!string.IsNullOrWhiteSpace(_executionContext.LlmProvider))
                return _executionContext.LlmProvider;
            return _http.HttpContext?.Session?.GetString(Constants.SessionKeys.ActiveLlmProvider)
                   ?? _defaultSettings.DefaultLlmProvider;
        }

        public string GetModel()
        {
            
            if (!string.IsNullOrWhiteSpace(_executionContext.LlmModel))
                return _executionContext.LlmModel;

            return _http.HttpContext?.Session?.GetString(Constants.SessionKeys.ActiveLlmModel)
                   ?? _defaultSettings.DefaultLlmModel;
        }

        public ILlmProvider GetProvider()
        {
            return _factory.Get(GetProviderName());
        }
    }
}
