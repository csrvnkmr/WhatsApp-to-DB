//using Betalgo.Ranul.OpenAI.ObjectModels;
//using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory.AI;
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
using WhatsAppToDB.MemoryService;
using WhatsAppToDB.Models;
using WhatsAppToDB.Plugins;
using WhatsAppToDB.Settings;
using WhatsAppToDB.VectorStore;
using VectorDBSync.EmbeddingService;
using VectorDBSync.VectorDBService;
using WhatsAppToDB.Eval;

namespace WhatsAppToDB.Services
{
    public static class ServiceCollectionExtension
    {
        private static readonly ILogger _logger;

        static ServiceCollectionExtension()
        {
            _logger = new AppLogger { WriteToConsole = true };
        }

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

            services.AddSingleton<ILogger, AppLogger>();
            services.AddScoped<AiRequestContext>();

            services.AddSingleton<JsonConfigService>();
            //services.AddSingleton(provider => new FolderUtils(provider.GetRequiredService<IConfiguration>()));
            services.AddSingleton<FolderUtils>();
            services.AddSingleton<ChatDbRepository>();
            services.AddSingleton<IUserAuditService, UserAuditService>();
            services.AddControllers();

            //var configRoot = config.GetValue<string>("ConfigRootFolder");

            var tempJsonConfig = new JsonConfigService(config, new AppLogger());
            var defaultFolders = tempJsonConfig.GetDefaultFolders();

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

            var embeddingProviderPath = defaultFolders?.EmbeddingProviderFolder ?? config.GetValue<string>("EmbeddingPluginsFolder") ?? "Plugins/Embedding";
            services.RegisterEmbeddingServiceProviders(embeddingProviderPath);

            var vectorDbProviderPath = defaultFolders?.VectorDBProviderFolder ?? config.GetValue<string>("VectorDBPluginsFolder") ?? "Plugins/VectorDB";
            services.RegisterVectorDBProviders(vectorDbProviderPath);

            services.AddDistributedMemoryCache();
            services.AddScoped<IQueryService, QueryService>();
            services.AddSingleton<LlmCancellationService>();
            services.AddScoped<EvalRunRepository>();
            services.AddScoped<EvalReportRepository>();
            
            var dbProviderPath = defaultFolders?.DatabaseProviderFolder ?? config.GetValue<string>("DatabasePluginsFolder") ?? "Plugins/DB";
            services.RegisterDatabaseProviders(dbProviderPath);

            services.AddScoped<IIdentityContextEnricher, IdentityContextEnricher>();

            services.AddScoped<UserInstructionRepository>();            

            services.AddSingleton<VectorSyncStatusStore>();
            services.AddSingleton<VectorSyncJobService>();
            services.AddScoped<PluginLoaderService>();
            services.AddScoped<VectorKernelFunctionFactory>();
            services.AddScoped<KernelTestService>();

            services.AddScoped<FewShotMemoryHelper>();

            services.AddKernel();

            // Register ITextGenerator for Kernel Memory integration (minimal implementation)
            services.AddScoped<ITextGenerator>(sp => new MinimalTextGenerator());

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
            services.AddSingleton<ISchemaProvider, MsSqlSchemaProvider>();
            services.AddSingleton<ISchemaProvider, SqliteSchemaProvider>();
            services.LoadProviders<IDbProvider>(folderName, "*DBPlugin.dll", ServiceLifetime.Singleton);            
            services.LoadProviders<ISchemaProvider>(folderName, "*DBPlugin.dll", ServiceLifetime.Singleton);            
            services.AddSingleton<Database.DbProviderFactory>();  // Changed from AddScoped to AddSingleton
        }        

        public static void RegisterLlmProviders(this IServiceCollection services, string configPath)
        {
            services.AddScoped<LlmContextService>();
            services.AddScoped<ILlmProvider, OpenAiProvider>();
            services.AddScoped<ILlmProvider, LocalAiProvider>();
            //LoadLlmPlugins(services);
            services.LoadProviders<ILlmProvider>(configPath, "*LlmPlugin.dll");

            services.AddSingleton<LlmRegistry>();
            services.AddScoped<LlmProviderFactory>();
        }

        public static void RegisterEmbeddingServiceProviders(this IServiceCollection services, string folderName)
        {
            var builtIns = new IEmbeddingServiceProvider[]
            {
                new ElBrunoEmbeddingServiceProvider(),
                new OpenAiEmbeddingServiceProvider()
            };

            foreach (var provider in builtIns)
            {
                services.AddSingleton<IEmbeddingServiceProvider>(provider);
                EmbeddingServiceFactory.Register(provider);
            }

            LoadEmbeddingProviders(services, folderName, "*EmbeddingProvider.dll");
        }

        public static void RegisterVectorDBProviders(this IServiceCollection services, string folderName)
        {
            var builtIns = new IVectorDBServiceProvider[]
            {
                new SQLiteVectorDBServiceProvider()
            };

            foreach (var provider in builtIns)
            {
                services.AddSingleton<IVectorDBServiceProvider>(provider);
                VectorDBServiceFactory.Register(provider);
            }

            LoadVectorDBProviders(services, folderName, "*VectorDBProvider.dll");
        }

        static void LoadProviders<TInterface>(this IServiceCollection services, string pluginFolder, string filefilter,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            _logger.LogInfo($"Checking for plugins {filefilter} in {pluginFolder}");
            if (!Directory.Exists(pluginFolder))
            {
                _logger.LogInfo($"Directory does not exist {pluginFolder}");
                return;
            }
            var dlls = Directory.GetFiles(pluginFolder, filefilter);

            foreach (var file in dlls)
            {
                try
                {
                    _logger.LogInfo($"Loading plugins {file} from {pluginFolder}");
                    var asm = Assembly.LoadFrom(file);

                    var types = asm.GetTypes()
                        .Where(t =>
                            typeof(TInterface).IsAssignableFrom(t) &&
                            !t.IsInterface &&
                            !t.IsAbstract);

                    foreach (var type in types)
                    {
                        services.Add(new ServiceDescriptor(typeof(TInterface), type, lifetime));

                        _logger.LogInfo($"[Plugin:{typeof(TInterface).Name}] Loaded: {type.Name}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[Plugin:{typeof(TInterface).Name}] Failed: {file}");
                    _logger.LogError(ex.Message);
                }
            }
        }
        
        static void LoadEmbeddingProviders(IServiceCollection services, string pluginFolder, string filefilter)
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
                            typeof(IEmbeddingServiceProvider).IsAssignableFrom(t) &&
                            !t.IsInterface &&
                            !t.IsAbstract);

                    foreach (var type in types)
                    {
                        services.Add(new ServiceDescriptor(typeof(IEmbeddingServiceProvider), type, ServiceLifetime.Singleton));
                        _logger.LogInfo($"[Plugin:EmbeddingServiceProvider] Loaded: {type.Name}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[Plugin:EmbeddingServiceProvider] Failed: {file}");
                    _logger.LogError(ex.Message);
                }
            }
        }

        static void LoadVectorDBProviders(IServiceCollection services, string pluginFolder, string filefilter)
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
                            typeof(IVectorDBServiceProvider).IsAssignableFrom(t) &&
                            !t.IsInterface &&
                            !t.IsAbstract);

                    foreach (var type in types)
                    {
                        services.Add(new ServiceDescriptor(typeof(IVectorDBServiceProvider), type, ServiceLifetime.Singleton));
                        _logger.LogInfo($"[Plugin:VectorDBServiceProvider] Loaded: {type.Name}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[Plugin:VectorDBServiceProvider] Failed: {file}");
                    _logger.LogError(ex.Message);
                }
            }
        }

        public static void AddKernel(this IServiceCollection services)
        {
            services.AddScoped(sp =>
            {
                // Use a unique ID to track this specific resolution in the console
                var requestId = Guid.NewGuid().ToString().Substring(0, 4);
                _logger.LogInfo($"[WhatsAppToDB] [{requestId}] Building Kernel...");                                

                var kernelBuilder = Kernel.CreateBuilder();
                var llmContext = sp.GetRequiredService<LlmContextService>();
                var llmRegistry = sp.GetRequiredService<LlmRegistry>();

                var selectedName = llmContext.GetProviderName();
                var llmConfig = llmRegistry.GetByName(selectedName);
                var provider = llmContext.GetProvider();

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

                    _logger.LogInfo($"[Kernel] Added Plugin: {plugin.Name}");
                }

                var vectorFactory = sp.GetRequiredService<VectorKernelFunctionFactory>();

                var vectorPlugin = vectorFactory.CreatePlugin(database);

                kernelBuilder.Plugins.Add(vectorPlugin);

                return kernelBuilder.Build();
            });
        }

    }
}
