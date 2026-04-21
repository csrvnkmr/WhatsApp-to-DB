using ChromaDB.Client;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DBSearchHelperPlugin
{
    public class AWSearchHelperPlugin
    {
        private const string CollectionNamePerson = "AW-Person";
        private const string CollectionNameProduct = "AW-Product";
        private const string CollectionNameStore = "AW-Store";

        private VectorDBSync.DynamicVectorSyncService dvss;
        private VectorDBSync.VectorSyncService vss;
        private VectorDBSync.ISyncService syncService;
        private string chromaUrl, connectionString, apiKey;

        private void LoadChromaDBService(IConfiguration config)
        {

            apiKey = config["OpenAiSettings:ApiKey"] ?? "";
            chromaUrl = config["DatabaseSettings:ChromaUrl"] ?? "";
            connectionString = config["DatabaseSettings:ConnectionString"] ?? "";
            Console.WriteLine($"[SearchHelperPluginAW] Initialized with ChromaUrl: {chromaUrl}, ConnectionString: {connectionString}, " +
                $"ApiKey: {(string.IsNullOrEmpty(apiKey) ? "Not Set" : "Set")}");
            dvss = new VectorDBSync.DynamicVectorSyncService(apiKey, chromaUrl, connectionString);
            syncService = dvss;

        }
        private void LoadSqLiteDBService(IConfiguration config)
        {
            connectionString = config["DatabaseSettings:ConnectionString"] ?? "";
            vss = new VectorDBSync.VectorSyncService(connectionString);
            syncService = vss;
            Console.WriteLine($"[SearchHelperPluginAW] Initialized with SQLite Vector Sync Service. ConnectionString: {connectionString}");
        }

        public AWSearchHelperPlugin()
        {
            /*
            Console.WriteLine($"In [AWSearchHelperPlugin] Constructor");
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .Build();

            apiKey = config["OpenAiSettings:ApiKey"] ?? "";
            chromaUrl = config["DatabaseSettings:ChromaUrl"] ?? "";
            connectionString = config["DatabaseSettings:ConnectionString"] ?? "";
            Console.WriteLine($"[SearchHelperPlugin] Initialized with ChromaUrl: {chromaUrl}, ConnectionString: {connectionString}, " +
                $"ApiKey: {(string.IsNullOrEmpty(apiKey) ? "Not Set" : "Set")}");
            dvss = new VectorDBSync.DynamicVectorSyncService(apiKey, chromaUrl, connectionString);
            vss = new VectorDBSync.VectorSyncService(connectionString);
            */
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .Build();
            //LoadChromaDBService(config);
            LoadSqLiteDBService(config);
        }

        [KernelFunction]
        [Description("Gets the ProductId from product name using Vector DB")]
        public async Task<string> GetProductId([Description("Product Name")] string productname)
        {
            if (string.IsNullOrWhiteSpace(productname))
                return "Error: productname cannot be blank";

            Console.WriteLine($"[GetProductId]: {productname}");
            return await FuzzySearchCode(CollectionNameProduct, productname);
        }

        [KernelFunction]
        [Description("Gets the SalesPersonId from sales person name using Vector DB")]
        public async Task<string> GetSalesPersonId([Description("Sales Person Name")] string salespersonname)
        {
            if (string.IsNullOrWhiteSpace(salespersonname))
                return "Error: salespersonname cannot be blank";

            Console.WriteLine($"[GetSalesPersonId]: {salespersonname}");
            var metadata = new Dictionary<string, object> { { "PersonType", "SP" } };
            return await FuzzySearchCode(CollectionNamePerson, salespersonname, metadata);
        }

        [KernelFunction]
        [Description("Gets the EmployeeId from employee name using Vector DB")]
        public async Task<string> GetEmployeeId([Description("Employee Name")] string employeename)
        {
            if (string.IsNullOrWhiteSpace(employeename))
                return "Error: employeename cannot be blank";

            Console.WriteLine($"[GetEmployeeId]: {employeename}");
            var metadata = new Dictionary<string, object> { { "PersonType", "EM" } };
            return await FuzzySearchCode(CollectionNamePerson, employeename, metadata);
        }


        [KernelFunction]
        [Description("Gets the CustomerId for a retail or corporate customer. Use this before querying SalesOrderHeader.")]
        public async Task<string> GetCustomerId([Description("The name of the company or individual")] string customername)
        {
            if (string.IsNullOrWhiteSpace(customername)) return "Error: Name is required.";

            // 1. Check Stores (Corporate Customers) first
            // Note: In AW, Store.BusinessEntityID is what maps to Customer.StoreID
            var storeResult = await FuzzySearchCode(CollectionNameStore, customername);
            if (IsValidMatch(storeResult))
            {
                // Resolve Store ID to CustomerID
                return await ResolveIdToCustomerId("StoreID", storeResult);
            }

            // 2. Check Persons (Retail Customers)
            var metadata = new Dictionary<string, object> { { "PersonType", "IN" } };
            var personResult = await FuzzySearchCode(CollectionNamePerson, customername, metadata);

            if (IsValidMatch(personResult))
            {
                // Resolve PersonID (BusinessEntityID) to CustomerID
                return await ResolveIdToCustomerId("PersonID", personResult);
            }

            return "Error: Could not find a matching customer.";
        }

        // New Helper: Centralizes the SQL translation logic
        private async Task<string> ResolveIdToCustomerId(string columnName, string idValue)
        {
            using IDbConnection db = new SqlConnection(connectionString);

            // 1. Get ALL CustomerIDs associated with this BusinessEntityID
            var sql = $@"SELECT c.CustomerID, t.Name as Territory 
                 FROM Sales.Customer c 
                 LEFT JOIN Sales.SalesTerritory t ON c.TerritoryID = t.TerritoryID
                 WHERE c.{columnName} = @id";

            var results = await db.QueryAsync<dynamic>(sql, new { id = idValue });

            if (!results.Any()) return "Error: No Customer record found.";

            // 2. If there's only one, just return the ID
            if (results.Count() == 1) return results.First().CustomerID.ToString();

            // 3. If there are multiple, return a JSON string explaining the options
            // The LLM will see this and can decide to sum them up or ask "Which territory?"
            return JsonSerializer.Serialize(new
            {
                Note = "Multiple customer records found for this entity.",
                Options = results.Select(r => new { r.CustomerID, r.Territory })
            });
        }

        private bool IsValidMatch(string result) =>
            !string.IsNullOrEmpty(result) && !result.StartsWith("Error");

        [KernelFunction]
        [Description("Searches the vector database for a specific code (like ProductId or CustomerId) using a fuzzy name.")]
        public async Task<string> FuzzySearchCode(string collectionName, string fuzzyName, Dictionary<string, object>? metadata = null)
        {
            try
            {

                //var results = await dvss.SearchCollection(collectionName, fuzzyName, 1, metadata);
                var results = await syncService.SearchCollection(collectionName, fuzzyName, 1, metadata);
                var match = results.FirstOrDefault();

                if (match != null && match.Distance < 0.6) // Distance < 0.6 is a good match in Chroma
                {
                    Console.WriteLine($"Fuzzy searching for {fuzzyName} in collection {collectionName}. Found Match: {match.Id} ({match.Document})");
                    return match.Id; // Assuming the ID is the code we want
                }
                Console.WriteLine($"Fuzzy searching for {fuzzyName} in collection {collectionName}. No exact match found.");

                return "Error: No exact match found in vector storage.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error doing vector search for {fuzzyName} in collection {collectionName}: {ex}. ");
                return $"Error doing vector search for {fuzzyName} in collection {collectionName}: {ex.Message}. ";
            }
        }
    }
}
