using WhatsAppToDB.Services;

namespace WhatsAppToDB.LlmProviders
{
    public class LlmRegistry
    {
        private readonly JsonConfigService _jsonConfigService;

        public LlmRegistry(JsonConfigService jsonConfigService)
        {
            _jsonConfigService = jsonConfigService;
        }

        public List<LlmConfig> GetAll()
        {
            List<LlmConfig> _configs;
            _configs = _jsonConfigService.GetLlmConfigs();
            return _configs.Where(x => x.Enabled).ToList();
        }

        public LlmConfig GetByName(string name)
        {
            List<LlmConfig> _configs = GetAll();
            return _configs.First(x =>
                x.Enabled &&
                x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public LlmConfig Get(string provider)
        {
            List<LlmConfig> _configs = GetAll();
            return _configs.First(x =>
                x.Enabled &&
                x.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase));
        }
    }
}
