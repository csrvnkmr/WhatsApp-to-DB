using WhatsAppToDB.Abstractions;

namespace WhatsAppToDB.LlmProviders
{
    public class LlmProviderFactory
    {
        private readonly IServiceProvider _sp;
        private readonly Dictionary<string, Type> _providerTypes;

        public LlmProviderFactory(IServiceProvider sp, IEnumerable<ILlmProvider> providers)
        {
            _sp = sp;

            _providerTypes = providers.ToDictionary(
                p => p.Name,
                p => p.GetType()
            , StringComparer.OrdinalIgnoreCase) ;
        }

        public ILlmProvider Get(string name)
        {
            if (!_providerTypes.ContainsKey(name))
                throw new Exception($"Provider {name} not found");

            var type = _providerTypes[name];

            return (ILlmProvider)ActivatorUtilities.CreateInstance(_sp, type);
        }
    }
}
