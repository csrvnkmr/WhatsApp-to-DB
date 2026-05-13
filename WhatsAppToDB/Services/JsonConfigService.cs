using NPOI.HSSF.Model;
using NPOI.POIFS.Storage;
using System.Runtime.CompilerServices;
using System.Text.Json;
using WhatsAppToDB.Database;
using WhatsAppToDB.DbProviders.SchemaModels;
using WhatsAppToDB.LlmProviders;
using WhatsAppToDB.Models;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.Services
{
    public class JsonConfigService
    {
        private readonly string _configRoot;
        private readonly ILogger _logger;

        private readonly JsonSerializerOptions _options =
            new()
            {
                PropertyNameCaseInsensitive = true
            };

        public JsonConfigService(
            IConfiguration config, ILogger logger)
        {
            _configRoot =
                config.GetValue<string>(
                    "ConfigRootFolder")!;
            _logger = logger;
        }

        // ============================================
        // GLOBAL FILE
        // ============================================

        public T Load<T>(
            string fileName, [CallerFilePath] string callerfile="", [CallerMemberName] string callermember="", [CallerLineNumber] int callerlinenum=-1)
        {
            try
            {
                var path =
                    Path.Combine(
                        _configRoot,
                        fileName);

                if (!File.Exists(path))
                {
                    throw new Exception(
                        $"Config file not found: {path}");
                }

                var json = File.ReadAllText(path);

                return JsonSerializer.Deserialize<T>(json, _options)!;
            }
            catch(Exception ex)
            {
                _logger.LogInfo($"Exception in {nameof(Load)} in {nameof(JsonConfigService)} {ex}, {callerfile}.{callermember} at {callerlinenum}");
                throw;
            }
        }

        // ============================================
        // DATABASE FILE
        // ============================================

        public T LoadDatabaseConfig<T>(
            string database,
            string fileName, [CallerFilePath] string callerfile = "", [CallerMemberName] string callermember = "", [CallerLineNumber] int callerlinenum = -1)
        {
            try
            {
                var path =
                    Path.Combine(
                        _configRoot,
                        "databases",
                        database,
                        fileName);

                if (!File.Exists(path))
                {
                    throw new Exception(
                        $"Config file not found: {path}");
                }

                var json =
                    File.ReadAllText(path);

                return JsonSerializer.Deserialize<T>(
                    json,
                    _options)!;

            }
            catch (Exception ex)
            {
                _logger.LogInfo($"Exception in {nameof(LoadDatabaseConfig)} in {nameof(JsonConfigService)} {ex}, {callerfile}.{callermember} at {callerlinenum}");
                throw;
            }
        }

        public List<DatabaseConfig> GetDatabaseConfigs()
        {
            var lstConfigs = Load<List<DatabaseConfig>>(Constants.ConfigFiles.Databases);
            return lstConfigs;
        }
        public List<LlmConfig> GetLlmConfigs()
        {
            var lstConfigs = Load<List<LlmConfig>>(Constants.ConfigFiles.Llms);
            return lstConfigs;
        }
        public List<LoginUser> GetUsers()
        {
            var lstUsers = Load<List<LoginUser>>(Constants.ConfigFiles.Users);
            return lstUsers;
        }
        public List<DatabaseTable> GetTables(string database)
        {
            var lstTables = LoadDatabaseConfig <List<DatabaseTable>>(database, Constants.ConfigFiles.Tables);
            return lstTables;
        }
        public List<DatabaseTableJoin> GetTableJoins(string database)
        {
            var lstTableJoins = LoadDatabaseConfig <List<DatabaseTableJoin>>(database, Constants.ConfigFiles.TableJoins);
            return lstTableJoins;
        }
        public List<Module> GetModules(string database)
        {
            var lstModules = LoadDatabaseConfig<List<Module>>(database, Constants.ConfigFiles.Modules);
            return lstModules;
        }

        public List<PluginSettings> GetPlugins(string database)
        {
            var lstPlugins = LoadDatabaseConfig<List<PluginSettings>>(database, Constants.ConfigFiles.Plugins);
            return lstPlugins;
        }

        public string GetPrompt(string database)
        {
            var lstPrompts = LoadDatabaseConfig<List<DatabasePrompt>>(database, Constants.ConfigFiles.SystemPrompt);
            var finalPrompt = "";
            if (lstPrompts != null && lstPrompts.Count > 0)
            {
                lstPrompts.ForEach(x=> finalPrompt += $"{x.systemprompt}\n\n");
            }
            return finalPrompt;
        }

        public List<Role> GetRoles(string database)
        {
            var lstRoles = LoadDatabaseConfig<List<Role>>(database, Constants.ConfigFiles.Roles);
            return lstRoles;
        }
    }
}
