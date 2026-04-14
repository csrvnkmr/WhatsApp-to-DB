using System.Runtime.Loader;
using WhatsAppToDB.Abstractions;

namespace WhatsAppToDB
{
    public static class ServiceCollectionExtension
    {
        public static void AddDynamicExtensions(this IServiceCollection services, IConfiguration config)
        {
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
                        Console.WriteLine($"[Extension] Registered Prompt Injector: {type.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Extension] Failed to load Prompt Injector from {promptDll}: {ex.ToString()}");
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
                        Console.WriteLine($"[Extension] Registered SQL Interceptor: {type.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Extension] Failed to load SQL Interceptor from {interceptorDll}: {ex.ToString()}");
                }
            }
        }


    }
}
