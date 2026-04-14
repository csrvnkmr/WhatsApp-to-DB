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
using System.Threading.Tasks;

namespace DBSearchHelperPlugin
{
    public class SearchHelperPlugin
    {
        
        private VectorDBSync.DynamicVectorSyncService dvss;

        private string chromaUrl, connectionString, apiKey;

        public SearchHelperPlugin()
        {
            Console.WriteLine($"In [SearchHelperPlugin] Constructor");
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .Build();

            apiKey = config["OpenAiSettings:ApiKey"] ?? "";
            chromaUrl = config["DatabaseSettings:ChromaUrl"] ?? "";
            connectionString = config["DatabaseSettings:ConnectionString"] ?? "";
            Console.WriteLine($"[SearchHelperPlugin] Initialized with ChromaUrl: {chromaUrl}, ConnectionString: {connectionString}, ApiKey: {(string.IsNullOrEmpty(apiKey) ? "Not Set" : "Set")}");
            dvss = new VectorDBSync.DynamicVectorSyncService(apiKey, chromaUrl, connectionString);
        }

        [KernelFunction]
        [Description("Gets the itemcode from itemname using Vector DB")]
        public async Task<string> GetItemCode([Description("ItemName")] string itemname)
        {
            if (!string.IsNullOrWhiteSpace(itemname))
                return "Error: itemname cannot be blank";

            Console.WriteLine($"[GetItemCode]: {itemname}");
            try
            {
                return await FuzzySearchCode("OITM", itemname);
            }
            catch (Exception ex)
            {
                return $"Error when getting itemcode for {itemname}: {ex.Message}. ";
            }

        }

        [KernelFunction]
        [Description("Retrieves the official CardName from database using a specific CardCode. Use this after a vector search to ensure the name is correct before displaying to the user.")]
        public async Task<string> GetCardName([Description("CardCode")] string cardcode)
        {
            // Add a check here to ensure the query starts with "SELECT"
            if (string.IsNullOrWhiteSpace(cardcode))
                return "Error: cardcode cannot be blank";

            Console.WriteLine($"[GetCardName]: {cardcode}");
            try
            {
                using IDbConnection db = new SqlConnection(connectionString);
                var sql = "Select CardName from OCRD where CardCode = @CardCode";
                var results = await db.QueryAsync<string>(sql, new { CardCode = cardcode });
                if (!results.Any()) return $"Error: CardName not found for {cardcode}";
                return results.First();

            }
            catch (Exception ex)
            {
                return $"Error when getting CardCode for {cardcode}: {ex.Message}. ";
            }

        }


        [KernelFunction]
        [Description("Gets the code for customer/supplier/vendor name using Vector DB")]
        public async Task<string> GetCardCode([Description("CardName")] string cardname)
        {
            if (string.IsNullOrWhiteSpace(cardname))
                return "Error: CardName cannot be blank";

            Console.WriteLine($"[GetCardCode]: {cardname}");
            try
            {
                return await FuzzySearchCode("OCRD", cardname);
            }
            catch (Exception ex)
            {
                return $"Error when getting CardCode for {cardname}: {ex.Message}. ";
            }

        }

        [KernelFunction]
        [Description("Searches the vector database for a specific code (like CardCode or ItemCode) using a fuzzy name.")]
        public async Task<string> FuzzySearchCode(string collectionName, string fuzzyName)
        {
            var results = await dvss.SearchCollection(collectionName, fuzzyName, 1);
            var match = results.FirstOrDefault();

            if (match != null && match.Distance < 0.6) // Distance < 0.6 is a good match in Chroma
            {
                Console.WriteLine($"Fuzzy searching for {fuzzyName} in collection {collectionName}. Found Match: {match.Id} ({match.Document})");
                return match.Id; // Assuming the ID is the code we want
            }
            Console.WriteLine($"Fuzzy searching for {fuzzyName} in collection {collectionName}. No exact match found.");

            return "No exact match found in vector storage.";
        }
    }
}
