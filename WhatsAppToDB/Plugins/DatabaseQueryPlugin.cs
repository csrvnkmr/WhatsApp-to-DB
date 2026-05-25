using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Models;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;
using WhatsAppToDB.Database;
using WhatsAppToDB.Models;
using WhatsAppToDB.Services;
using WhatsAppToDB.Plugins;

namespace WhatsAppToDB.Plugin
{
   
    public class DatabaseQueryPlugin
    {
        private readonly ILogger _logger;
        

        
        private readonly AiRequestContext _ctx;

        private readonly IModulePrompt? _promptExtension;
        //private readonly DatabaseSettings _dbSettings;
        private readonly DatabaseContextService _databaseContextService;
        private readonly FolderUtils _folderUtils;
        private readonly JsonConfigService _jsonConfigService;
        private readonly PluginLoaderService _pluginLoaderService;


        public DatabaseQueryPlugin(AiRequestContext ctx,
            IModulePrompt? promptExtension = null,   
            ILogger? logger = null, DatabaseContextService databaseContextService = null,
            FolderUtils folderUtils = null, 
            JsonConfigService jsonConfigService = null,
            IConfiguration configuration = null,
            PluginLoaderService pluginLoaderService = null
            )
        {
            _promptExtension = promptExtension;
            _logger = logger ?? new AppLogger();
            //_dbSettings = dbSettings.Value;
            _ctx = ctx;
            _databaseContextService = databaseContextService;
            _folderUtils = folderUtils;
            _jsonConfigService = jsonConfigService;
            _pluginLoaderService = pluginLoaderService;
            //_folderUtils = folderUtils ?? new FolderUtils(configuration ?? new ConfigurationBuilder().AddInMemoryCollection(new[] { new KeyValuePair<string, string>("DataFolder", "Data") }).Build());
        }

        [KernelFunction]
        [Description("Executes a READ-ONLY SQL SELECT query against the database.")]
        public async Task<string> ExecuteSql([Description("The T-SQL SELECT statement")] string sql, Kernel kernel)
        {
            // Add a check here to ensure the query starts with "SELECT"
            if (!sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                return "Error: Only SELECT queries are permitted.";

            string userName ="", userQuestion="", moduleName="";

            userName = _ctx.UserName;
            userQuestion = _ctx.UserQuestion;

            //if (kernel.Data.ContainsKey("WhatsAppNumber")) waNumber = kernel.Data["WhatsAppNumber"]?.ToString();
            //if (kernel.Data.ContainsKey("UserQuestion")) userQuestion = kernel.Data["UserQuestion"]?.ToString();            
            if (kernel.Data.ContainsKey("LastRequestedModule"))
            {
                moduleName = kernel.Data["LastRequestedModule"]?.ToString();
                _ctx.ModuleName= moduleName;
            }

            var identity = _ctx.Identity; // kernel.Data["UserIdentity"] as IdentityContext;

            //var currentConnectionString = _dbSettings.ConnectionString;
            var currentConnectionString = _databaseContextService.GetCurrentConfig().ConnectionString;
            if (identity!=null && !string.IsNullOrWhiteSpace(identity.ConnectionString)) {
                currentConnectionString = identity.ConnectionString;
            }
            try
            {
                await _logger.LogInfoAsync($"[DatabaseQueryPlugin] UserName: {userName}, Question: {userQuestion}, module {moduleName}");
                var dbName = _databaseContextService.GetCurrentDatabaseName();

                var extn = _jsonConfigService.GetExtension(dbName);
                if (extn != null)
                {
                    var sqlInterceptor = _pluginLoaderService.CreatePluginInstance(extn.SqlInterceptorDLL, extn.SqlInterceptorClass);
                    if (sqlInterceptor != null && sqlInterceptor is ISqlInterceptor interceptor)
                    {
                        sql = await interceptor.OnBeforeExecuteAsync(identity, userName, sql);
                    }
                }

                /*
                if (_sqlExtension != null)
                {
                    sql = await _sqlExtension.OnBeforeExecuteAsync(identity, userName?.ToString(), sql);
                }
                */

                await _logger.LogDebugAsync($"[DatabaseQueryPlugin] SQL : {sql}");
                kernel.Data["LastExecutedSql"] = sql;
                var provider = _databaseContextService.GetProvider();
                using IDbConnection db = _databaseContextService.CreateConnection(currentConnectionString);
                // Ensure the connection is open before setting session context, as Dapper relies on it for the session state to be applied correctly.
                db.Open(); 
                if (identity != null && identity.IsAdministrator())
                {
                    await provider.SetSessionContext(db, identity);
                }
                 _logger.LogDebug($"[DatabaseQueryPlugin] {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Connection opened. Executing query...");   
                // Use Dapper to get dynamic results (perfect for unpredictable SAP tables)
                var results = await db.QueryAsync(sql);                
                _logger.LogDebug($"[DatabaseQueryPlugin] {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Executed query...");
                //if (!results.Any()) return "[]";
                var jsonresult = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
                var qrpath = _folderUtils.GetQueryResultFile();
                if (qrpath != null)
                {
                    File.WriteAllText(qrpath, jsonresult);
                }

                _ctx.LastExecutedSql = sql;
                _ctx.ShowSql = true;
                _ctx.ShowChart = false;
                _ctx.ResultRowCount = results.Count();
                _ctx.ShowData = _ctx.ResultRowCount >0 ;
                _ctx.DataFileName = qrpath;
                if (results.Count() > 0)
                {
                    // If the result set is large, we can choose to return only the first 50 rows to the AI to prevent overwhelming it,
                    // while still saving the full results to a file for later retrieval.
                    jsonresult = JsonSerializer.Serialize(results.Take(50), new JsonSerializerOptions { WriteIndented = true });
                }
                // Return raw JSON to the AI
                return jsonresult;
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    await _logger.LogErrorAsync("[DatabaseQueryPlugin] Error in Execute SQL", ex);
                }
                else
                {
                    await new AppLogger().LogErrorAsync($"[DatabaseQueryPlugin] Error in Execute SQL", ex);
                }


                    // Give the error to the AI so it can try to fix the SQL
                    return $"Database Error: {ex.Message}. Check your table/column names.";
            }
            
        }

        
    }
}
