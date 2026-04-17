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

        [KernelFunction]
        [Description("Returns a list of all available modules to help decide which schema to load.")]
        public string GetAvailableModules() => _schemaService.GetAvailableModules(); // b1Modules.GetModules(); // string.Join(", ", moduleSchemas.Keys);


        [KernelFunction]
        [Description("Gets the detailed table and field schema for specific modules. " +
             "IMPORTANT: You must call GetAvailableModules first to identify the correct module names.")]        
        public async Task<string> GetSchemaForModule(
            [Description("Comma-separated module names")] string modulename,    
            Kernel kernel)
        {
            // 1. Resolve Identity
            var identity = kernel.Data["UserIdentity"] as IdentityContext;
            if (identity == null) return "Error: Security context missing.";

            // 2. Split and Clean Module Names
            var requestedModules = modulename.Split(',')
                                             .Select(m => m.Trim())
                                             .Where(m => !string.IsNullOrEmpty(m))
                                             .ToList();

            kernel.Data["LastRequestedModule"] = string.Join(", ", requestedModules);

            var schemaBuilder = new StringBuilder();
            var waNumber = kernel.Data["WhatsAppNumber"]?.ToString();
            var userQuestion = kernel.Data["UserQuestion"]?.ToString();

            var isFirstModule = true;   

            foreach (var module in requestedModules)
            {
                if (!isFirstModule)
                {
                    schemaBuilder.AppendLine("\n--- Next Module ---\n");
                }
                isFirstModule = false;
                // 3. Security Check: Role-Based Access Control
                // Allow if Admin OR if the specific module is in their authorized list
                bool isAuthorized = identity.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                                    (identity.AuthorizedModules != null &&
                                     identity.AuthorizedModules.Contains(module, StringComparer.OrdinalIgnoreCase));

                if (!isAuthorized)
                {
                    schemaBuilder.AppendLine($"[Access Denied: You do not have permission to view the {module} schema.]");
                    continue;
                }

                // 4. Fetch Base Schema
                var moduleSchema = _schemaService.GetModuleSchema(module);

                // 5. Apply Dynamic Constraints (Row Level Security / Prompt Extensions)
                if (_promptExtension != null && !string.IsNullOrWhiteSpace(waNumber))
                {
                    var xtensionText = await _promptExtension.GetModuleConstraintAsync(waNumber, module, userQuestion);
                    // Wrap the schema with the constraints to ensure the LLM prioritizes them
                    moduleSchema = $"--- {module} Security Constraints ---\n{xtensionText}\n\n--- {module} Schema ---\n{moduleSchema}";
                }

                schemaBuilder.AppendLine(moduleSchema);
            }

            var finalSchema = schemaBuilder.ToString();
            return string.IsNullOrWhiteSpace(finalSchema) ? "No authorized modules found." : finalSchema;
        }
       
        public async Task SetDatabaseSessionAsync(SqlConnection conn, IdentityContext identity)
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

            var waNumber = kernel.Data["WhatsAppNumber"]?.ToString();
            var userQuestion = kernel.Data["UserQuestion"]?.ToString();
            var moduleName = kernel.Data["LastRequestedModule"]?.ToString();
            var identity = kernel.Data["UserIdentity"] as IdentityContext;
            try
            {
                Console.WriteLine($"PhoneNumber: {waNumber}, Question: {userQuestion}, module {moduleName}");
                if (_sqlExtension != null)
                {
                    sql = await _sqlExtension.OnBeforeExecuteAsync(waNumber?.ToString(), sql);
                }
                Console.WriteLine($"[SAP EXECUTION]: {sql}");
                using IDbConnection db = new SqlConnection(_connectionString);
                // Ensure the connection is open before setting session context, as Dapper relies on it for the session state to be applied correctly.
                db.Open(); 
                if (identity != null && identity.Role?.ToLower()!= "admin")
                {
                    await SetDatabaseSessionAsync(db as SqlConnection, identity);
                }
                    
                // Use Dapper to get dynamic results (perfect for unpredictable SAP tables)
                var results = await db.QueryAsync(sql);

                //var salesPersoncontext = await db.ExecuteScalarAsync<string>("SELECT SESSION_CONTEXT(N'SalesPersonID')");
                //Console.WriteLine($"[DB DEBUG] Session Context for SalesPersonID: {salesPersoncontext}");
                //var employeecontext = await db.ExecuteScalarAsync<string>("SELECT SESSION_CONTEXT(N'EmployeeId')");
                //Console.WriteLine($"[DB DEBUG] Session Context for EmployeeId: {employeecontext}");

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
