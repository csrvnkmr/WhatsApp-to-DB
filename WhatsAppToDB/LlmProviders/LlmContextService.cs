using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Models;
using WhatsAppToDB.Services;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.LlmProviders
{
    

    public class LlmContextService
    {
        private readonly IHttpContextAccessor _http;
        private readonly LlmProviderFactory _factory;
        private readonly LlmRegistry _llmRegistry;
        private readonly DefaultSettings _defaultSettings;
        private readonly IUserAuditService _userAuditService;
        private readonly AiRequestContext _requestContext;

        public LlmContextService(
            IHttpContextAccessor http,
            LlmProviderFactory factory,
            LlmRegistry llmRegistry,
            JsonConfigService jsonConfigService,
            IUserAuditService userAuditService,
            AiRequestContext requestContext)
        {
            _http = http;
            _factory = factory;
            _llmRegistry = llmRegistry;
            _defaultSettings = jsonConfigService.GetDefaultSettings();
            _userAuditService = userAuditService;
            _requestContext = requestContext;
        }

        public string GetProviderName()
        {                
            if (!string.IsNullOrWhiteSpace(_requestContext?.LlmProvider))
                return _requestContext.LlmProvider;
            return _http.HttpContext?.Session?.GetString(Constants.SessionKeys.ActiveLlmProvider)
                   ?? _defaultSettings.DefaultLlmProvider;
        }

        public string GetModel()
        {
            
            if (!string.IsNullOrWhiteSpace(_requestContext?.LlmModel))
                return _requestContext.LlmModel;

            return _http.HttpContext?.Session?.GetString(Constants.SessionKeys.ActiveLlmModel)
                   ?? _defaultSettings.DefaultLlmModel;
        }

        public ILlmProvider GetProvider()
        {
            var selectedName = GetProviderName();
            var selectedConfig = _llmRegistry.GetByName(selectedName);
            return _factory.Get(selectedConfig.Provider);
        }
    }
}
