using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Data;
using VectorDBSync;

namespace DBSearchHelperPlugin
{
    public class SearchHelperPluginOld
    {

        private VectorDBSync.ChromaSyncService css;        

        private string chromaUrl, connectionString, apiKey;

        public SearchHelperPluginOld()
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
            css = new VectorDBSync.ChromaSyncService(apiKey, chromaUrl, connectionString);
        }

        [KernelFunction]
        [Description("Gets the itemcode from itemname using Vector DB")]
        public async Task<string> GetItemCode([Description("ItemName")] string itemname)
        {
            // Add a check here to ensure the query starts with "SELECT"
            if (!string.IsNullOrWhiteSpace(itemname))
                return "Error: itemname cannot be blank";

            Console.WriteLine($"[GetItemCode]: {itemname}");
            try
            {
                var searchResult= await css.SearchItems(itemname);
                if (searchResult != null && searchResult.Any())
                {
                    var topResult = searchResult.First();
                    Console.WriteLine($"[GetItemCode] Search successful. Top match: {topResult.Id} {topResult.Document} with distance {topResult.Distance}");
                    return topResult.Id; // Assuming the document contains the item code
                }
                Console.WriteLine($"[GetItemCode] Search failed. returning itemname");
                return itemname;
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
                // Use Dapper to get dynamic results (perfect for unpredictable SAP tables)
                var results = await db.QueryAsync<string>(sql, new {CardCode=cardcode});
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
            // Add a check here to ensure the query starts with "SELECT"
            if (string.IsNullOrWhiteSpace(cardname))
                return "Error: CardName cannot be blank";

            Console.WriteLine($"[GetCardCode]: {cardname}");
            try
            {
                var searchResult = await css.SearchBusinessPartners(cardname);
                if (searchResult != null && searchResult.Any())
                {
                    var topResult = searchResult.First();
                    Console.WriteLine($"[GetCardCode] Search successful. Top match: {topResult.Id} {topResult.Document} with distance {topResult.Distance}");
                    return topResult.Id; // Assuming the document contains the item code
                }
                Console.WriteLine($"[GetCardCode] Search failed. returning cardname");
                return cardname;
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
            var results = await css.SearchCollection(collectionName, fuzzyName, 1);
            var match = results.FirstOrDefault();

            if (match != null && match.Distance < 0.5) // Distance < 0.5 is a strong match in Chroma
            {
                return $"Found Match: {match.Id} ({match.Document})";
            }

            return "No exact match found in vector storage.";
        }
    }
}
