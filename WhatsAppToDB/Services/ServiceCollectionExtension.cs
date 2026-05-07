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
using WhatsAppToDB.LlmProviders;
using WhatsAppToDB.Models;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.Services
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDynamicExtensions(this IServiceCollection services, IConfiguration config)
        {

            using var tempProvider = services.BuildServiceProvider();
            var logger = tempProvider.GetService<ILogger>();
            logger.WriteToConsole= true;

            // Load paths from AppSettings
            var promptDll = config["Extensions:PromptInjector:DllPath"];
            var interceptorDll = config["Extensions:SqlInterceptor:DllPath"];
            var sqlTemplateDll = config["Extensions:SqlTemplateExtension:DllPath"];

            // Registering your Schema/Prompt Injector
            services.RegisterPluginFromDll<IModulePrompt>(promptDll, logger);

            // Registering your SQL Template Provider
            services.RegisterPluginFromDll<ISqlTemplateExtension>(sqlTemplateDll, logger);

            // Registering your SQL Interceptor Layer
            services.RegisterPluginFromDll<ISqlInterceptor>(interceptorDll, logger);
            return services;
        }

        private static IServiceCollection RegisterPluginFromDll<TInterface>(
            this IServiceCollection services, string dllPath, ILogger logger) where TInterface : class

        {
            if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath))
            {
                logger.LogAsync($"[Extension] Skipping load: Path null or file not found at {dllPath}");
                return services;
            }

            try
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(dllPath));

                // Find implementation of the generic TInterface
                var type = assembly.GetTypes().FirstOrDefault(t =>
                    typeof(TInterface).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                if (type != null)
                {
                    // Register the found type against the Interface
                    services.AddScoped(typeof(TInterface), type);
                    logger.LogAsync($"[Extension] Successfully registered {typeof(TInterface).Name}: {type.Name}");
                }
                else
                {
                    logger.LogAsync($"[Extension] No implementation of {typeof(TInterface).Name} found in {dllPath}");
                }
            }
            catch (Exception ex)
            {
                logger.LogAsync($"[Extension] Error loading {typeof(TInterface).Name} from {dllPath}: {ex.Message}");
            }

            return services;
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

            services.AddScoped<AiRequestContext>();

            services.AddSingleton<ILogger, AppLogger>();
            services.AddSingleton<ChatDbRepository>();
            services.AddSingleton<IUserAuditService, UserAuditService>();
            services.AddControllers();

            services.Configure<WhatsAppSettings>(config.GetSection("WhatsAppSettings"));
            services.Configure<DatabaseSettings>(config.GetSection("DatabaseSettings"));
            services.Configure<OpenAiSettings>(config.GetSection("OpenAiSettings"));
            services.Configure<CommonAiSettings>(config.GetSection("CommonAiSettings"));
            services.Configure<LocalAiSettings>(config.GetSection("LocalAiSettings"));
            services.Configure<RoleSettings>(config.GetSection("RoleSettings"));
            services.Configure<MailSettings>(config.GetSection("MailSettings"));
            services.Configure<DefaultSettings>(config.GetSection("DefaultSettings"));
            services.AddDynamicExtensions(config);

            services.AddPlugin(config, metadata);



            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<Plugin.DatabaseQueryPlugin>();
            services.AddScoped<Plugin.SchemaPlugin>();
            services.AddEndpointsApiExplorer();
            //services.AddSwaggerGen();
            AddSwaggerGen(services);

            var llmConfigPath = config.GetValue<string>("LlmSettings:LlmConfigFile");
            LoadLlmConfigs(services, llmConfigPath);
            services.AddScoped<LlmContextService>();
            
            services.AddScoped<LlmProviderFactory>();
            var llmPluginsPath = config.GetValue<string>("LlmSettings:LlmPluginsFolder");
            services.RegisterLlmProviders(llmPluginsPath);

            var dbConfigPath = config.GetValue<string>("DatabaseSettings:DatabaseConfigFile");

            LoadDatabaseConfigs(services, dbConfigPath);
            services.AddScoped<DatabaseContextService>();
            services.AddSingleton<DatabaseRegistry>();
            services.AddScoped<DbProviderFactory>();
            services.AddDistributedMemoryCache();
            services.AddScoped<IQueryService, QueryService>();
            

            var dbPluginsPath = config.GetValue<string>("DatabaseSettings:DatabasePluginsFolder");
            services.RegisterDatabaseProviders(dbPluginsPath);
            
            services.AddKernel(metadata);

            services.AddHttpContextAccessor();
            services.AddSession();

        }

        private static void LoadDatabaseConfigs(IServiceCollection services, string configPath)
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

        

        public static void AddPlugin(this IServiceCollection services, IConfiguration config, PluginMetadata metadata)
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
                        if (metadata.PluginTypes==null)
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
        }

        public static void RegisterDatabaseProviders(this IServiceCollection services, string folderName)
        {
            services.AddScoped<DatabaseContextService>();
            services.AddScoped<IDbProvider, Database.MsSqlDbProvider>();
            services.AddScoped<IDbProvider, Database.SqliteDbProvider>();
            LoadPlugins<IDbProvider>(services, folderName);            
            services.AddScoped<Database.DbProviderFactory>();
        }

        private static void LoadLlmConfigs(IServiceCollection services, string configPath)
        {
            if (!File.Exists(configPath))
                return;

            var configs =
                JsonSerializer.Deserialize<List<LlmConfig>>(
                    File.ReadAllText(configPath))
                ?? new();

            services.AddSingleton(configs);
            services.AddSingleton<LlmRegistry>();
        }

        public static void RegisterLlmProviders(this IServiceCollection services, string configPath)
        {
            services.AddScoped<LlmContextService>();
            services.AddScoped<ILlmProvider, OpenAiProvider>();
            services.AddScoped<ILlmProvider, LocalAiProvider>();
            //LoadLlmPlugins(services);
            LoadPlugins<ILlmProvider>(services, configPath);
            services.AddScoped<LlmProviderFactory>();            
        }

        //static void LoadLlmPlugins(IServiceCollection services)
        //{
        //    var pluginPath = Path.Combine(AppContext.BaseDirectory, "plugins/llm");

        //    if (!Directory.Exists(pluginPath))
        //        return;

        //    var dlls = Directory.GetFiles(pluginPath, "*.dll");

        //    foreach (var file in dlls)
        //    {
        //        try
        //        {

        //            var asm = Assembly.LoadFrom(file);

        //            var types = asm.GetTypes()
        //                .Where(t =>
        //                    typeof(ILlmProvider).IsAssignableFrom(t) &&
        //                    !t.IsInterface &&
        //                    !t.IsAbstract);

        //            foreach (var type in types)
        //            {
        //                services.AddScoped(typeof(ILlmProvider), type);
        //                Console.WriteLine($"[LLM Plugin] Loaded: {type.Name}");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"[LLM Plugin] Failed to load from {file}: {ex.Message}");
        //            Console.WriteLine(ex.ToString());
        //        }
        //    }
        //}

        static void LoadPlugins<TInterface>(IServiceCollection services, string pluginFolder, 
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            

            if (!Directory.Exists(pluginFolder))
                return;

            var dlls = Directory.GetFiles(pluginFolder, "*llmplugin.dll");

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

        public static void AddKernel(this IServiceCollection services, PluginMetadata metadata)
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


    }
}
