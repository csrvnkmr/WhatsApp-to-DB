using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.MemoryStorage;
using WhatsAppToDB.DbProviders.SchemaModels;
using WhatsAppToDB.Services;


public class FewShotMemoryService
{
    private readonly ITextEmbeddingGenerator _embeddingGenerator;
    private readonly IMemoryDb _vectorDatabase;
    private readonly ITextGenerator _textGenerator;

    // Pass your dynamic runtime implementations (e.g., Jina/OpenAI, SQLite/Chroma) here
    public FewShotMemoryService(ITextEmbeddingGenerator embeddingGenerator, IMemoryDb vectorDatabase, ITextGenerator textGenerator)
    {
        _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
        _vectorDatabase = vectorDatabase ?? throw new ArgumentNullException(nameof(vectorDatabase));
        _textGenerator = textGenerator ?? throw new ArgumentNullException(nameof(textGenerator));
    }

    private string GetIndexName(string targetDatabase) => $"shot-{targetDatabase.ToLower().Trim()}";

    /// <summary>
    /// Sanitizes a string to create a valid Kernel Memory document ID.
    /// Kernel Memory only allows: A-Z, a-z, 0-9, '.', '_', '-'
    /// </summary>
    private string SanitizeDocumentId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Guid.NewGuid().ToString("N").Substring(0, 8);

        var sanitized = new System.Text.StringBuilder();
        foreach (char c in id)
        {
            if (char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-')
            {
                sanitized.Append(c);
            }
            else if (char.IsWhiteSpace(c))
            {
                // Replace spaces with underscores
                sanitized.Append('_');
            }
            // Skip all other characters
        }

        var result = sanitized.ToString();
        return string.IsNullOrEmpty(result) ? Guid.NewGuid().ToString("N").Substring(0, 8) : result;
    }
 

    /// <summary>
    /// Helper method to safely build the isolated IKernelMemory instance using your dynamic injections.
    /// </summary>
    private IKernelMemory GetMemoryPipeline()
    {
        var options = new KernelMemoryBuilderBuildOptions
        {
            AllowMixingVolatileAndPersistentData = true
        };

        // Wrap the embedding generator to report a higher token limit compatible with Kernel Memory defaults
        var wrappedEmbedding = new CompatibleEmbeddingGeneratorWrapper(_embeddingGenerator, reportedMaxTokens: 1024);

        return new KernelMemoryBuilder()
            .WithCustomEmbeddingGenerator(wrappedEmbedding)
            .WithCustomMemoryDb(_vectorDatabase)
            .WithCustomTextGenerator(_textGenerator)
            .Build(options); // Allow mixing volatile (file) and persistent (vector DB) storage
    }

    public async Task SaveOrUpdateFewShotAsync(string targetDatabase, ModuleQuery moduleQuery)
    {
        string targetIndex = GetIndexName(targetDatabase);

        // Generate embedding directly
        var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(moduleQuery.Name);

        var record = new MemoryRecord
        {
            Id = SanitizeDocumentId(moduleQuery.Name),
            Vector = embedding,
            Tags = new TagCollection
            {
                { "module", moduleQuery.Module },
                { "__document_id", SanitizeDocumentId(moduleQuery.Name) }
            },
            Payload = new Dictionary<string, object>
            {
                { "text", moduleQuery.Name },
                { "module", moduleQuery.Module }
            }
        };

        await _vectorDatabase.UpsertAsync(targetIndex, record);
    }

    /// <summary>
    /// Safely updates or upserts a single few-shot template when an admin modifies it.
    /// </summary>
    public async Task SaveOrUpdateFewShotAsyncOLD(string targetDatabase, ModuleQuery moduleQuery)
    {
        // Safe lowercased index string isolated by database name
        string targetIndex = GetIndexName(targetDatabase);
        IKernelMemory memoryContext = GetMemoryPipeline();

        // ImportTextAsync acts as an UPSERT if the documentId matches
        await memoryContext.ImportTextAsync(
            text: moduleQuery.Name,          // The question text gets vectorized
            documentId: SanitizeDocumentId(moduleQuery.Name),    // Sanitized unique question name used as the primary Key/ID
            index: targetIndex,              // The dynamic database index partition
            tags: new TagCollection 
            { 
                { "module", moduleQuery.Module } 
            }
        );
        bool exists = await memoryContext.IsDocumentReadyAsync(
            documentId: SanitizeDocumentId(moduleQuery.Name),
            index: targetIndex
        );
        Console.WriteLine($"Document ready after import: {exists}");
    }

    public async Task SaveOrUpdateFewShotAsyncNOT(string targetDatabase, ModuleQuery moduleQuery)
    {
        // Safe lowercased index string isolated by database name
        string targetIndex = GetIndexName(targetDatabase);
        IKernelMemory memoryContext = GetMemoryPipeline();

        // FIX: Pass the steps collection directly to force synchronous execution.
        // By providing an explicit string array of steps, Kernel Memory runs them 
        // immediately on the current thread, completely bypassing the asynchronous background queue.
        var steps = new[] { "extract", "partition", "gen_embeddings", "save_records" };

        await memoryContext.ImportTextAsync(
            text: moduleQuery.Name,                       // The question text gets vectorized
            documentId: SanitizeDocumentId(moduleQuery.Name), // Unique question name used as Key/ID
            index: targetIndex,                           // The database partition index
            tags: new TagCollection 
            { 
                { "module", moduleQuery.Module } 
            },
            steps: steps // This forces an inline, blocking execution path!
        );
    }

    public async Task SaveOrUpdateFewShotAsync(string targetDatabase, List<ModuleQuery> moduleQueries)
    {
        for (int i = 0; i < moduleQueries.Count; i++)
        {
            var moduleQuery = moduleQueries[i];
            await SaveOrUpdateFewShotAsync   (targetDatabase, moduleQuery);
        }
    }

    /// <summary>
    /// Deletes a specific few-shot question when an admin removes it.
    /// </summary>
    public async Task DeleteFewShotAsync(string targetDatabase, string questionName)
    {
        string targetIndex = GetIndexName(targetDatabase);
        var memoryContext = GetMemoryPipeline();
        
        await memoryContext.DeleteDocumentAsync(documentId: SanitizeDocumentId(questionName), index: targetIndex);
    }


    public async Task<string?> FindClosestMatchIdAsync(
        string targetDatabase, string currentModule, string userQuestion)
    {
        string targetIndex = GetIndexName(targetDatabase);
        await DumpAllFewShotsAsync(targetDatabase);
        //var queryEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(userQuestion);

        var results =   _vectorDatabase.GetSimilarListAsync(
            index: targetIndex,
            text: userQuestion,
            //withEmbeddings: false,
            //embedding: queryEmbedding,
            filters: new[] { MemoryFilters.ByTag("module", currentModule) },
            minRelevance: 0.75,
            limit: 1
        );

        await foreach (var (record, score) in results)
        {
            Console.WriteLine($"Score: {score:F4} | Text: {record.Payload["text"]}");
            return record.Payload["text"]?.ToString();
        }
        //var topMatch = await results.FirstOrDefaultAsync();
        //return topMatch.Item1?.Id; // Item1 = MemoryRecord, Item2 = relevance score
        return "";
    }

    /// <summary>
    /// Searches the distinct database index for the closest few-shot match.
    /// </summary>
    public async Task<string?> FindClosestMatchIdAsyncOld(string targetDatabase, string currentModule, string userQuestion)
    {
        await DumpAllFewShotsAsync(targetDatabase);
        string targetIndex = GetIndexName(targetDatabase);
        var memoryContext = GetMemoryPipeline();

        // Perform precise scoped search matching the module
        SearchResult searchResult = await memoryContext.SearchAsync(
            query: userQuestion,
            index: targetIndex,
            limit: 1, // We only need the single closest blueprint match
            filter: MemoryFilters.ByTag("module", currentModule)
        );

        var topMatch = searchResult.Results.FirstOrDefault();
        if (topMatch != null)
        {
            // Extract mathematical relevance score
            double confidence = topMatch.Partitions.FirstOrDefault()?.Relevance ?? 0;

            // 75% semantic floor match or higher
            if (confidence >= 0.75)
            {
                // Returns the unique documentId (ModuleQuery.Name)
                return topMatch.Link; 
            }
        }

        return null;
    }

    public async Task DumpAllFewShotsAsync(string targetDatabase)
    {
        // 1. Resolve the clean index identifier name
        string targetIndex = GetIndexName(targetDatabase);
        
        Console.WriteLine($"\n==================================================");
        Console.WriteLine($"🔍 DIRECT DATABASE DUMP FOR INDEX: {targetIndex}");
        Console.WriteLine($"==================================================\n");

        try
        {
            // 2. Query your injected IMemoryDb instance directly.
            // This skips Kernel Memory's semantic handlers and hits your adapter directly.
            // Passing null for filters tells your GetListAsync method to return all rows.
            IAsyncEnumerable<MemoryRecord> recordsStream = _vectorDatabase.GetListAsync(
                index: targetIndex,
                filters: null, 
                limit: 100,
                withEmbeddings: false
            );

            int counter = 0;

            await foreach (MemoryRecord record in recordsStream)
            {
                counter++;
                
                string documentId = record.Id; 
                
                // Extract text from Kernel Memory's internal dictionary payload map
                string storedQuestionText = record.Payload.ContainsKey("text") 
                    ? record.Payload["text"]?.ToString() ?? string.Empty 
                    : "Empty Payload Content";

                Console.WriteLine($"📍 Record #{counter}");
                Console.WriteLine($"   🔹 DocumentID (Sanitized Key): \"{documentId}\"");
                Console.WriteLine($"   🔹 Stored Question Text      : \"{storedQuestionText}\"");
                
                // Print out metadata tags (e.g. module values)
                Console.WriteLine($"   🔹 Meta Tags Matrix:");
                if (record.Tags != null && record.Tags.Any())
                {
                    foreach (var tagGroup in record.Tags)
                    {
                        string tagValues = string.Join(", ", tagGroup.Value);
                        Console.WriteLine($"      - {tagGroup.Key} = [{tagValues}]");
                    }
                }
                else
                {
                    Console.WriteLine($"      - (No filtering tags attached)");
                }
                Console.WriteLine(new string('-', 50));
            }

            if (counter == 0)
            {
                Console.WriteLine("⚠️ The database table returned 0 records. The insert phase failed.");
                Console.WriteLine($"👉 Action item: Open your debug folder and manually check if 'shot-{targetDatabase.ToLower()}.db' exists on disk and has data.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Runtime execution error mapping database directly: {ex.Message}");
        }
        
        Console.WriteLine($"\n==================================================");
    }

}
