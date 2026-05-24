using System;
using System.Collections.Generic;
using System.Linq;
using WhatsAppToDB.Abstractions;
using VectorDBSync.EmbeddingService;

namespace VectorDBSync.VectorDBService
{
    public static class VectorDBServiceFactory
    {
        private static readonly Dictionary<string, IVectorDBServiceProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _sync = new();

        static VectorDBServiceFactory()
        {
            Register(new SQLiteVectorDBServiceProvider());
        }

        public static void Register(IVectorDBServiceProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);
            if (string.IsNullOrWhiteSpace(provider.Type))
                throw new ArgumentException("Vector DB provider type cannot be empty.", nameof(provider));

            Register(provider.Type, provider);
        }

        public static void Register(string name, IVectorDBServiceProvider provider)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Vector DB provider name cannot be empty.", nameof(name));

            ArgumentNullException.ThrowIfNull(provider);

            lock (_sync)
            {
                _providers[name.Trim()] = provider;
            }
        }

        public static IReadOnlyCollection<string> GetRegisteredProviderNames()
        {
            lock (_sync)
            {
                return _providers.Keys.OrderBy(name => name).ToArray();
            }
        }

        public static IVectorDBService CreateVectorDBService(VectorDBSettings settings)
        {
            var providerType = settings.VectorDBProviderSettings.Type?.Trim();
            if (string.IsNullOrWhiteSpace(providerType))
            {
                providerType = "sqlite";
            }

            var embeddingService = EmbeddingServiceFactory.Create(settings.EmbeddingServiceSettings);

            lock (_sync)
            {
                if (_providers.TryGetValue(providerType, out var provider))
                {
                    return provider.CreateVectorDBService(settings, embeddingService);
                }
            }

            if (providerType.Equals("sqlite", StringComparison.OrdinalIgnoreCase))
            {
                return new SQLiteVectorDBService(settings);
            }

            throw new NotSupportedException($"Vector DB provider '{providerType}' is not registered.");
        }
    }
}
