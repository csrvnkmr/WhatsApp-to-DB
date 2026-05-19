using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;
using WhatsAppToDB.Abstractions;

using VectorDBSync;

// Create a simple SQL Server DB provider
IDbProvider dbProvider = new SqlServerDbProvider();

var json = await File.ReadAllTextAsync("vectorConfig.json");
var vectorConfigs = JsonSerializer.Deserialize<VectorSyncRoot>(json, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
});

var settingsJson = await File.ReadAllTextAsync("appsettings.json");
var settings = JsonSerializer.Deserialize<VectorDBSettings>(settingsJson, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
}) ?? throw new JsonException("Failed to deserialize Vector DB settings from appsettings.json.");

var sourceConnectionString = settings.DatabaseSettings.ConnectionString ?? string.Empty;
ISyncService vss = new VectorSyncService(settings, sourceConnectionString);

await vss.SyncAllCollections(vectorConfigs.SyncCollections, dbProvider);
await TestSearchB1_2(vss);

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

async static Task TestSearchB1_2(ISyncService vss)
{
    await TestSearchCollection(vss, "OCRD", "SG auto parts");
    await TestSearchCollection(vss, "OITM", "Socket and Wire");
    await TestSearchCollection(vss, "OITB", "Japanes vehicles");
    await TestSearchCollection(vss, "OSLP", "kasthuri");
    await TestSearchCollection(vss, "FEWSHOTQUERIES", "Customer Ledger");
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
