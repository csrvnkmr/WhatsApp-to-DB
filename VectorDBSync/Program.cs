using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;
using VectorDBSync;
////var vss = new VectorDBSync.ChromaSyncService("", "", "");
//IConfiguration config = new ConfigurationBuilder()
//    .SetBasePath(Directory.GetCurrentDirectory())
//    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)    
//    .Build();

var json = await File.ReadAllTextAsync("vectorConfig.json");
var vectorConfigs = JsonSerializer.Deserialize<VectorSyncRoot>(json);

var settings = Settings.LoadFromFile("appsettings.json");
ISyncService vss = new VectorSyncService(settings);

await vss.SyncAllCollections(vectorConfigs.SyncCollections);
await TestSearchB1(vss);

Console.WriteLine("Press Enter to close");
Console.ReadLine();


async static Task TestSearchCollection(ISyncService syncService, string collectionName, string searchText)
{
    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} searching for {searchText} in collection {collectionName}.");
    //var searchItemResults = await vss.SearchItems(searchItemText);
    var searchItemResults = await syncService.SearchCollection(collectionName, searchText);
    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} searching for {searchText} in collection {collectionName} completed.");
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

async static Task TestSearchAW(ISyncService vss)
{
    await TestSearchCollection(vss, "AW-Store", "Bike Mechanic");
    await TestSearchCollection(vss, "AW-Person", "Patrik Wedge");
    await TestSearchCollection(vss, "AW-Product", "Road 650 Red 44");
}

async static Task TestSearchB1(ISyncService vss)
{
    await TestSearchCollection(vss, "OCRD", "Lumarks");
    await TestSearchCollection(vss, "OITM", "JB Officeprint 1186");
    await TestSearchCollection(vss, "OITB", "JB Printer");
    await TestSearchCollection(vss, "OSLP", "Bhaskar Lakshman");
    await TestSearchCollection(vss, "FEWSHOTQUERIES", "Itemgroupwise sales");
}


async static Task TestChromaSearch(DynamicVectorSyncService dvss)
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
