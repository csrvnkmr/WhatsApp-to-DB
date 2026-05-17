using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Text.RegularExpressions;
using VectorDBSync;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Database;
using WhatsAppToDB.Services;

namespace WhatsAppToDB.VectorStore
{
    public class VectorKernelFunctionFactory
    {
        private readonly JsonConfigService _jsonConfigService;
        private readonly DatabaseRegistry _databaseRegistry;
        private readonly ILogger _logger;

        public VectorKernelFunctionFactory(
            JsonConfigService jsonConfigService,
            DatabaseRegistry databaseRegistry,
            ILogger logger)
        {
            _jsonConfigService = jsonConfigService;
            _databaseRegistry = databaseRegistry;
            _logger = logger;
        }

        public KernelPlugin CreatePlugin(string database)
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("Database cannot be blank.", nameof(database));

            // Validate database exists. Your registry throws if invalid.
            var dbConfig =
                _databaseRegistry.GetDatabaseConfig(database);

            var vectorDbSettings =
                _jsonConfigService.GetVectorDBSettings(database);

            var syncConfigs =
                _jsonConfigService.GetVectorSyncConfigurations(database);

            var functions =
                new List<KernelFunction>();

            foreach (var config in syncConfigs)
            {
                var function =
                    CreateFunction(
                        database,
                        dbConfig.ConnectionString ?? string.Empty,
                        vectorDbSettings,
                        config);

                functions.Add(function);
            }

            return KernelPluginFactory.CreateFromFunctions(
                pluginName: "VectorSearch",
                description: $"Dynamic vector search functions for database '{database}'.",
                functions: functions);
        }

        private KernelFunction CreateFunction(
            string database,
            string connectionString,
            VectorDBSettings vectorDbSettings,
            VectorSyncConfig config)
        {
            var functionName = config.GetFunctionName() ?? BuildFunctionName(config);

            var description = config.GetDescription()  ?? BuildDescription(config);

            return KernelFunctionFactory.CreateFromMethod(
                async (string query) =>
                {
                    return await SearchAsync(
                        database,
                        connectionString,
                        vectorDbSettings,
                        config,
                        query);
                },
                functionName: functionName,
                description: description);
        }

        private async Task<string> SearchAsync(
            string database,
            string connectionString,
            VectorDBSettings vectorDbSettings,
            VectorSyncConfig config,
            string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return JsonSerializer.Serialize(
                    Error(
                        database,
                        config,
                        "Search query cannot be blank."),
                    JsonOptions());
            }

            try
            {
                ISyncService syncService =
                    new VectorSyncService(
                        vectorDbSettings,
                        connectionString);
                Console.WriteLine("[VectorKernelFunctionFactory] Starting vector search. " +
                    $"Database={database}, Collection={config.CollectionName}, Query={query}");
                var results =
                    await syncService.SearchCollection(
                        config.CollectionName,
                        query);

                var match =
                    results.FirstOrDefault();

                if (match == null)
                {
                    return JsonSerializer.Serialize(
                        Error(
                            database,
                            config,
                            $"No match found for '{query}' in collection '{config.CollectionName}'."),
                        JsonOptions());
                }

                var metadata =
                    match.Metadata?
                        .ToDictionary(
                            x => x.Key,
                            x => x.Value)
                    ?? new Dictionary<string, object?>();

                var result =
                    new VectorKernelSearchResult
                    {
                        Success = true,
                        Database = database,
                        CollectionName = config.CollectionName,
                        TableName = config.TableName,
                        KeyField = config.KeyField,
                        KeyValue = match.Id,
                        ContentField = config.ContentField,
                        ContentValue = match.Document ?? string.Empty,
                        Distance = (double)match.Distance,
                        Metadata = metadata,
                        Message =
                            $"Matched {config.TableName}.{config.ContentField}: " +
                            $"{match.Document} ({config.KeyField}={match.Id})"
                    };

                return JsonSerializer.Serialize(
                    result,
                    JsonOptions());
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(
                    $"[VectorKernelFunctionFactory] Search failed. " +
                    $"Database={database}, Collection={config.CollectionName}, Query={query}, Error={ex}");

                return JsonSerializer.Serialize(
                    Error(
                        database,
                        config,
                        $"Vector search failed: {ex.Message}"),
                    JsonOptions());
            }
        }

        private static VectorKernelSearchResult Error(
            string database,
            VectorConfiguration config,
            string message)
        {
            return new VectorKernelSearchResult
            {
                Success = false,
                Database = database,
                CollectionName = config.CollectionName,
                TableName = config.TableName,
                KeyField = config.KeyField,
                ContentField = config.ContentField,
                Message = message
            };
        }

        private static string BuildFunctionName(
            VectorConfiguration config)
        {
            var table =
                ToSafeIdentifier(config.TableName);

            var key =
                ToSafeIdentifier(config.KeyField);

            var name =
                $"Get{table}{key}";

            if (string.IsNullOrWhiteSpace(table) ||
                string.IsNullOrWhiteSpace(key))
            {
                name =
                    $"Search{ToSafeIdentifier(config.CollectionName)}";
            }

            return name;
        }

        private static string BuildDescription(
            VectorConfiguration config)
        {
            return
                $"Searches vector collection '{config.CollectionName}' " +
                $"to find the official {config.KeyField} from a fuzzy {config.ContentField}. " +
                $"Use this before generating SQL when the user provides an approximate name for " +
                $"{config.TableName}.{config.ContentField}. " +
                $"Returns JSON containing {config.KeyField}, {config.ContentField}, distance, and metadata.";
        }

        private static string ToSafeIdentifier(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var cleaned =
                Regex.Replace(
                    value,
                    @"[^A-Za-z0-9_]",
                    string.Empty);

            if (string.IsNullOrWhiteSpace(cleaned))
                return string.Empty;

            if (char.IsDigit(cleaned[0]))
                cleaned = "_" + cleaned;

            return cleaned;
        }

        private static JsonSerializerOptions JsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true
            };
        }
    }
}