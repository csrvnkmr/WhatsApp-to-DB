namespace WhatsAppToDB.LlmProviders
{
    public class LlmRegistryOld
    {
        private readonly List<LlmConfig> _configs;

        public LlmRegistryOld(List<LlmConfig> configs)
        {
            _configs = configs;
        }

        public List<LlmConfig> GetAll()
        {
            return _configs.Where(x => x.Enabled).ToList();
        }

        public LlmConfig Get(string provider)
        {
            return _configs.First(x =>
                x.Enabled &&
                x.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase));
        }
    }
}
