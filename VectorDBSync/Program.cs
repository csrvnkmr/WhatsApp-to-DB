using ChromaDB.Client;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;
using VectorDBSync;
//var vss = new VectorDBSync.ChromaSyncService("", "", "");
IConfiguration config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .Build();

var apikey = config["OpenAiSettings:ApiKey"];
var chromaUrl = config["DatabaseSettings:ChromaUrl"];
var connectionString = config["DatabaseSettings:ConnectionString"];

var json = await File.ReadAllTextAsync("vectorConfig.json");
var configs = JsonSerializer.Deserialize<VectorSyncRoot>(json);
DynamicVectorSyncService dvss = new DynamicVectorSyncService(apikey, chromaUrl, connectionString);
await dvss.SyncAllCollections(configs.SyncCollections);
//await TestSearch(dvss);
Console.WriteLine("Press Enter to close");
Console.ReadLine();

async static Task TestSearch(DynamicVectorSyncService dvss)
{
    var searchText = "Patrik Wedge";
    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} searching Person for {searchText}.");
    //var searchResults = await vss.SearchBusinessPartners(searchText);
    var searchResults = await dvss.SearchCollection("AW-Person", searchText);

    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} searching Person completed.");
    if (searchResults.Count > 0)
    {
        foreach (var res in searchResults)
        {
            Console.WriteLine($"Match Found! ID: {res.Id} | Similarity Distance: {res.Distance}");
            Console.WriteLine($"Content: {res.Document}");
        }
    }
    else
    {
        Console.WriteLine("No matches found.");
    }

    var searchItemText = "Road 650 Red 44";
    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} searching for Product {searchItemText}.");
    //var searchItemResults = await vss.SearchItems(searchItemText);
    var searchItemResults = await dvss.SearchCollection("AW-Product", searchItemText);
    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} searching for Product completed.");
    if (searchItemResults.Count == 0)
    {
        Console.WriteLine("No matches found.");
    }
    foreach (var res in searchItemResults)
    {
        Console.WriteLine($"Match Found! ID: {res.Id} | Similarity Distance: {res.Distance}");
        Console.WriteLine($"Content: {res.Document}");
    }

}
