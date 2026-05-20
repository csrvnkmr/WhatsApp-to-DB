    using global::WhatsAppToDB.Settings;
    using System.Reflection;
    using System.Runtime.Loader;
    

    namespace WhatsAppToDB.Plugins
    {
        public class PluginLoaderService
        {
            private readonly ILogger _logger;

            // ============================================
            // ASSEMBLY CACHE
            // key = full assembly path
            // ============================================

            private readonly Dictionary<string, Assembly>
                _assemblies = new(
                    StringComparer.OrdinalIgnoreCase);

            private readonly object _lock = new();

            public PluginLoaderService(ILogger logger)
            {
                _logger = logger;
            }

            // ============================================
            // CREATE PLUGIN INSTANCE
            // ============================================

            public object? CreatePluginInstance(string assemblyPath, string fullyQualifiedClassName)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(assemblyPath) ||
                        string.IsNullOrWhiteSpace(fullyQualifiedClassName))
                    {
                        _logger.LogError(
                            $"Invalid plugin parameters. AssemblyPath and FullyQualifiedClassName are required.");

                        return null;
                    }
                    var assembly =
                        GetAssembly(
                            assemblyPath);

                    if (assembly == null)
                        return null;

                    // ====================================
                    // GET TYPE
                    // ====================================

                    var pluginType =
                        assembly.GetType(
                            fullyQualifiedClassName);

                    if (pluginType == null)
                    {
                        _logger.LogError(
                            $"Plugin type not found: {fullyQualifiedClassName}");

                        return null;
                    }

                    // ====================================
                    // CREATE INSTANCE
                    // ====================================

                    var instance =
                        Activator.CreateInstance(
                            pluginType);

                    if (instance == null)
                    {
                        _logger.LogError(
                            $"Failed to create plugin instance: {fullyQualifiedClassName}");

                        return null;
                    }

                    _logger.LogInfo(
                        $"Plugin loaded: {fullyQualifiedClassName}");

                    return instance;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        $"Failed loading plugin {fullyQualifiedClassName}",
                        ex);

                    return null;
                }
            }


            public object? CreatePluginInstance(PluginSettings pluginSettings)
            {

                if (pluginSettings == null ||
                    (string.IsNullOrWhiteSpace(pluginSettings.AssemblyPath) ||
                    string.IsNullOrWhiteSpace(pluginSettings.PluginClassName)))
                {
                    _logger.LogError(
                        $"Plugin settings incomplete for plugin: {pluginSettings.Name}");

                    return null;
                }
                return CreatePluginInstance(pluginSettings.AssemblyPath, pluginSettings.PluginClassName);

            }

            // ============================================
            // GET ASSEMBLY (WITH CACHE)
            // ============================================

            private Assembly? GetAssembly(
                string assemblyPath)
            {
                try
                {
                    assemblyPath =
                        Path.GetFullPath(assemblyPath);

                    lock (_lock)
                    {
                        // ================================
                        // CACHE HIT
                        // ================================

                        if (_assemblies.TryGetValue(
                            assemblyPath,
                            out var cachedAssembly))
                        {
                            return cachedAssembly;
                        }

                        // ================================
                        // FILE EXISTS?
                        // ================================

                        if (!File.Exists(assemblyPath))
                        {
                            _logger.LogError(
                                $"Plugin assembly not found: {assemblyPath}");

                            return null;
                        }

                        // ================================
                        // RESOLVE DEPENDENCIES
                        // ================================

                        var pluginDir =
                            Path.GetDirectoryName(
                                assemblyPath)!;

                        AssemblyLoadContext.Default.Resolving +=
                            (context, assemblyName) =>
                            {
                                try
                                {
                                    // ----------------------------
                                    // already loaded?
                                    // ----------------------------

                                    var alreadyLoaded =
                                        AppDomain.CurrentDomain
                                            .GetAssemblies()
                                            .FirstOrDefault(a =>
                                                a.GetName().Name ==
                                                assemblyName.Name);

                                    if (alreadyLoaded != null)
                                        return alreadyLoaded;

                                    // ----------------------------
                                    // plugin folder
                                    // ----------------------------

                                    var dependencyPath =
                                        Path.Combine(
                                            pluginDir,
                                            $"{assemblyName.Name}.dll");

                                    if (File.Exists(dependencyPath))
                                    {
                                        return context
                                            .LoadFromAssemblyPath(
                                                dependencyPath);
                                    }

                                    return null;
                                }
                                catch
                                {
                                    return null;
                                }
                            };

                        // ================================
                        // LOAD ASSEMBLY
                        // ================================

                        var assembly =
                            AssemblyLoadContext.Default
                                .LoadFromAssemblyPath(
                                    assemblyPath);

                        // ================================
                        // CACHE
                        // ================================

                        _assemblies[assemblyPath] =
                            assembly;

                        _logger.LogInfo(
                            $"Assembly cached: {assemblyPath}");

                        return assembly;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        $"Failed loading assembly: {assemblyPath}",
                        ex);
                     
                    return null;
                }
            }
        }
    }
