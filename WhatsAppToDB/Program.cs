using System.Text.Json;
using System.Text.Json.Nodes;
using WhatsAppToDB.Data;
using WhatsAppToDB.Database;
using WhatsAppToDB.Services;
using WhatsAppToDB.Settings;
using VectorDBSync;
using WhatsAppToDB;

var currentFolder = Directory.GetCurrentDirectory();
var appSettingsPath = Path.Combine(currentFolder, "appsettings.json");
var logger = new AppLogger { WriteToConsole = true };
EnsureAppSettingsFile(appSettingsPath, currentFolder, logger);

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

var app = builder.Build();
app.UseRouting();
app.UseCors("AllowAll");
app.UseSession();

app.UseSwagger();
app.UseSwaggerUI();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseMiddleware<TokenAuthMiddleware>();
app.UseMiddleware<SessionBootstrapMiddleware>();

app.MapControllers();
app.MapFallbackToFile("index.html");

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

static void EnsureAppSettingsFile(string appSettingsPath, string currentFolder, AppLogger logger)
{
    var configRootPath = Path.Combine(currentFolder, "Config");

    if (!File.Exists(appSettingsPath))
    {
        logger.LogInfo($"Creating appsettings.json at {appSettingsPath}");
        var root = new JsonObject
        {
            ["ConfigRootFolder"] = configRootPath
        };

        File.WriteAllText(appSettingsPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        logger.LogInfo($"Writing ConfigRootFolder='{configRootPath}' to {appSettingsPath}");
        CopyConfigDefaults(configRootPath, currentFolder, logger);
        return;
    }

    try
    {
        var json = File.ReadAllText(appSettingsPath);
        var node = JsonNode.Parse(json) as JsonObject ?? new JsonObject();

        if (node["ConfigRootFolder"] == null || string.IsNullOrWhiteSpace(node["ConfigRootFolder"]?.GetValue<string>()))
        {
            node["ConfigRootFolder"] = configRootPath;
            File.WriteAllText(appSettingsPath, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            logger.LogInfo($"Added missing ConfigRootFolder='{configRootPath}' to {appSettingsPath}");
        }

        var configRoot = node["ConfigRootFolder"]?.GetValue<string>() ?? configRootPath;
        configRoot = GetAbsolutePath(configRoot, currentFolder);
        CopyConfigDefaults(configRoot, currentFolder, logger);
    }
    catch (Exception ex)
    {
        logger.LogWarning($"Failed to parse existing appsettings.json, recreating default layout. Reason: {ex.Message}");
        var root = new JsonObject
        {
            ["ConfigRootFolder"] = configRootPath
        };

        File.WriteAllText(appSettingsPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        logger.LogInfo($"Created fallback appsettings.json with ConfigRootFolder='{configRootPath}'");
        CopyConfigDefaults(configRootPath, currentFolder, logger);
    }
}

static void CopyConfigDefaults(string configRoot, string currentFolder, AppLogger logger)
{
    if (string.IsNullOrWhiteSpace(configRoot))
        configRoot = Path.Combine(currentFolder, "Config");

    if (!Path.IsPathRooted(configRoot))
        configRoot = Path.GetFullPath(Path.Combine(currentFolder, configRoot));

    var configDefaultsRoot = Path.Combine(currentFolder, "ConfigDefaults");
    if (!Directory.Exists(configDefaultsRoot))
        return;

    Directory.CreateDirectory(configRoot);

    var defaultFiles = Directory.GetFiles(configDefaultsRoot, "*.json", SearchOption.AllDirectories);
    var databasesCopied = false;

    foreach (var sourceFile in defaultFiles)
    {
        var relativePath = Path.GetRelativePath(configDefaultsRoot, sourceFile);
        var destinationFile = Path.Combine(configRoot, relativePath);

        var destinationDir = Path.GetDirectoryName(destinationFile);
        if (!Directory.Exists(destinationDir))
            Directory.CreateDirectory(destinationDir!);

        if (!File.Exists(destinationFile))
        {
            logger.LogInfo($"Copying {sourceFile} to {destinationFile}");
            File.Copy(sourceFile, destinationFile);
            if (string.Equals(relativePath, "Databases.json", StringComparison.OrdinalIgnoreCase))
            {
                databasesCopied = true;
            }
        }
    }

    if (databasesCopied)
    {
        var sqliteSource = Path.Combine(configDefaultsRoot, "Data", "Chinook_Sqlite.sqlite");
        var sqliteTargetDir = Path.Combine(currentFolder, "data", "sqlite");
        var sqliteTarget = Path.Combine(sqliteTargetDir, "Chinook_Sqlite.sqlite");

        if (!File.Exists(sqliteSource))
        {
            logger.LogWarning($"SQLite source file not found: {sqliteSource}");
        }
        else
        {
            if (!Directory.Exists(sqliteTargetDir))
            {
                logger.LogInfo($"Creating SQLite data folder {sqliteTargetDir}");
                Directory.CreateDirectory(sqliteTargetDir);
            }

            if (!File.Exists(sqliteTarget))
            {
                logger.LogInfo($"Copying {sqliteSource} to {sqliteTarget}");
                File.Copy(sqliteSource, sqliteTarget);
            }
        }
    }
}

static string GetAbsolutePath(string path, string baseFolder)
{
    if (Path.IsPathRooted(path))
        return path;

    return Path.GetFullPath(Path.Combine(baseFolder, path));
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
        Console.WriteLine($"  - Vector DB Type: {vectorDbSettings.VectorDBProviderSettings.Type}");
        Console.WriteLine($"  - Cache Folder: {vectorDbSettings.CacheFolder}");
        Console.WriteLine($"  - SQLite VectorDB Folder: {vectorDbSettings.VectorDBProviderSettings.VectorDBFolder}");

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

