using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using WhatsAppToDB.Services;
using WhatsAppToDB.DbProviders;
using WhatsAppToDB.Models;
using WhatsAppToDB.Database;

namespace WhatsAppToDB.Admin
{
    [ApiController]
    [Route("admin/api/eval")]
    public class EvalController : ControllerBase
    {
        private readonly JsonConfigService _jsonConfigService;
        private readonly DatabaseRegistry _registry;
        private readonly DbProviderFactory _factory;
        private readonly IQueryService _queryService;
        private readonly IIdentityContextEnricher _identityContextEnricher;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _logger;
        private readonly LlmCancellationService _cancellationService;
        public EvalController(
            JsonConfigService jsonConfigService,
            DatabaseRegistry registry,
            DbProviderFactory factory,
            IQueryService queryService,
            IIdentityContextEnricher identityContextEnricher,
            IServiceScopeFactory scopeFactory,
            ILogger logger,
            LlmCancellationService cancellationService)
        {
            _jsonConfigService = jsonConfigService;
            _registry = registry;
            _factory = factory;
            _queryService = queryService;
            _identityContextEnricher = identityContextEnricher;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _cancellationService = cancellationService;
        }

        [HttpPost("/admin/api/execute/{database}")]
        public async Task<IActionResult> ExecuteQuery(string database, [FromBody] ExecuteRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.QuestionText))
                {
                    return BadRequest("Question text is required.");
                }

                // 1. Load test suite queries for this database
                var queries = _jsonConfigService.LoadDatabaseConfigAndDecrypt<System.Collections.Generic.List<TestSuiteQueryItem>>(database, "testsuitequeries.json");
                if (queries == null || queries.Count == 0)
                {
                    return NotFound("No test suite queries configured for this database.");
                }

                // 2. Find the query matching request.QuestionText
                var queryItem = queries.FirstOrDefault(q => q.Question.Equals(request.QuestionText, StringComparison.OrdinalIgnoreCase));
                if (queryItem == null)
                {
                    return NotFound($"Query not found for question: '{request.QuestionText}'");
                }

                if (string.IsNullOrWhiteSpace(queryItem.Query))
                {
                    return BadRequest("The configured query text is empty.");
                }

                // 3. Get the database connection
                var dbConfig = _registry.GetDatabaseConfig(database);
                if (dbConfig == null)
                {
                    return NotFound($"Database configuration not found for: '{database}'");
                }

                var provider = _factory.GetDbProvider(dbConfig.DbProvider);
                using var conn = provider.GetConnection(dbConfig.ConnectionString);

                // 4. Run the query
                conn.Open();
                var results = await conn.QueryAsync(queryItem.Query);

                // 5. Serialize results as JSON and return
                var jsonResult = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
                return Ok(jsonResult);
            }
            catch (Exception ex)
            {
                _logger.LogError( 
                    $"Error executing query for database '{database}' and question '{request.QuestionText}' {ex}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("compare/{database}")]
        public async Task<IActionResult> Compare(string database, [FromBody] EvalCompareRequest request)
        {
            try
            {
                var userName =
                    HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? "";
                var result =
                    UserService.ValidateUserName(userName);

                if (!result.isSuccess)
                    return Unauthorized();

                var identity =
                    result.identity;
                _identityContextEnricher.EnrichFromHttpContext(
                    identity,
                    HttpContext);

                var llmResult = request.LlmResult ?? "";
                var databaseResult = request.DatabaseResult ?? "";
                using var cts = new CancellationTokenSource();
                _cancellationService.Register(userName, cts);
                try
                {
                    var comparisonResult = await _queryService.CompareWithLlm(_scopeFactory,
                        identity,
                        request.QuestionText,
                        databaseResult, llmResult,
                        cts.Token);

                    return Ok(new
                    {
                        id = request.QuestionText,
                        llmresult = llmResult,
                        databaseresult = databaseResult,
                        comparison = comparisonResult.Differences.Length == 0 ? "Pass" : "Failed",
                        difference = comparisonResult.Differences,
                        reason = comparisonResult.Reason,
                        answersthequestion = comparisonResult.AnswersQuestion ? "Yes" : "No"
                    });
                }
                catch(Exception ex) {
                    _logger.LogError(
                        $"Error comparing LLM and database results for database '{database}' and question '{request.QuestionText}' {ex}");
                    return BadRequest(new { error = ex.Message });
                }
                finally
                {
                    _cancellationService.Remove(userName);
                }

                /*var passed = false;

                // Clean matching
                if (llmResult.Trim().Equals(databaseResult.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    passed = true;
                }
                else if (!string.IsNullOrWhiteSpace(databaseResult))
                {
                    try
                    {
                        using var dbDoc = JsonDocument.Parse(databaseResult);
                        if (dbDoc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            var allMatch = true;
                            var hasRows = false;
                            foreach (var row in dbDoc.RootElement.EnumerateArray())
                            {
                                hasRows = true;
                                foreach (var prop in row.EnumerateObject())
                                {
                                    var valStr = prop.Value.ToString();
                                    if (!string.IsNullOrWhiteSpace(valStr) && !llmResult.Contains(valStr, StringComparison.OrdinalIgnoreCase))
                                    {
                                        allMatch = false;
                                        break;
                                    }
                                }
                                if (!allMatch) break;
                            }
                            passed = hasRows && allMatch;
                        }
                        else
                        {
                            passed = llmResult.Contains(databaseResult.Trim(), StringComparison.OrdinalIgnoreCase);
                        }
                    }
                    catch
                    {
                        passed = llmResult.Contains(databaseResult.Trim(), StringComparison.OrdinalIgnoreCase);
                    }
                }

                var comparisonValue = passed ? "Pass" : "Failed";

                return Ok(new
                {
                    id = request.QuestionText,
                    llmresult = llmResult,
                    databaseresult = databaseResult,
                    comparison = comparisonValue
                });
                */
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Error comparing LLM and database results for database '{database}' and question '{request.QuestionText}' {ex}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("stop")]
        [HttpGet("stop")]
        public IActionResult Stop()
        {
            var userName = HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? string.Empty;
            var cancelled = _cancellationService.TryCancel(userName);
            return Ok(new { success = true, cancelled });
        }
    }

    public class TestSuiteQueryItem
    {
        public string Question { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
    }

    public class ExecuteRequest
    {
        [JsonPropertyName("questiontext")]
        public string QuestionText { get; set; } = string.Empty;
    }

    public class EvalCompareRequest
    {
        [JsonPropertyName("questiontext")]
        public string QuestionText { get; set; } = string.Empty;

        [JsonPropertyName("llmresult")]
        public string LlmResult { get; set; } = string.Empty;

        [JsonPropertyName("databaseresult")]
        public string DatabaseResult { get; set; } = string.Empty;

        [JsonPropertyName("modelname")]
        public string ModelName { get; set; } = string.Empty;

        [JsonPropertyName("starttime")]
        public DateTime StartTime { get; set; }

        [JsonPropertyName("endtime")]
        public DateTime EndTime { get; set; }
    }
}
