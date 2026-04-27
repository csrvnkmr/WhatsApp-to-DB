using Microsoft.Graph.Models;

namespace WhatsAppToDB.Data
{
    public class FolderUtls
    {
        public static string GetDataFolder()
        {
            var datafolder = Path.Combine(AppContext.BaseDirectory, "Data");
            return datafolder;
        }

        public static string GetSqliteDBPath()
        {
            var dbpath = GetDataFolder();
            dbpath = Path.Combine(dbpath, "chathistory.db");
            return dbpath;
        }

        public static string GetQueryResultFolder()
        {
            var qrfolder = GetDataFolder();
            qrfolder = Path.Combine(qrfolder, "QueryResults");
            if (!Directory.Exists(qrfolder))
            {
                Directory.CreateDirectory(qrfolder);
            }
            return qrfolder;
        }

        public static string GetQueryResultFile()
        {
            var qrfolder = GetQueryResultFolder();
            var qrpath = Path.Combine(qrfolder, System.Guid.NewGuid().ToString()+".json");
            return qrpath;
        }

    }
}
