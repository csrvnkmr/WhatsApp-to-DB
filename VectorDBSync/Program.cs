using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Text.Json;
using WhatsAppToDB.Abstractions;
using VectorDBSync.VectorDBService;
using VectorDBSync;

// Currently this will not work.
// The sync service should be used as a class library
// by passing the settings and connection string from the host application that references it.

IDbProvider dbProvider = GetDbProvider("SqlServer");

var json = await File.ReadAllTextAsync("vectorConfig.json");
var vectorConfigs = JsonSerializer.Deserialize<VectorSyncRoot>(json, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
});

var settingsJson = await File.ReadAllTextAsync("appsettings.json");
var settings = JsonSerializer.Deserialize<VectorDBSettings>(settingsJson, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
}) ?? throw new JsonException("Failed to deserialize Vector DB settings from appsettings.json.");

LoadVectorDbProviders(Path.Combine(AppContext.BaseDirectory, "Plugins", "VectorDB"));

var sourceConnectionString = string.Empty;
ISyncService vss = new VectorSyncService(settings, sourceConnectionString);

await vss.SyncAllCollections(vectorConfigs.SyncCollections, dbProvider);
var ts = new TestSearch();
await TestSearch. TestSearchB1_2(vss);

Console.WriteLine("Press Enter to close");
Console.ReadLine();

static IDbProvider GetDbProvider(string providerType)
{
    throw new NotSupportedException($"Database provider '{providerType}' is not supported.");
}

static void LoadVectorDbProviders(string pluginFolder)
{
    if (string.IsNullOrWhiteSpace(pluginFolder) || !Directory.Exists(pluginFolder))
        return;

    foreach (var file in Directory.GetFiles(pluginFolder, "*vectordbprovider.dll"))
    {
        try
        {
            var asm = Assembly.LoadFrom(file);

            var types = asm.GetTypes()
                .Where(t =>
                    typeof(IVectorDBServiceProvider).IsAssignableFrom(t) &&
                    !t.IsInterface &&
                    !t.IsAbstract);

            foreach (var type in types)
            {
                if (Activator.CreateInstance(type) is not IVectorDBServiceProvider provider)
                    continue;

                VectorDBServiceFactory.Register(provider);
                Console.WriteLine($"[VectorDBProvider] Loaded: {type.Name} ({provider.Type})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VectorDBProvider] Failed: {file}");
            Console.WriteLine(ex.Message);
        }
    }
}
