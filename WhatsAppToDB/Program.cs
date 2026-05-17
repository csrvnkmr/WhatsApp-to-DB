using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

using WhatsAppToDB;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;
using WhatsAppToDB.Database;
using WhatsAppToDB.Services;
using WhatsAppToDB.Settings;
using WhatsAppToDB.VectorStore;
using VectorDBSync;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);
var metadata = new PluginMetadata();

// Centralize all service configurations in one method for better organization
// this is an extension method in ServiceCollectionExtension.cs
builder.Services.ConfigureAllServices(builder.Configuration, metadata);
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".InsightChat.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
//await TestSqliteExecution();
//await AdventureWorksTestHarness.RunTestMessages(builder);
//await AdventureWorksTestHarness.RunSecurityTests(builder);

var app = builder.Build();
app.UseRouting();
app.UseCors("AllowAll");
app.UseSession();


app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles(); // This will serve index.html if it's in a folder named wwwroot

app.UseMiddleware<TokenAuthMiddleware>();
app.UseMiddleware<SessionBootstrapMiddleware>();

app.MapControllers();

// Uncomment the line below to run Vector Sync test
//await TestVectorSync(app, "B1Database2");

using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<ChatDbRepository>();
    await repo.InitializeAsync();
}
//await VectorDbService.TestVectorDbService();
app.Run();


async Task TestSqliteExecution()
{
    var repo = new WhatsAppToDB.Tests.SqliteRepositoryTests();
    await repo.Full_Sqlite_Test_Create_Insert_Select_Delete();
}

/// <summary>
/// Test method to sync vector databases using VectorSyncService.
/// Loads settings via JsonConfigService and configs from vectorconfiguration.json.
/// Cache files (SQLite database) are stored in CacheFolder/{database}
/// Vector DBs (if SQLite) are stored in SqliteSettings.VectorDBFolder
/// </summary>
async Task TestVectorSync(WebApplication app, string database = "B1Database2")
{
    try
    {
        Console.WriteLine($"\n========== Starting Vector Sync Test for {database} ==========\n");

        // Get JsonConfigService from DI container
        Console.WriteLine($"Loading JsonConfigService from DI container...");
        var jsonConfigService = app.Services.GetRequiredService<JsonConfigService>();
        Console.WriteLine($"✓ JsonConfigService obtained");
        var dbRegistry = app.Services.GetRequiredService<DatabaseRegistry>();
        var dbConfig = dbRegistry.GetDatabaseConfig(database); // validate database exists in registry, will throw if not  
        Console.WriteLine($"✓ Database '{database}' found in registry with description: {dbConfig.Description}");

        // Load VectorDBSettings from JsonConfigService (handles decryption, metadata, etc.)
        Console.WriteLine($"\nLoading VectorDBSettings via JsonConfigService for database: {database}");
        var vectorDbSettings = jsonConfigService.GetVectorDBSettings(database);
        
        if (vectorDbSettings == null)
        {
            Console.WriteLine($"ERROR: VectorDBSettings not found for database: {database}");
            return;
        }

        Console.WriteLine($"✓ Loaded VectorDBSettings");
        Console.WriteLine($"  - Embedding Service: {vectorDbSettings.EmbeddingServiceSettings.Type}");
        Console.WriteLine($"  - Vector DB Type: {vectorDbSettings.VectorDBProvider.Type}");
        Console.WriteLine($"  - Cache Folder: {vectorDbSettings.CacheFolder}");
        Console.WriteLine($"  - SQLite VectorDB Folder: {vectorDbSettings.SqliteSettings.VectorDBFolder}");

        /*
        // Load VectorSyncConfig array from vectorconfiguration.json
        string baseConfigPath = $"Config/databases/{database}";
        string configPath = Path.Combine(baseConfigPath, "vectorconfiguration.json");

        if (!File.Exists(configPath))
        {
            Console.WriteLine($"ERROR: Configuration file not found: {configPath}");
            return;
        }

        Console.WriteLine($"\nLoading configurations from: {configPath}");
        var configJson = await File.ReadAllTextAsync(configPath);
        var syncConfigs = JsonSerializer.Deserialize<List<VectorSyncConfig>>(configJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        */
        var syncConfigs = jsonConfigService.GetVectorSyncConfigurations(database);

        if (syncConfigs == null )
        {
            Console.WriteLine("ERROR: Failed to deserialize VectorSyncConfig or list is empty");
            return;
        }

        Console.WriteLine($"✓ Loaded {syncConfigs.Count} sync configurations:");
        foreach (var config in syncConfigs)
        {
            Console.WriteLine($"  - {config.CollectionName}: {config.TableName} ({config.KeyField} -> {config.ContentField})");
        }

        // Create VectorSyncService with the settings
        Console.WriteLine($"\nCreating VectorSyncService...");
        ISyncService syncService = new VectorSyncService(vectorDbSettings, dbConfig.ConnectionString ?? string.Empty);
        Console.WriteLine($"✓ VectorSyncService created with connection string: {dbConfig.ConnectionString}");

        // Get IDbProvider from DbProviderFactory by name
        Console.WriteLine($"\nGetting IDbProvider from DbProviderFactory for 'mssql'...");
        var dbProviderFactory = app.Services.GetRequiredService<DbProviderFactory>();
        var dbProvider = dbProviderFactory.GetDbProvider("mssql");
        Console.WriteLine($"✓ IDbProvider obtained: {dbProvider.Name}");

        // Start the sync
        Console.WriteLine($"\n========== Starting Sync ==========\n");
        await syncService.SyncAllCollections(syncConfigs, dbProvider);

        Console.WriteLine($"\n========== Vector Sync Test Completed Successfully ==========\n");

        async Task TestSearchCollection(string collectionName, string query)
        {
            Console.WriteLine($"\n [{collectionName}]Testing search  for query: '{query}'");
            var results = await syncService.SearchCollection(collectionName, query);
            Console.WriteLine($"Search completed. Found {results.Count} results.");
            foreach (var res in results)
            {
                Console.WriteLine($"[{collectionName}] - ID: {res.Id}, Distance: {res.Distance}, Document: {res.Document}");
            }
        }


        await TestSearchCollection("VectorOCRD", "SG auto parts");
        await TestSearchCollection("VectorOITM", "Socket and Wire");
        await TestSearchCollection("VectorOITB", "Japanes vehicles");
        await TestSearchCollection("VectorOSLP", "kasthuri");
        await TestSearchCollection("VectorOCRG", "china Chonqing");
        
        //await TestSearchCollection("FEWSHOTQUERIES", "Customer Ledger");
    }    
    catch (Exception ex)
    {
        Console.WriteLine($"\nERROR in Vector Sync Test: {ex.Message}");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
    }
}

