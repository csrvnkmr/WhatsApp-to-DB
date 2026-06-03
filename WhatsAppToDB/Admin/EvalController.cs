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
        private readonly WhatsAppToDB.Eval.EvalRunRepository _evalRepo;
        public EvalController(
            JsonConfigService jsonConfigService,
            DatabaseRegistry registry,
            DbProviderFactory factory,
            IQueryService queryService,
            IIdentityContextEnricher identityContextEnricher,
            IServiceScopeFactory scopeFactory,
            ILogger logger,
            LlmCancellationService cancellationService,
            WhatsAppToDB.Eval.EvalRunRepository evalRepo)
        {
            _jsonConfigService = jsonConfigService;
            _registry = registry;
            _factory = factory;
            _queryService = queryService;
            _identityContextEnricher = identityContextEnricher;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _cancellationService = cancellationService;
            _evalRepo = evalRepo;
        }


        [HttpPost("/admin/api/execute/{database}")]
        public async Task<IActionResult> ExecuteQuery(string database, [FromBody] ExecuteRequest request)
        {
            long? evalCaseId = null;
            long? evalRunId = null;
            
            try
            {
                if (string.IsNullOrWhiteSpace(request.QuestionText))
                {
                    return BadRequest("Question text is required.");
                }

                // 1. Load test suite queries for this database
                var queries = _jsonConfigService.GetTestSuiteQueries(database);
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

                // Check if we're in an eval context (try to find running eval_run)
                var userName = HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(userName) && _evalRepo != null)
                {
                    (evalRunId, evalCaseId) = await _evalRepo.GetOrCreateEvalContextAsync(userName, database, request.QuestionText);
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

                // 5. If in eval context, save results and mark completed
                string? resultFilePath = null;
                if (evalCaseId.HasValue && evalRunId.HasValue && _evalRepo != null)
                {
                    await _evalRepo.SaveEvalCaseResultAsync(database, evalRunId.Value, evalCaseId.Value, request.QuestionText, results);
                    resultFilePath = _evalRepo.GetEvalCaseResultFilePath(database, evalRunId.Value, evalCaseId.Value, request.QuestionText);
                    await _evalRepo.UpdateEvalCaseCompletedAsync(evalCaseId.Value, resultFilePath);
                }

                // 6. Serialize results as JSON and return
                var jsonResult = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
                return Ok(jsonResult);
            }
            catch (Exception ex)
            {
                _logger.LogError( 
                    $"Error executing query for database '{database}' and question '{request.QuestionText}' {ex}");
                
                // Mark as completed even on error
                if (evalCaseId.HasValue && _evalRepo != null)
                {
                    try
                    {
                        await _evalRepo.UpdateEvalCaseCompletedAsync(evalCaseId.Value, null);
                    }
                    catch { }
                }
                
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
                if (identity == null)
                    return Unauthorized();

                _identityContextEnricher.EnrichFromHttpContext(
                    identity,
                    HttpContext);

                var llmResult = request.LlmResult ?? "";
                var databaseResult = request.DatabaseResult ?? "";

                if (string.IsNullOrWhiteSpace(llmResult) || 
                    llmResult.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) || 
                    llmResult.StartsWith("Database Error:", StringComparison.OrdinalIgnoreCase) ||
                    llmResult.StartsWith("Progress stream disconnected", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(new
                    {
                        id = request.QuestionText,
                        llmresult = llmResult,
                        databaseresult = databaseResult,
                        comparison = "Failed",
                        difference = "Evaluation output is empty or contains an error.",
                        reason = "Comparison skipped because LLM generation failed.",
                        answersthequestion = "No"
                    });
                }

                using var cts = new CancellationTokenSource();
                _cancellationService.Register(userName, cts);
                try
                {
                    var comparisonResult = await _queryService.CompareWithLlm(_scopeFactory,
                        identity,
                        request.QuestionText,
                        databaseResult, llmResult,
                        cts.Token);

                    if (_evalRepo != null)
                    {
                        var provider = !string.IsNullOrWhiteSpace(request.Provider)
                            ? request.Provider
                            : identity.LlmProvider ?? string.Empty;
                        var model = !string.IsNullOrWhiteSpace(request.ModelName)
                            ? request.ModelName
                            : identity.LlmModel ?? string.Empty;

                        await _evalRepo.UpdateEvalCaseInferenceJudgeAsync(
                            userName,
                            database,
                            request.QuestionText,
                            provider,
                            model,
                            comparisonResult.Match,
                            comparisonResult.AnswersQuestion,
                            comparisonResult.Confidence ?? string.Empty,
                            comparisonResult.Reason ?? string.Empty,
                            comparisonResult.Differences ?? string.Empty,
                            comparisonResult.Match ? "Pass" : "Fail",
                            0);
                    }

                    return Ok(new
                    {
                        id = request.QuestionText,
                        llmresult = llmResult,
                        databaseresult = databaseResult,
                        comparison = comparisonResult.Match ? "Pass" : "Failed",
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

        [HttpPost("/eval/api/start")]
        public async Task<IActionResult> StartEval([FromBody] WhatsAppToDB.Eval.EvalRunRequest request)
        {
            try
            {
                var userName = HttpContext.Items[Constants.ContextItems.UserName]?.ToString() ?? "";
                var result = UserService.ValidateUserName(userName);
                if (!result.isSuccess)
                    return Unauthorized();

                var identity = result.identity;
                if (identity == null)
                    return Unauthorized();

                _identityContextEnricher.EnrichFromHttpContext(identity, HttpContext);

                var database = request.Database ?? identity.Database ?? "";
                if (string.IsNullOrWhiteSpace(database))
                    return BadRequest("Database is required");

                var userToken = HttpContext.Items[Constants.ContextItems.Token]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(userToken))
                    return Unauthorized();

                // Start eval run
                var evalRunId = await _evalRepo.InsertEvalRunAsync(identity.UserName, userToken, database);

                // Load test suite queries to enrich ground-truth
                var queries = _jsonConfigService.GetTestSuiteQueries(database) ?? new System.Collections.Generic.List<TestSuiteQueryItem>();

                int seq = 1;
                foreach (var q in request.Questions ?? new System.Collections.Generic.List<string>())
                {
                    var item = queries.FirstOrDefault(x => x.Question.Equals(q, StringComparison.OrdinalIgnoreCase));
                    var groundSql = item?.Query ?? string.Empty;
                    var module = item?.Module ?? string.Empty;

                    var caseId = await _evalRepo.InsertEvalCaseAsync(evalRunId, module, seq++, q, groundSql);

                    foreach (var m in request.Models ?? new System.Collections.Generic.List<WhatsAppToDB.Eval.EvalModel>())
                    {
                        await _evalRepo.InsertEvalCaseInferenceAsync(caseId, m.Provider, m.Model);
                    }
                }

                return Ok(new { eval_run_id = evalRunId });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error starting eval run: {ex}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("/eval/api/end")]
        public async Task<IActionResult> EndEval()
        {
            try
            {
                var userToken = HttpContext.Items[Constants.ContextItems.Token]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(userToken))
                {
                    return Unauthorized();
                }

                var evalRunId = await _evalRepo.GetEvalRunIdByUserTokenAsync(userToken);
                if (!evalRunId.HasValue)
                {
                    return NotFound("No eval run found for the provided user token.");
                }

                var rowsUpdated = await _evalRepo.UpdateEvalRunCompletionStatsAsync(evalRunId.Value);
                if (rowsUpdated == 0)
                {
                    return BadRequest("Failed to update eval run completion stats.");
                }

                return Ok(new { eval_run_id = evalRunId.Value });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error ending eval run: {ex}");
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

        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty;

        [JsonPropertyName("modelname")]
        public string ModelName { get; set; } = string.Empty;

        [JsonPropertyName("starttime")]
        public DateTime StartTime { get; set; }

        [JsonPropertyName("endtime")]
        public DateTime EndTime { get; set; }
    }
}
