using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using System.Runtime.Loader;
using WhatsAppToDB.Abstractions;

namespace WhatsAppToDB
{
    public static class ServiceCollectionExtension
    {
        public static void AddDynamicExtensions(this IServiceCollection services, IConfiguration config)
        {

            using var tempProvider = services.BuildServiceProvider();
            var logger = tempProvider.GetService<IWhatsAppLogger>();
            logger.WriteToConsole= true;

            // Load paths from AppSettings
            var promptDll = config["Extensions:PromptInjector:DllPath"];
            var interceptorDll = config["Extensions:SqlInterceptor:DllPath"];

            // Use your existing AssemblyLoadContext logic to load these
            if (!string.IsNullOrEmpty(promptDll) && File.Exists(promptDll))
            {
                try
                {
                    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(promptDll));

                    // Find implementation of IModulePrompt
                    var type = assembly.GetTypes().FirstOrDefault(t => typeof(IModulePrompt).IsAssignableFrom(t) && !t.IsInterface);
                    if (type != null)
                    {
                        services.AddScoped(typeof(IModulePrompt), type);                        
                        logger.LogAsync($"[Extension] Registered Prompt Injector: {type.Name}");
                        //Console.WriteLine($"[Extension] Registered Prompt Injector: {type.Name}");
                    } else
                    {
                        logger.LogAsync($"[Extension] Failed to register Prompt Injector: {type?.Name}");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogAsync($"[Extension] Failed to load Prompt Injector from {promptDll}: {ex.ToString()}");
                }
            }

            if (!string.IsNullOrEmpty(interceptorDll) && File.Exists(interceptorDll))
            {
                try
                {
                    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(interceptorDll));

                    // Find implementation of ISqlInterceptor
                    var type = assembly.GetTypes().FirstOrDefault(t => typeof(ISqlInterceptor).IsAssignableFrom(t) && !t.IsInterface);
                    if (type != null)
                    {
                        services.AddScoped(typeof(ISqlInterceptor), type);
                        logger.LogAsync($"[Extension] Registered SQL Interceptor: {type.Name}");
                        //Console.WriteLine($"[Extension] Registered SQL Interceptor: {type.Name}");
                    } else
                    {
                        logger.LogAsync($"[Extension] Failed to register SQL Interceptor: {type?.Name}");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogAsync($"[Extension] Failed to load SQL Interceptor from {interceptorDll}: {ex.ToString()}");
                }
            }
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

            services.AddSingleton<IWhatsAppLogger, WhatsAppLogger>();

            services.Configure<WhatsAppSettings>(config.GetSection("WhatsAppSettings"));
            services.Configure<DatabaseSettings>(config.GetSection("DatabaseSettings"));
            services.Configure<OpenAiSettings>(config.GetSection("OpenAiSettings"));
            services.Configure<RoleSettings>(config.GetSection("RoleSettings"));
            services.AddDynamicExtensions(config);

            services.AddPlugin(config, metadata);

            
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<DatabaseQueryPlugin>();

            services.AddKernel(metadata);

        }

        public static void AddPlugin(this IServiceCollection services, IConfiguration config, PluginMetadata metadata)
        {
            using var tempProvider = services.BuildServiceProvider();
            var logger = tempProvider.GetService<IWhatsAppLogger>();
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
                        // 1. Force use of Host's version for ANY Infrastructure DLLs
                        if (assemblyName.Name.StartsWith("System.") ||
                            assemblyName.Name.StartsWith("Microsoft.") ||
                            assemblyName.Name == "Newtonsoft.Json")
                        {
                            return null; // Returning null tells .NET "Look in the Host's bin folder instead"
                        }

                        // 2. Check if already loaded (The Bridge)
                        var alreadyLoaded = AppDomain.CurrentDomain.GetAssemblies()
                            .FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
                        if (alreadyLoaded != null) return alreadyLoaded;

                        // 3. Only load your custom logic from the Plugins folder
                        string path = Path.Combine(pluginDir!, $"{assemblyName.Name}.dll");
                        if (File.Exists(path))
                        {
                            return context.LoadFromAssemblyPath(path);
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

        public static void AddKernel(this IServiceCollection services, PluginMetadata metadata)
        {
            services.AddScoped<Kernel>(sp =>
            {
                // Use a unique ID to track this specific resolution in the console
                var requestId = Guid.NewGuid().ToString().Substring(0, 4);
                Console.WriteLine($"[WhatsAppToDB] [{requestId}] Building Kernel...");

                var aiSettings = sp.GetRequiredService<IOptions<OpenAiSettings>>().Value;
                var kernelBuilder = Kernel.CreateBuilder();

                kernelBuilder.AddOpenAIChatCompletion(aiSettings.Model, aiSettings.ApiKey);

                var dbPlugin = sp.GetRequiredService<DatabaseQueryPlugin>();
                kernelBuilder.Plugins.AddFromObject(dbPlugin);

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
