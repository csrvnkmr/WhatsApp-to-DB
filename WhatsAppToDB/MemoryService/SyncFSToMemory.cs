using System.Collections.Concurrent;
using System.Dynamic;
using Microsoft.KernelMemory.AI;
using VectorDBSync.VectorDBService;
using WhatsAppToDB;
using WhatsAppToDB.MemoryService;
using WhatsAppToDB.Services;


public class FewShotMemoryHelper
{    
    private static readonly ConcurrentDictionary<string, List<string>> _syncedDatabases = new();
    private readonly JsonConfigService _configService;
    private readonly global::WhatsAppToDB.ILogger _logger;

    public FewShotMemoryHelper(JsonConfigService configService, global::WhatsAppToDB.ILogger logger)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task EnsureSyncDoneAsync(string targetDatabase, bool forceSync = false)
    {
        try {
        if (!_syncedDatabases.ContainsKey(targetDatabase) || forceSync)
        {
            _syncedDatabases.TryAdd(targetDatabase, new List<string>());
            await SyncFewShotsToMemoryAsync(targetDatabase);
        }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error syncing few-shots to memory for database '{targetDatabase}'", ex);
        }
    }

    public async Task SyncFewShotsToMemoryAsync(string targetDatabase)
    {
        var vectorDbSettings = _configService.GetVectorDBSettings(targetDatabase);
        if (vectorDbSettings == null)        {
            return;
        }

        var moduleQueries = _configService.GetModuleQueries(targetDatabase);
        var newQueries = moduleQueries.Where(q => !_syncedDatabases[targetDatabase].Contains(q.Name)).ToList();
        if (newQueries.Count == 0)
        {
            _logger.LogInfo($"No new few-shot queries to sync for database '{targetDatabase}'.");
            return;
        } else
        {
            _logger.LogInfo($"Syncing {newQueries.Count} new few-shot queries to memory for database '{targetDatabase}'.");
        }

        var vectorDBService = VectorDBServiceFactory.CreateVectorDBService(vectorDbSettings); 
        ITextEmbeddingGenerator embeddingGenerator = new ElBrunoKernelMemoryEmbeddingGenerator();
        var _memoryAdapter = new VectorDbMemoryAdapter(vectorDBService, embeddingGenerator);       
        ITextGenerator textGenerator = new MinimalTextGenerator();
        var fewShotMemoryService = new FewShotMemoryService(embeddingGenerator, _memoryAdapter, textGenerator);
        await fewShotMemoryService.SaveOrUpdateFewShotAsync(targetDatabase, newQueries);
        await fewShotMemoryService.DumpAllFewShotsAsync(targetDatabase);
    }

    public async Task<string> SearchFewShotsInMemoryAsync(string targetDatabase, string module, string query)
    {
        var vectorDbSettings = _configService.GetVectorDBSettings(targetDatabase);
        if (vectorDbSettings == null)        {
            return "";
        }

        var vectorDBService = VectorDBServiceFactory.CreateVectorDBService(vectorDbSettings); 
        ITextEmbeddingGenerator embeddingGenerator = new ElBrunoKernelMemoryEmbeddingGenerator();
        var _memoryAdapter = new VectorDbMemoryAdapter(vectorDBService, embeddingGenerator);       
        ITextGenerator textGenerator = new MinimalTextGenerator();
        var fewShotMemoryService = new FewShotMemoryService(embeddingGenerator, _memoryAdapter, textGenerator);
        var results = await fewShotMemoryService.FindClosestMatchIdAsync (targetDatabase, module, query);
        
        _logger.LogInfo($"Search results for query '{query}' in database '{targetDatabase}':");
        if (string.IsNullOrWhiteSpace(results))
        {
            _logger.LogInfo("No matches found.");
            return string.Empty;
        }
        var mq = _configService.GetModuleQueries(targetDatabase).
            FirstOrDefault(q => q.Name == results);
        _logger.LogInfo($"- {results} is the closet match for {query}");
        if (mq != null)
        {
            _logger.LogInfo($"- Matched ModuleQuery Name: {mq.Name}");
            _logger.LogInfo($"- Matched ModuleQuery Question: {mq.Query}");
            return mq.Query;
        }
        return "";
    }

    
}
