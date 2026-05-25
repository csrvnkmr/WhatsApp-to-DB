using Microsoft.Graph.Models;
using Microsoft.Extensions.Configuration;
using WhatsAppToDB.Services;

namespace WhatsAppToDB.Data
{
    public class FolderUtils
    {
        private readonly string _dataFolder;
        private readonly JsonConfigService _jsonConfig;
        private readonly ILogger _logger;
        
        public FolderUtils(IConfiguration configuration, JsonConfigService jsonConfig)
        {
            _jsonConfig = jsonConfig;
            var defaultFolders = _jsonConfig.GetDefaultFolders();
            if (defaultFolders != null && !string.IsNullOrWhiteSpace(defaultFolders.ChatHistoryFolder))
            {
                _dataFolder = defaultFolders.ChatHistoryFolder;
            } 
            else 
            {
                 _logger.LogInfo("ChatHistoryFolder not found in default folders config, falling back to DataFolder from configuration or 'Data'");                 
                _dataFolder = Path.Combine(AppContext.BaseDirectory, "ChatData");
            }
        }

        public string GetDataFolder()
        {
            return Path.Combine(AppContext.BaseDirectory, _dataFolder);
        }

        public string GetSqliteDBPath()
        {
            var dbpath = GetDataFolder();
            dbpath = Path.Combine(dbpath, "chathistory.db");
            return dbpath;
        }

        public string GetQueryResultFolder()
        {
            var qrfolder = GetDataFolder();
            qrfolder = Path.Combine(qrfolder, "QueryResults");
            if (!Directory.Exists(qrfolder))
            {
                Directory.CreateDirectory(qrfolder);
            }
            return qrfolder;
        }

        public string GetQueryResultFile()
        {
            var qrfolder = GetQueryResultFolder();
            var qrpath = Path.Combine(qrfolder, System.Guid.NewGuid().ToString()+".json");
            return qrpath;
        }

    }
}
