using System.Collections.Concurrent;
using VectorDBSync;
using WhatsAppToDB.Database;

namespace WhatsAppToDB.Services
{
    public class VectorSyncJobService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly VectorSyncStatusStore _statusStore;
        private readonly ILogger _logger;

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public VectorSyncJobService(
            IServiceScopeFactory scopeFactory,
            VectorSyncStatusStore statusStore,
            ILogger logger)
        {
            _scopeFactory = scopeFactory;
            _statusStore = statusStore;
            _logger = logger;
        }

        public bool Start(string database)
        {
            var syncLock =
                _locks.GetOrAdd(database, _ => new SemaphoreSlim(1, 1));

            if (syncLock.CurrentCount == 0)
                return false;

            _ = Task.Run(async () =>
            {
                await RunInternal(database, syncLock);
            });

            return true;
        }

        private async Task RunInternal(
            string database,
            SemaphoreSlim syncLock)
        {
            await syncLock.WaitAsync();

            try
            {
                _statusStore.SetRunning(database);

                await _logger.LogAsync(
                    $"[VectorSync] Started for database {database}");

                using var scope =
                    _scopeFactory.CreateScope();

                var jsonConfigService =
                    scope.ServiceProvider.GetRequiredService<JsonConfigService>();

                var dbRegistry =
                    scope.ServiceProvider.GetRequiredService<DatabaseRegistry>();

                var dbProviderFactory =
                    scope.ServiceProvider.GetRequiredService<DbProviderFactory>();

                var dbConfig =
                    dbRegistry.GetDatabaseConfig(database);

                var vectorDbSettings =
                    jsonConfigService.GetVectorDBSettings(database);

                var syncConfigs =
                    jsonConfigService.GetVectorSyncConfigurations(database);

                var dbProvider = dbProviderFactory.GetDbProvider(dbConfig.DbProvider);

                ISyncService syncService =
                    new VectorSyncService(
                        vectorDbSettings,
                        dbConfig.ConnectionString ?? string.Empty);

                await syncService.SyncAllCollections(syncConfigs, dbProvider);

                _statusStore.SetCompleted(
                    database,
                    true,
                    "Completed");
            }
            catch (Exception ex)
            {
                _statusStore.SetCompleted(
                    database,
                    false,
                    ex.Message);

                await _logger.LogAsync(
                    $"[VectorSync] Failed for database {database}. {ex}");
            }
            finally
            {
                syncLock.Release();
            }
        }
    }
}