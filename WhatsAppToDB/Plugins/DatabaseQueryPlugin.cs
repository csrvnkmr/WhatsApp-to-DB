using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
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
using WhatsAppToDB.Models;

namespace WhatsAppToDB.Plugin
{
   
    public class DatabaseQueryPlugin
    {
        private readonly ILogger _logger;
        

        
        private readonly AiRequestContext _ctx;

        private readonly IModulePrompt? _promptExtension;
        private readonly ISqlInterceptor? _sqlExtension;
        private readonly DatabaseSettings _dbSettings;

        public DatabaseQueryPlugin(IOptions<DatabaseSettings> dbSettings,
            AiRequestContext ctx,
            IModulePrompt? promptExtension = null,   
            ISqlInterceptor? sqlExtension = null,
            ILogger? logger = null
            )    
        {
            _promptExtension = promptExtension;
            _sqlExtension = sqlExtension;
            _logger = logger ?? new AppLogger();
            _dbSettings = dbSettings.Value;
            _ctx = ctx;
        }

        
        public async Task SetDatabaseSessionAsync(IDbConnection conn, IdentityContext identity)
        {
            // Determine the key: Mapped Key -> Fallback to Role
            string contextKey = identity.GetActiveContextKey();

            // In SQL Server, we set the session context
            string sql = "EXEC sp_set_session_context @Key, @Value, @read_only = 1;";

            await conn.ExecuteAsync(sql, new
            {
                Key = contextKey,
                Value = identity.InternalUserId
            });
        }

        [KernelFunction]
        [Description("Executes a READ-ONLY SQL SELECT query against the database.")]
        public async Task<string> ExecuteSql([Description("The T-SQL SELECT statement")] string sql, Kernel kernel)
        {
            // Add a check here to ensure the query starts with "SELECT"
            if (!sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                return "Error: Only SELECT queries are permitted.";

            string waNumber ="", userQuestion="", moduleName="";

            waNumber = _ctx.WhatsAppNumber;
            userQuestion = _ctx.UserQuestion;
            

            //if (kernel.Data.ContainsKey("WhatsAppNumber")) waNumber = kernel.Data["WhatsAppNumber"]?.ToString();
            //if (kernel.Data.ContainsKey("UserQuestion")) userQuestion = kernel.Data["UserQuestion"]?.ToString();            
            if (kernel.Data.ContainsKey("LastRequestedModule")) moduleName = kernel.Data["LastRequestedModule"]?.ToString();

            var identity = _ctx.Identity; // kernel.Data["UserIdentity"] as IdentityContext;
            var currentConnectionString = _dbSettings.ConnectionString;
            if (identity!=null && !string.IsNullOrWhiteSpace(identity.ConnectionString)) {
                currentConnectionString = identity.ConnectionString;
            }
            try
            {
                Console.WriteLine($"PhoneNumber: {waNumber}, Question: {userQuestion}, module {moduleName}");
                if (_sqlExtension != null)
                {
                    sql = await _sqlExtension.OnBeforeExecuteAsync(identity, waNumber?.ToString(), sql);
                }
                Console.WriteLine($"[SAP EXECUTION]: {sql}");
                kernel.Data["LastExecutedSql"] = sql;

                using IDbConnection db = DbConnectionFactory.CreateConnection(currentConnectionString);
                // Ensure the connection is open before setting session context, as Dapper relies on it for the session state to be applied correctly.
                db.Open(); 
                if (identity != null && identity.Role?.ToLower()!= "admin")
                {
                    await SetDatabaseSessionAsync(db, identity);
                }
                    
                // Use Dapper to get dynamic results (perfect for unpredictable SAP tables)
                var results = await db.QueryAsync(sql);                

                //if (!results.Any()) return "[]";
                var jsonresult = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
                var qrpath = FolderUtls.GetQueryResultFile();
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
                // Give the error to the AI so it can try to fix the SQL
                return $"Database Error: {ex.Message}. Check your table/column names.";
            }
            
        }

        
    }
}
