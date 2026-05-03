// See https://aka.ms/new-console-template for more information
using System.Text;
using UglyToad.PdfPig;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using System.Text.Json;

// 1. Setup Kernel
var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion("gpt-4o", "your-openai-api-key");
var kernel = builder.Build();

// 2. Prompt for JSON output (to make aggregation easy)
string jsonPrompt = @"
Extract transactions from this credit card statement. 
Categorize into: Clothes, Train Tickets, Movie Tickets, Hotels, Groceries, Others.
Return ONLY a valid JSON array of objects with 'Category' and 'Amount' keys.
Example: [{""Category"": ""Clothes"", ""Amount"": 1200.50}, {""Category"": ""Train Tickets"", ""Amount"": 500.00}]

Text: {{$input}}";

var analysisFunction = kernel.CreateFunctionFromPrompt(jsonPrompt);

// 3. Process and Aggregate
var allTransactions = new List<Transaction>();
var files = Directory.GetFiles(@"C:\Bills", "hdfccreditcard*.pdf");

foreach (var file in files)
{
    Console.WriteLine($"Reading {Path.GetFileName(file)}...");
    string text = ExtractTextFromProtectedPdf(file, "YOUR_PASSWORD");

    var result = await kernel.InvokeAsync(analysisFunction, new() { ["input"] = text });

    // Parse the AI's JSON response and add to our master list
    var batch = JsonSerializer.Deserialize<List<Transaction>>(result.ToString());
    if (batch != null) allTransactions.AddRange(batch);
}

// 4. Group and Sum using LINQ
var yearlySummary = allTransactions
    .GroupBy(t => t.Category)
    .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) });

Console.WriteLine("\n=== YEARLY SPENDING SUMMARY ===");
foreach (var item in yearlySummary)
{
    Console.WriteLine($"{item.Category,-15}: {item.Total,10:C2}");
}


static string ExtractTextFromProtectedPdf(string filePath, string password)
{
    var sb = new StringBuilder();

    // PdfPig handles passwords in the ParsingOptions
    var options = new ParsingOptions { Password = password };

    using (var document = PdfDocument.Open(filePath, options))
    {
        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }
    }
    return sb.ToString();
}

// Model for deserialization
public record Transaction(string Category, decimal Amount);
