using NPOI.HSSF.Model;
using NPOI.POIFS.Storage;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using WhatsAppToDB.Abstractions;
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
        private readonly FieldEncryptionService _encryption;

        private readonly JsonSerializerOptions _options =
            new()
            {
                PropertyNameCaseInsensitive = true
            };

        public JsonConfigService(
            IConfiguration config, ILogger logger)
        {
            _configRoot = config.GetValue<string>("ConfigRootFolder")!;
            if (string.IsNullOrWhiteSpace(_configRoot))
            {
                _configRoot = Path.Combine(AppContext.BaseDirectory, "Config");
            }
            if (!Directory.Exists(_configRoot))
                Directory.CreateDirectory(_configRoot);

            _logger = logger;
              _encryption = new FieldEncryptionService();
        }
        

        /// <summary>
        /// Reads a JSON file and decrypts any ENC:... values whose field
        /// names are flagged as sensitive in the supplied metadata.
        /// Returns the raw JsonNode so AdminController can send it as-is.
        /// </summary>
        public JsonNode? LoadAndDecrypt(string filePath, IEnumerable<string> sensitiveFields)
        {
            if (!File.Exists(filePath))
                return JsonNode.Parse("[]");
 
            var json = File.ReadAllText(filePath);
            var node = JsonNode.Parse(json);
 
            DecryptNode(node, sensitiveFields.ToHashSet(StringComparer.OrdinalIgnoreCase));
            return node;
        }
 
        /// <summary>
        /// Encrypts sensitive fields in the supplied JSON, then writes to filePath.
        /// </summary>
        public void EncryptAndSave(
            string filePath,
            string rawJson,
            IEnumerable<string> sensitiveFields,
            JsonSerializerOptions? prettyOptions = null)
        {
            var node   = JsonNode.Parse(rawJson)
                         ?? throw new Exception("Invalid JSON body");
            var fields = sensitiveFields.ToHashSet(StringComparer.OrdinalIgnoreCase);
 
            EncryptNode(node, fields);
 
            var pretty = node.ToJsonString(prettyOptions ?? new JsonSerializerOptions { WriteIndented = true });
 
            var folder = Path.GetDirectoryName(filePath)!;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
 
            File.WriteAllText(filePath, pretty);
        }


        // ============================================================
        // PRIVATE HELPERS
        // ============================================================
 
        private void DecryptNode(JsonNode? node, HashSet<string> fields)
        {
            if (node is JsonArray arr)
                foreach (var item in arr)
                    DecryptNode(item, fields);
 
            else if (node is JsonObject obj)
                foreach (var field in fields)
                    if (obj[field]?.GetValue<string>() is string val && _encryption.IsEncrypted(val))
                        obj[field] = _encryption.Decrypt(val);
        }
 
        private void EncryptNode(JsonNode? node, HashSet<string> fields)
        {
            if (node is JsonArray arr)
                foreach (var item in arr)
                    EncryptNode(item, fields);
 
            else if (node is JsonObject obj)
                foreach (var field in fields)
                    if (obj[field]?.GetValue<string>() is string val && !_encryption.IsEncrypted(val))
                        obj[field] = _encryption.Encrypt(val);
        }

        /// <summary>
        /// Reads entity's metadata file and returns field names marked as sensitive.
        /// </summary>
        public List<string> GetSensitiveFields(string entity)
        {
            var metadataRoot = Path.Combine(_configRoot, "metadata");
            var metaFile = Path.Combine(metadataRoot, $"{entity}.json");
 
            if (!File.Exists(metaFile))
                return new List<string>();
 
            try
            {
                var json = File.ReadAllText(metaFile);
                var metaNode = JsonNode.Parse(json);
 
                var fields = metaNode?["fields"]?.AsArray();
                if (fields == null) return new List<string>();
 
                return fields
                    .Where(f => f?["sensitive"]?.GetValue<bool>() == true)
                    .Select(f => f?["name"]?.GetValue<string>() ?? "")
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList();
            }
            catch
            {
                // If metadata can't be parsed, treat nothing as sensitive
                return new List<string>();
            }
        }

        /// <summary>
        /// Loads and decrypts a global config file using metadata-driven sensitive fields.
        /// </summary>
        public T LoadAndDecryptGlobal<T>(string fileName) where T : class
        {
            var filePath = Path.Combine(_configRoot, fileName);
            var entity = Path.GetFileNameWithoutExtension(fileName);
            var sensitiveFields = GetSensitiveFields(entity);
            
            var node = LoadAndDecrypt(filePath, sensitiveFields);
            return node?.Deserialize<T>(_options) ?? null!;
        }

        /// <summary>
        /// Loads and decrypts a database-specific config file using metadata-driven sensitive fields.
        /// </summary>
        public T LoadDatabaseConfigAndDecrypt<T>(string database, string fileName) where T : class
        {
            var filePath = Path.Combine(_configRoot, "databases", database, fileName);
            var entity = Path.GetFileNameWithoutExtension(fileName);
            var sensitiveFields = GetSensitiveFields(entity);
            
            var node = LoadAndDecrypt(filePath, sensitiveFields);
            return node?.Deserialize<T>(_options) ?? null!;
        }


        public List<DatabaseConfig> GetDatabaseConfigs()
        {
            return LoadAndDecryptGlobal<List<DatabaseConfig>>(Constants.ConfigFiles.Databases);
        }
        public List<LlmConfig> GetLlmConfigs()
        {
            return LoadAndDecryptGlobal<List<LlmConfig>>(Constants.ConfigFiles.Llms);
        }


        public (LlmConfig? Config, Dictionary<string, object?> ExtraProperties) GetLlmConfig(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
            {
                return (null, new Dictionary<string, object?>());
            }

            // 1. Load the strongly typed collection using your existing method
            List<LlmConfig> configs = GetLlmConfigs();
            if (configs == null || !configs.Any())
            {
                return (null, new Dictionary<string, object?>());
            }

            // 2. Locate the specific item and its index in the list
            var targetIndex = configs.FindIndex(c => 
                c.Provider != null && 
                c.Provider.Equals(providerName, StringComparison.OrdinalIgnoreCase));

            if (targetIndex == -1)
            {
                // Provider matching the string was not found
                return (null, new Dictionary<string, object?>());
            }

            var matchedConfig = configs[targetIndex];

            // 3. Fetch the raw payload text directly from your storage provider mapping
            string rawJson = ""; 
            //_storageProvider.ReadGlobalConfigRaw(Path.GetFileNameWithoutExtension(Constants.ConfigFiles.Llms));

            // 4. Extract unmapped parameters exclusively for this item's array position
            var extraProperties = JsonExtensionUtils.GetUnmappedPropertiesForIndex(matchedConfig, rawJson, targetIndex);

            return (matchedConfig, extraProperties);
        }

        public List<LoginUser> GetUsers()
        {
            return LoadAndDecryptGlobal<List<LoginUser>>(Constants.ConfigFiles.Users);
        }
        public LoginUser? GetUser(string userName)
        {
            var users = LoadAndDecryptGlobal<List<LoginUser>>(Constants.ConfigFiles.Users);            
            return users.FirstOrDefault(u => u.Username.Equals(userName, StringComparison.OrdinalIgnoreCase));
        }
        public LoginUser? GetUserByWhatsAppNumber(string whatsAppNumber)
        {
            var users = LoadAndDecryptGlobal<List<LoginUser>>(Constants.ConfigFiles.Users);    

            return users.FirstOrDefault(
                u => (u.WhatsAppNumber != null && 
                (u.WhatsAppNumber.Equals(whatsAppNumber, StringComparison.OrdinalIgnoreCase)
                || ("+" + u.WhatsAppNumber).Equals(whatsAppNumber, StringComparison.OrdinalIgnoreCase)
                || (u.WhatsAppNumber).Equals("+" + whatsAppNumber, StringComparison.OrdinalIgnoreCase)
                )));
        }

        public List<WhatsAppProfile>? GetWhatsAppProfiles()
        {
            return LoadAndDecryptGlobal<List<WhatsAppProfile>>(Constants.ConfigFiles.WhatsAppProfiles);
        }

        public WhatsAppProfile? GetWhatsAppProfile(string profileId)
        {
            var profiles = LoadAndDecryptGlobal<List<WhatsAppProfile>>(Constants.ConfigFiles.WhatsAppProfiles);
            return profiles.FirstOrDefault(p => p.ProfileId.Equals(profileId, StringComparison.OrdinalIgnoreCase));
        }

        public WhatsAppProfile? GetWhatsAppProfileFromPhoneId(string phoneId)
        {
            var profiles = LoadAndDecryptGlobal<List<WhatsAppProfile>>(Constants.ConfigFiles.WhatsAppProfiles);
            return profiles.FirstOrDefault(p => p.PhoneId.Equals(phoneId, StringComparison.OrdinalIgnoreCase));
        }

        public List<DatabaseTable> GetTables(string database)
        {
            return LoadDatabaseConfigAndDecrypt<List<DatabaseTable>>(database, Constants.ConfigFiles.Tables);
        }
        public List<DatabaseTableJoin> GetTableJoins(string database)
        {
            return LoadDatabaseConfigAndDecrypt<List<DatabaseTableJoin>>(database, Constants.ConfigFiles.TableJoins);
        }
        public List<Module> GetModules(string database)
        {
            return LoadDatabaseConfigAndDecrypt<List<Module>>(database, Constants.ConfigFiles.Modules);
        }

        public List<PluginSettings> GetPlugins(string database)
        {
            return LoadDatabaseConfigAndDecrypt<List<PluginSettings>>(database, Constants.ConfigFiles.Plugins);
        }

        public VectorDBSettings GetVectorDBSettings(string database)
        {
            var vectorDBSettingsList = 
                LoadDatabaseConfigAndDecrypt<List<VectorDBSettings>>(database, Constants.ConfigFiles.VectordbSettings);
            var firstvalue= vectorDBSettingsList?.FirstOrDefault();
            if (firstvalue != null)
            {
                firstvalue.VectorDBProviderSettings = firstvalue.VectorDBProviderSettings ?? new VectorDBProviderSettings();
                firstvalue.VectorDBProviderSettings.Database = database;
                firstvalue.VectorDBProviderSettings.TenantName = "database";
            }
            return firstvalue;
        }

        public List<VectorConfiguration> GetVectorConfigurations(string database)
        {
            var VectorConfigurationList = 
                LoadDatabaseConfigAndDecrypt<List<VectorConfiguration>>(database, 
                Constants.ConfigFiles.VectorConfigurations);
            return VectorConfigurationList ?? new List<VectorConfiguration>();
        }

        public List<VectorSyncConfig> GetVectorSyncConfigurations(string database)
        {
            var VectorConfigurationList = 
                LoadDatabaseConfigAndDecrypt<List<VectorSyncConfig>>(database, 
                Constants.ConfigFiles.VectorConfigurations);
            return VectorConfigurationList ?? new List<VectorSyncConfig>();
        }

        public string GetPrompt(string database)
        {
            var lstPrompts = LoadDatabaseConfigAndDecrypt<List<DatabasePrompt>>(database, Constants.ConfigFiles.SystemPrompt);
            var finalPrompt = "";
            if (lstPrompts != null && lstPrompts.Count > 0)
            {
                lstPrompts.ForEach(x=> finalPrompt += $"{x.systemprompt}\n\n");
            }
            return finalPrompt;
        }

        public List<ModuleQuery> GetModuleQueries(string database, string moduleName)
        {
            var lstQueries = LoadDatabaseConfigAndDecrypt<List<ModuleQuery>>(database, Constants.ConfigFiles.FewShotQueries);
            var finalQueries = new List<ModuleQuery>();
            if (lstQueries != null && lstQueries.Count > 0)
            {
                lstQueries.ForEach(x => {if (x.Module == moduleName) finalQueries.Add(x);});
            }
            return finalQueries;
        }
    
        public List<Role> GetRoles(string database)
        {
            return LoadDatabaseConfigAndDecrypt<List<Role>>(database, Constants.ConfigFiles.Roles);
        }

        public DefaultSettings GetDefaultSettings()
        {
            var settings = LoadAndDecryptGlobal<List<DefaultSettings>>(Constants.ConfigFiles.DefaultSettings);
            if (settings != null && settings.Count > 0)
            {
                return settings[0];
            }            
            settings = new List<DefaultSettings>() { new DefaultSettings() };
            return settings[0];
        }

        public DefaultFolders GetDefaultFolders()
        {
            var folders = LoadAndDecryptGlobal<List<DefaultFolders>>(Constants.ConfigFiles.DefaultFolders);
            if (folders != null && folders.Count > 0)
            {
                return folders[0];
            }            
            folders = new List<DefaultFolders>() { new DefaultFolders() };
            return folders[0];
        }

        public MailSettings GetMailSettings(string database)
        {
            var mailSettings = LoadDatabaseConfigAndDecrypt<List<MailSettings>>(database, Constants.ConfigFiles.MailSettings);
            if (mailSettings != null && mailSettings.Count > 0)
            {
                return mailSettings[0];
            }            
            return null;
        }

        public Extension GetExtension(string database)
        {
            var extension = LoadDatabaseConfigAndDecrypt<List<Extension>>(database, Constants.ConfigFiles.Extensions);
            if (extension != null && extension.Count > 0)
            {
                return extension[0];
            }            
            return null;
        }

    }
}
