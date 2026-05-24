using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsAppToDB.Abstractions;

namespace VectorDBSync.EmbeddingService
{
    public static class EmbeddingServiceFactory
    {
        private static readonly Dictionary<string, IEmbeddingServiceProvider> _services = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _sync = new();

        public static void Register(IEmbeddingServiceProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);
            if (string.IsNullOrWhiteSpace(provider.Type))
                throw new ArgumentException("Embedding service type cannot be empty.", nameof(provider));

            Register(provider.Type, provider);
        }

        public static void Register(string name, IEmbeddingServiceProvider provider)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Embedding service name cannot be empty.", nameof(name));

            ArgumentNullException.ThrowIfNull(provider);

            lock (_sync)
            {
                _services[name.Trim()] = provider;
            }
        }

        public static IReadOnlyCollection<string> GetRegisteredProviderNames()
        {
            lock (_sync)
            {
                return _services.Keys.OrderBy(name => name).ToArray();
            }
        }

        public static IEmbeddingService Create(EmbeddingServiceSettings settings)
        {
            var serviceName = settings.Type?.Trim();
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                serviceName = "openai";
            }

            lock (_sync)
            {
                if (_services.TryGetValue(serviceName, out var registeredService))
                {
                    return registeredService.CreateEmbeddingService(settings);
                }

                var createdService = CreateDefault(serviceName, settings);
                return createdService;
            }
        }

        private static IEmbeddingService CreateDefault(string serviceName, EmbeddingServiceSettings settings)
        {
            if (serviceName.Equals("local", StringComparison.OrdinalIgnoreCase))
            {
                return new ElBrunoEmbeddingService(settings.Model);
            }

            if (serviceName.Equals("openai", StringComparison.OrdinalIgnoreCase))
            {
                return new OpenAiEmbeddingService(settings.Model, settings.ApiKey);
            }

            throw new NotSupportedException($"Embedding service '{serviceName}' is not registered.");
        }
    }
}
