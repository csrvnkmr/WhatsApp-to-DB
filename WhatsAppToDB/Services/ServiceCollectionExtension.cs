//using Betalgo.Ranul.OpenAI.ObjectModels;
//using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.OpenApi.MicrosoftExtensions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Audit;
using WhatsAppToDB.Data;
using WhatsAppToDB.Database;
using WhatsAppToDB.DbProviders;
using WhatsAppToDB.LlmProviders;
using WhatsAppToDB.Models;
using WhatsAppToDB.Plugins;
using WhatsAppToDB.Settings;
using WhatsAppToDB.VectorStore;

namespace WhatsAppToDB.Services
{
    public static class ServiceCollectionExtension
    {

        public static void ConfigureAllServices(this IServiceCollection services, IConfiguration config, PluginMetadata metadata)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.SetIsOriginAllowed(_ => true) // This allows 'null' origins from local files
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            services.AddScoped<AiRequestContext>();

            services.AddSingleton<ILogger, AppLogger>();
            services.AddSingleton<JsonConfigService>();
            //services.AddSingleton(provider => new FolderUtils(provider.GetRequiredService<IConfiguration>()));
            services.AddSingleton<FolderUtils>();
            services.AddSingleton<ChatDbRepository>();
            services.AddSingleton<IUserAuditService, UserAuditService>();
            services.AddControllers();

            //var configRoot = config.GetValue<string>("ConfigRootFolder");

            var tempJsonConfig = new JsonConfigService(config, new AppLogger());
            var defaultFolders = tempJsonConfig.GetDefaultFolders();

            services.Configure<Settings.OpenAiSettings>(config.GetSection("OpenAiSettings"));
            services.Configure<LocalAiSettings>(config.GetSection("LocalAiSettings"));
            //services.AddDynamicExtensions(tempJsonConfig);

            //services.AddPlugin(config, metadata);

            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<Plugin.DatabaseQueryPlugin>();
            services.AddScoped<Plugin.SchemaPlugin>();
            services.AddEndpointsApiExplorer();
            //services.AddSwaggerGen();
            AddSwaggerGen(services);           
            
            var llmProviderPath = defaultFolders?.LlmProviderFolder ?? config.GetValue<string>("LlmPluginsFolder") ?? "Plugins/LLM";
            services.RegisterLlmProviders(llmProviderPath);

            services.AddDistributedMemoryCache();
            services.AddScoped<IQueryService, QueryService>();
            
            var dbProviderPath = defaultFolders?.DatabaseProviderFolder ?? config.GetValue<string>("DatabasePluginsFolder") ?? "Plugins/DB";
            services.RegisterDatabaseProviders(dbProviderPath);

            services.AddScoped<IIdentityContextEnricher, IdentityContextEnricher>();
            services.AddScoped<ExecutionContextService>();

            services.AddSingleton<VectorSyncStatusStore>();
            services.AddSingleton<VectorSyncJobService>();
            services.AddScoped<PluginLoaderService>();
            services.AddScoped<VectorKernelFunctionFactory>();
            services.AddScoped<KernelTestService>();
            services.AddKernel();

            services.AddHttpContextAccessor();
            services.AddSession();

        }

        /* private static void LoadDatabaseConfigs(IServiceCollection services, string configPath)
        {
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"[DatabaseRegistry] Config file not found at {configPath}");
                return;
            }
            List<DatabaseConfig> lstDbConfigs = 
                System.Text.Json.JsonSerializer.Deserialize<List<DatabaseConfig>>(File.ReadAllText(configPath)) ?? new List<DatabaseConfig>();
            services.AddSingleton<List<DatabaseConfig>>(lstDbConfigs);
        } 
        */

        private static void AddSwaggerGen(IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "InsightChat API",
                    Version = "v1"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your Bearer token (without 'Bearer ' prefix):"
                });

                // Swashbuckle 10.x new delegate-based syntax
                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                });
            });
        }

        public static void RegisterDatabaseProviders(this IServiceCollection services, string folderName)
        {
            services.AddScoped<DatabaseContextService>();
            services.AddSingleton<DatabaseRegistry>();

            services.AddSingleton<IDbProvider, Database.MsSqlDbProvider>();
            services.AddSingleton<IDbProvider, Database.SqliteDbProvider>();
            services.AddSingleton<ISchemaProvider, SqlServerSchemaProvider>();
            services.AddSingleton<ISchemaProvider, SqliteSchemaProvider>();
            services.LoadProviders<IDbProvider>(folderName, "*dbplugin.dll", ServiceLifetime.Singleton);            
            services.AddSingleton<Database.DbProviderFactory>();  // Changed from AddScoped to AddSingleton
        }        

        public static void RegisterLlmProviders(this IServiceCollection services, string configPath)
        {
            services.AddScoped<LlmContextService>();
            services.AddScoped<ILlmProvider, OpenAiProvider>();
            services.AddScoped<ILlmProvider, LocalAiProvider>();
            //LoadLlmPlugins(services);
            services.LoadProviders<ILlmProvider>(configPath, "*llmplugin.dll");

            services.AddSingleton<LlmRegistry>();
            services.AddScoped<LlmProviderFactory>();
        }

        static void LoadProviders<TInterface>(this IServiceCollection services, string pluginFolder, string filefilter,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            
            if (!Directory.Exists(pluginFolder))
                return;

            var dlls = Directory.GetFiles(pluginFolder, filefilter);

            foreach (var file in dlls)
            {
                try
                {
                    var asm = Assembly.LoadFrom(file);

                    var types = asm.GetTypes()
                        .Where(t =>
                            typeof(TInterface).IsAssignableFrom(t) &&
                            !t.IsInterface &&
                            !t.IsAbstract);

                    foreach (var type in types)
                    {
                        services.Add(new ServiceDescriptor(typeof(TInterface), type, lifetime));

                        Console.WriteLine($"[Plugin:{typeof(TInterface).Name}] Loaded: {type.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Plugin:{typeof(TInterface).Name}] Failed: {file}");
                    Console.WriteLine(ex.Message);
                }
            }
        }

/*      public static void AddKernelOLD(this IServiceCollection services, PluginMetadata metadata)
        {
            services.AddScoped(sp =>
            {
                // Use a unique ID to track this specific resolution in the console
                var requestId = Guid.NewGuid().ToString().Substring(0, 4);
                Console.WriteLine($"[WhatsAppToDB] [{requestId}] Building Kernel...");


                var kernelBuilder = Kernel.CreateBuilder();
                var llmContext = sp.GetRequiredService<LlmContextService>();

                var provider = llmContext.GetProvider();

                var model = llmContext.GetModel();

                provider.Register(kernelBuilder, sp, model);

                //var aiSettings = sp.GetRequiredService<IOptions<CommonAiSettings>>().Value;
                //var llmfactory = sp.GetRequiredService<LlmProviderFactory>();
                //var provider = llmfactory.Get(aiSettings.Provider);
                //provider.Register(kernelBuilder, sp, aiSettings.Model);

                var dbPlugin = sp.GetRequiredService<Plugin.DatabaseQueryPlugin>();
                var schemaPlugin = sp.GetRequiredService<Plugin.SchemaPlugin>();
                kernelBuilder.Plugins.AddFromObject(dbPlugin);
                kernelBuilder.Plugins.AddFromObject(schemaPlugin);


                // Add all dynamic plugins loaded from DLLs
                foreach (var pluginType in metadata.PluginTypes)
                {
                    var pluginInstance = sp.GetRequiredService(pluginType);
                    kernelBuilder.Plugins.AddFromObject(pluginInstance);
                    Console.WriteLine($"[Kernel] Registered Plugin: {pluginType.Name}");
                }

                return kernelBuilder.Build();
            });
        }
 */

/*      public static void AddPlugin(this IServiceCollection services, IConfiguration config, PluginMetadata metadata)
        {
            using var tempProvider = services.BuildServiceProvider();
            var logger = tempProvider.GetService<ILogger>();
            logger.WriteToConsole = true;

            var lstpluginSettings = config.GetSection("PluginSettings").Get<List<PluginSettings>>();
            if (lstpluginSettings != null)
            {
                foreach (var pluginSettings in lstpluginSettings)
                {
                    logger.LogAsync($"[AddPlugin] Attempting to load plugin from {pluginSettings.AssemblyPath} with class {pluginSettings.PluginClassName}");
                    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(pluginSettings.AssemblyPath));

                    var pluginDir = Path.GetDirectoryName(pluginSettings.AssemblyPath);
                    AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
                    {
                        // 1. Check if already loaded in the AppDomain
                        var alreadyLoaded = AppDomain.CurrentDomain.GetAssemblies()
                            .FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
                        if (alreadyLoaded != null) return alreadyLoaded;

                        // 2. SEARCH STRATEGY: Look in the EXE folder AND the Plugins folder
                        string[] searchPaths = {
                            AppDomain.CurrentDomain.BaseDirectory, // Current EXE folder
                            pluginDir!                             // Your Plugins folder
                        };

                        foreach (var dir in searchPaths)
                        {
                            string path = Path.Combine(dir, $"{assemblyName.Name}.dll");
                            if (File.Exists(path))
                            {
                                // IMPORTANT: Using LoadFromAssemblyPath FORCES the runtime 
                                // to use this file, even if the version isn't an exact match.
                                return context.LoadFromAssemblyPath(path);
                            }
                        }

                        return null;
                    };

                    var pluginType = assembly.GetType(pluginSettings.PluginClassName);

                    if (pluginType != null)
                    {
                        if (metadata.PluginTypes == null)
                        {
                            metadata.PluginTypes = new List<Type>();
                        }
                        if (!metadata.PluginTypes.Contains(pluginType))
                        {
                            metadata.PluginTypes.Add(pluginType);
                            // Register for DI so Kernel can resolve it
                            services.AddScoped(pluginType);
                        }
                        else
                        {
                            logger.LogAsync($"[AddPlugin] Failed to register plugin: {pluginSettings.PluginClassName}");
                        }
                    }
                }
                services.AddSingleton(metadata);
            }
        } */

        public static void AddKernel(this IServiceCollection services)
        {
            services.AddScoped(sp =>
            {
                // Use a unique ID to track this specific resolution in the console
                var requestId = Guid.NewGuid().ToString().Substring(0, 4);
                Console.WriteLine($"[WhatsAppToDB] [{requestId}] Building Kernel...");                                

                var kernelBuilder = Kernel.CreateBuilder();
                var llmContext = sp.GetRequiredService<LlmContextService>();
                var llmRegistry = sp.GetRequiredService<LlmRegistry>();

                var provider = llmContext.GetProvider();
                var llmConfig = llmRegistry.Get(provider.Name);

                var model = llmContext.GetModel();

                provider.Register(kernelBuilder, llmConfig, model);

                //var aiSettings = sp.GetRequiredService<IOptions<CommonAiSettings>>().Value;
                //var llmfactory = sp.GetRequiredService<LlmProviderFactory>();
                //var provider = llmfactory.Get(aiSettings.Provider);
                //provider.Register(kernelBuilder, sp, aiSettings.Model);

                var dbPlugin = sp.GetRequiredService<Plugin.DatabaseQueryPlugin>();
                var schemaPlugin = sp.GetRequiredService<Plugin.SchemaPlugin>();
                kernelBuilder.Plugins.AddFromObject(dbPlugin);
                kernelBuilder.Plugins.AddFromObject(schemaPlugin);

                var dbContext = sp.GetRequiredService<DatabaseContextService>();

                var database = dbContext.GetCurrentDatabaseName();

                var json = sp.GetRequiredService<JsonConfigService>();

                var pluginLoader = sp.GetRequiredService<PluginLoaderService>();

                var plugins = json.GetPlugins(database);

                foreach (var plugin in plugins)
                {
                    var pluginInstance = pluginLoader.CreatePluginInstance(plugin);

                    if (pluginInstance == null) continue;

                    kernelBuilder.Plugins.AddFromObject(pluginInstance);

                    Console.WriteLine($"[Kernel] Added Plugin: {plugin.Name}");
                }

                var vectorFactory = sp.GetRequiredService<VectorKernelFunctionFactory>();

                var vectorPlugin = vectorFactory.CreatePlugin(database);

                kernelBuilder.Plugins.Add(vectorPlugin);

                return kernelBuilder.Build();
            });
        }

    }
}
