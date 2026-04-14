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

namespace WhatsAppToDB
{
   
    public class DatabaseQueryPlugin
    {

        private readonly string _connectionString;
        private readonly SchemaService _schemaService;

        private readonly IModulePrompt? _promptExtension;
        private readonly ISqlInterceptor? _sqlExtension;

        public DatabaseQueryPlugin(IOptions<DatabaseSettings> dbSettings,
            IModulePrompt? promptExtension = null,   
            ISqlInterceptor? sqlExtension = null)    
        {
            _connectionString = dbSettings.Value.ConnectionString;
            _schemaService = new SchemaService(dbSettings.Value.SchemaDefinitionFile);
            _promptExtension = promptExtension;
            _sqlExtension = sqlExtension;
        }

        //AppModules b1Modules = AppModules.Instance;

        [KernelFunction]
        [Description("Returns a list of all available SAP modules to help decide which schema to load.")]
        public string GetAvailableModules() => _schemaService.GetAvailableModules(); // b1Modules.GetModules(); // string.Join(", ", moduleSchemas.Keys);

        [KernelFunction]
        [Description("Gets the detailed table and field schema for a specific SAP module.")]
        public async Task<string> GetSchemaForModule(
            [Description("Module name: Accounts, Sales, Inventory, Purchase")] string modulename,
            Kernel kernel)
        {
            kernel.Data["LastRequestedModule"] = modulename;
            var returnData = _schemaService.GetModuleSchema(modulename); 
            if (_promptExtension != null)
            {
                var waNumber = kernel.Data["WhatsAppNumber"]?.ToString();
                var userQuestion = kernel.Data["UserQuestion"]?.ToString();
                Console.WriteLine($"PhoneNumber: {waNumber}, Question: {userQuestion}, requesting for module {modulename}");
                if (!string.IsNullOrWhiteSpace( waNumber))
                {
                    var xtensionText = await _promptExtension.GetModuleConstraintAsync(waNumber, modulename,userQuestion?.ToString());
                    returnData = $"{xtensionText}\n{returnData}\n{xtensionText}";
                }
            }
            return returnData;
            //return b1Modules.GetModuleDetails(modulename);
        }

        [KernelFunction]
        [Description("Executes a READ-ONLY SQL SELECT query against the SAP database.")]
        public async Task<string> ExecuteSql([Description("The T-SQL SELECT statement")] string sql, Kernel kernel)
        {
            // Add a check here to ensure the query starts with "SELECT"
            if (!sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                return "Error: Only SELECT queries are permitted.";

            var waNumber = kernel.Data["WhatsAppNumber"]?.ToString();
            var userQuestion = kernel.Data["UserQuestion"]?.ToString();
            var moduleName = kernel.Data["LastRequestedModule"]?.ToString();
            try
            {
                Console.WriteLine($"PhoneNumber: {waNumber}, Question: {userQuestion}, module {moduleName}");
                if (_sqlExtension != null)
                {
                    sql = await _sqlExtension.OnBeforeExecuteAsync(waNumber?.ToString(), sql);
                }
                Console.WriteLine($"[SAP EXECUTION]: {sql}");
                using IDbConnection db = new SqlConnection(_connectionString);

                // Use Dapper to get dynamic results (perfect for unpredictable SAP tables)
                var results = await db.QueryAsync(sql);
                if (!results.Any()) return "[]";

                // Return raw JSON to the AI
                return JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                // Give the error to the AI so it can try to fix the SQL
                return $"Database Error: {ex.Message}. Check your table/column names.";
            }
            
        }

        
    }
}
