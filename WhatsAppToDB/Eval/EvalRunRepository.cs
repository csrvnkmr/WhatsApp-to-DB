using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;
using WhatsAppToDB.Services;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.Eval
{
    public class EvalInferenceResult
    {
        public string? GeneratedSql { get; set; }
        public string? InferenceResultJson { get; set; }
        public int? LatencyMs { get; set; }
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public string? Verdict { get; set; }
    }

    public class EvalRunRepository
    {
        private readonly string _connectionString;
        private readonly FolderUtils _folderUtils;
        private readonly ILogger _logger;
        private readonly ChatDbRepository _chatRepo;
        private readonly DefaultFolders _defaultFolders;

        public EvalRunRepository(FolderUtils folderUtils, ILogger logger, ChatDbRepository chatRepo, JsonConfigService jsonConfigService)
        {
            _folderUtils = folderUtils;
            _logger = logger ?? new AppLogger();
            _chatRepo = chatRepo;
            _defaultFolders = jsonConfigService.GetDefaultFolders();

            var dbPath = _folderUtils.GetChatHistoryDBPath();
            _connectionString = $"Data Source={dbPath}";

            EnsureTables();
        }

        private SqliteConnection GetConnection() => new SqliteConnection(_connectionString);

        private void EnsureTables()
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                
                // Create eval_run table
                conn.Execute(EvalTableSqls.CreateEvalRunTable);
                
                // Create eval_case table
                conn.Execute(EvalTableSqls.CreateEvalCaseTable);
                
                // Create eval_case_inference table
                conn.Execute(EvalTableSqls.CreateEvalCaseInferenceTable);
                
                // Create indices
                conn.Execute(EvalTableSqls.CreateEvalIndices);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EvalRunRepository] Error creating eval tables: {ex.Message}");
            }
        }

        public async Task<long> InsertEvalRunAsync(string userName, string usertoken, string database)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var sql = @"
INSERT INTO eval_run (username, usertoken, database, started_at, status)
VALUES ($UserName, $UserToken, $Database, $StartedAt, $Status);
SELECT last_insert_rowid();
";

            var id = await conn.ExecuteScalarAsync<long>(sql, new
            {
                UserName = userName,
                UserToken = usertoken ?? string.Empty,
                Database = database,
                StartedAt = now,
                Status = "Running"
            });

            return id;
        }

        public async Task<long?> GetEvalRunIdByUserTokenAsync(string userToken)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var sql = @"
SELECT eval_run_id
FROM eval_run
WHERE usertoken = $UserToken
ORDER BY eval_run_id DESC
LIMIT 1";

            return await conn.QueryFirstOrDefaultAsync<long?>(sql, new { UserToken = userToken ?? string.Empty });
        }

        public async Task<int> UpdateEvalRunCompletionStatsAsync(long evalRunId)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var totalCases = await conn.ExecuteScalarAsync<long>(
                @"SELECT COUNT(*)
FROM eval_case_inference ci
JOIN eval_case c ON ci.eval_case_id = c.eval_case_id
WHERE c.eval_run_id = $EvalRunId",
                new { EvalRunId = evalRunId });

            var passedCount = await conn.ExecuteScalarAsync<long>(
                @"SELECT COUNT(*)
FROM eval_case_inference ci
JOIN eval_case c ON ci.eval_case_id = c.eval_case_id
WHERE c.eval_run_id = $EvalRunId
  AND ci.verdict = 'Pass'",
                new { EvalRunId = evalRunId });

            var failedCount = await conn.ExecuteScalarAsync<long>(
                @"SELECT COUNT(*)
FROM eval_case_inference ci
JOIN eval_case c ON ci.eval_case_id = c.eval_case_id
WHERE c.eval_run_id = $EvalRunId
  AND ci.verdict = 'Fail'",
                new { EvalRunId = evalRunId });

            var errorCount = await conn.ExecuteScalarAsync<long>(
                @"SELECT COUNT(*)
FROM eval_case_inference ci
JOIN eval_case c ON ci.eval_case_id = c.eval_case_id
WHERE c.eval_run_id = $EvalRunId
  AND ci.verdict = 'Error'",
                new { EvalRunId = evalRunId });

            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var sql = @"
UPDATE eval_run
SET completed_at = $CompletedAt,
    status = 'Completed',
    total_cases = $TotalCases,
    passed_count = $PassedCount,
    failed_count = $FailedCount,
    error_count = $ErrorCount
WHERE eval_run_id = $EvalRunId";

            return await conn.ExecuteAsync(sql, new
            {
                CompletedAt = now,
                TotalCases = totalCases,
                PassedCount = passedCount,
                FailedCount = failedCount,
                ErrorCount = errorCount,
                EvalRunId = evalRunId
            });
        }

        public async Task<long> InsertEvalCaseAsync(long evalRunId, string module, int sequence, string inputText, string? groundTruthSql)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var sql = @"
INSERT INTO eval_case (eval_run_id, module_name, sequence, input_text, ground_truth_sql)
VALUES ($EvalRunId, $Module, $Seq, $InputText, $GroundTruth);
SELECT last_insert_rowid();
";

            var id = await conn.ExecuteScalarAsync<long>(sql, new
            {
                EvalRunId = evalRunId,
                Module = module ?? string.Empty,
                Seq = sequence,
                InputText = inputText ?? string.Empty,
                GroundTruth = groundTruthSql ?? string.Empty
            });

            return id;
        }

        public async Task<long> InsertEvalCaseInferenceAsync(long evalCaseId, string provider, string model)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var sql = @"
INSERT INTO eval_case_inference (eval_case_id, provider_name, model_name)
VALUES ($EvalCaseId, $Provider, $Model);
SELECT last_insert_rowid();
";

            var id = await conn.ExecuteScalarAsync<long>(sql, new
            {
                EvalCaseId = evalCaseId,
                Provider = provider ?? string.Empty,
                Model = model ?? string.Empty
            });

            return id;
        }

        public async Task<int> UpdateEvalCaseStartedAsync(long evalCaseId)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var sql = "UPDATE eval_case SET started_at = $StartedAt WHERE eval_case_id = $Id;";

            return await conn.ExecuteAsync(sql, new { StartedAt = now, Id = evalCaseId });
        }

        public async Task<int> UpdateEvalCaseCompletedAsync(long evalCaseId, string? resultFile)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var sql = "UPDATE eval_case SET ground_truth_result_file = $ResultFile, completed_at = $CompletedAt WHERE eval_case_id = $Id;";

            return await conn.ExecuteAsync(sql, new { ResultFile = resultFile ?? string.Empty, CompletedAt = now, Id = evalCaseId });
        }

        private string SanitizeFilename(string text)
        {
            // Replace invalid characters with underscore
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = text;
            foreach (var c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }
            // Also replace common problematic chars
            sanitized = sanitized.Replace('\\', '_').Replace('/', '_').Replace(':', '_').Replace('*', '_').Replace('?', '_').Replace('"', '_').Replace('<', '_').Replace('>', '_').Replace('|', '_');
            return sanitized;
        }

        public string GetEvalRunResultDirectory(string database, long evalRunId)
        {
            var chatHistoryFolder = _defaultFolders?.ChatHistoryFolder ?? "ChatData";
            var basePath = Path.Combine(AppContext.BaseDirectory, chatHistoryFolder, "eval_runs", database, $"eval_run_{evalRunId}");
            return basePath;
        }

        public string GetEvalCaseResultFilePath(string database, long evalRunId, long evalCaseId, string questionText)
        {
            var dir = GetEvalRunResultDirectory(database, evalRunId);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var sanitized = SanitizeFilename(questionText.Length > 30 ? questionText.Substring(0, 30) : questionText);
            var filename = $"{evalCaseId}_{sanitized}_dbresults.json";
            return Path.Combine(dir, filename);
        }

        public async Task SaveEvalCaseResultAsync(string database, long evalRunId, long evalCaseId, string questionText, object queryResult)
        {
            try
            {
                var filePath = GetEvalCaseResultFilePath(database, evalRunId, evalCaseId, questionText);
                var json = JsonSerializer.Serialize(queryResult, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error saving eval case result: {ex.Message}");
            }
        }

        public async Task<(long? EvalRunId, long? EvalCaseId)> GetOrCreateEvalContextAsync(string userName, string database, string questionText)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                // Get latest running eval_run
                var evalRunId = await conn.QueryFirstOrDefaultAsync<long?>(
                    "SELECT eval_run_id FROM eval_run WHERE username = $User AND database = $Db AND status = 'Running' ORDER BY eval_run_id DESC LIMIT 1",
                    new { User = userName, Db = database });

                if (!evalRunId.HasValue)
                {
                    return (null, null);
                }

                // Find matching eval_case for this question
                var evalCaseId = await conn.QueryFirstOrDefaultAsync<long?>(
                    "SELECT eval_case_id FROM eval_case WHERE eval_run_id = $EvalRunId AND input_text = $InputText ORDER BY eval_case_id DESC LIMIT 1",
                    new { EvalRunId = evalRunId.Value, InputText = questionText });

                if (evalCaseId.HasValue)
                {
                    // Mark case as started
                    await UpdateEvalCaseStartedAsync(evalCaseId.Value);
                }

                return (evalRunId, evalCaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting eval context for user '{userName}': {ex.Message}");
                return (null, null);
            }
        }

        private async Task<long?> GetEvalInferenceIdAsync(SqliteConnection conn, string userName, string database, string questionText, string provider, string model, bool onlyOpen = true)
        {
            var evalRunId = await conn.QueryFirstOrDefaultAsync<long?>(
                "SELECT eval_run_id FROM eval_run WHERE username = $User AND database = $Db ORDER BY eval_run_id DESC LIMIT 1",
                new { User = userName, Db = database });

            if (!evalRunId.HasValue)
                return null;

            var evalCaseId = await conn.QueryFirstOrDefaultAsync<long?>(
                "SELECT eval_case_id FROM eval_case WHERE eval_run_id = $EvalRunId AND input_text = $InputText ORDER BY eval_case_id DESC LIMIT 1",
                new { EvalRunId = evalRunId.Value, InputText = questionText });

            if (!evalCaseId.HasValue)
                return null;

            var sql = @"
SELECT eval_inference_id
FROM eval_case_inference
WHERE eval_case_id = $EvalCaseId
  AND provider_name = $Provider
  AND model_name = $Model";

            if (onlyOpen)
                sql += " AND (completed_at IS NULL OR completed_at = '')";

            sql += " ORDER BY eval_inference_id DESC LIMIT 1";

            return await conn.QueryFirstOrDefaultAsync<long?>(sql, new { EvalCaseId = evalCaseId.Value, Provider = provider, Model = model });
        }

        public async Task<int> UpdateEvalCaseInferenceStartedAsync(string userName, string database, string questionText, string provider, string model)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var evalInferenceId = await GetEvalInferenceIdAsync(conn, userName, database, questionText, provider, model, onlyOpen: true);
            if (!evalInferenceId.HasValue) {
                _logger.LogError($"No matching eval inference found to mark as started for user '{userName}', database '{database}', question '{questionText}', provider '{provider}', model '{model}'");
                return 0;
            }
                

            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var sql = "UPDATE eval_case_inference SET started_at = $StartedAt WHERE eval_inference_id = $Id;";

            return await conn.ExecuteAsync(sql, new { StartedAt = now, Id = evalInferenceId.Value });
        }

        public async Task<int> UpdateEvalCaseInferenceJudgeAsync(string userName, string database, string questionText, string provider, string model, bool match, bool answersQuestion, string judgeConfidence, string judgeReasoning, string differences, string verdict, int wasOverridden)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var evalInferenceId = await GetEvalInferenceIdAsync(conn, userName, database, questionText, provider, model, onlyOpen: false);
            if (!evalInferenceId.HasValue) {
                _logger.LogError($"No matching eval inference found to update judge results for user '{userName}', database '{database}', question '{questionText}', provider '{provider}', model '{model}'");
                return 0;
            }
                

            var sql = @"
UPDATE eval_case_inference
SET ""match"" = $Match,
    answers_question = $AnswersQuestion,
    judge_confidence = $JudgeConfidence,
    judge_reasoning = $JudgeReasoning,
    differences = $Differences,
    verdict = $Verdict,
    was_overridden = $WasOverridden
WHERE eval_inference_id = $Id;";

            return await conn.ExecuteAsync(sql, new
            {
                Match = match ? 1 : 0,
                AnswersQuestion = answersQuestion ? 1 : 0,
                JudgeConfidence = judgeConfidence ?? string.Empty,
                JudgeReasoning = judgeReasoning ?? string.Empty,
                Differences = differences ?? string.Empty,
                Verdict = verdict ?? string.Empty,
                WasOverridden = wasOverridden,
                Id = evalInferenceId.Value
            });
        }

        // Update inference row by using the assistant message id to correlate
        public async Task<int> UpdateInferenceFromAssistantMessageAsync(string userName, string database, long assistantMessageId, EvalInferenceResult result)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                // Get assistant message row (includes session and provider/model if present)
                var msg = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT Id, SessionId, Role, MessageText, LlmProvider, LlmModel FROM ChatMessage WHERE Id = $Id",
                    new { Id = assistantMessageId });

                if (msg == null) return 0;

                long sessionId = (long)msg.SessionId;
                string llmProvider = msg.LlmProvider ?? string.Empty;
                string llmModel = msg.LlmModel ?? string.Empty;

                // Get the previous user message in the same session
                var userMsg = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT Id, MessageText FROM ChatMessage WHERE SessionId = $SessionId AND Role = 'User' AND Id < $Id ORDER BY Id DESC LIMIT 1",
                    new { SessionId = sessionId, Id = assistantMessageId });

                if (userMsg == null) {
                    _logger.LogError($"No user message found in session {sessionId} before assistant message {assistantMessageId}");
                    return 0;
                }

                string inputText = userMsg.MessageText ?? string.Empty;

                // Get message extras (sql, data file)
                var extras = await _chatRepo.GetMessageExtrasAsync(assistantMessageId, userName);

                string? generatedSql = extras?.SqlText;
                string? inferenceResultFile = extras?.DataFileName;

                // Find latest running eval_run for user+database
                var evalRunId = await conn.QueryFirstOrDefaultAsync<long?>(
                    "SELECT eval_run_id FROM eval_run WHERE username = $User AND database = $Db AND status = 'Running' ORDER BY eval_run_id DESC LIMIT 1",
                    new { User = userName, Db = database });

                if (!evalRunId.HasValue) {
                    _logger.LogError($"No running eval run found for user '{userName}', database '{database}'");
                    return 0;
                }

                // Find eval_case for this input_text
                var evalCaseId = await conn.QueryFirstOrDefaultAsync<long?>(
                    "SELECT eval_case_id FROM eval_case WHERE eval_run_id = $EvalRunId AND input_text = $InputText ORDER BY eval_case_id DESC LIMIT 1",
                    new { EvalRunId = evalRunId.Value, InputText = inputText });

                if (!evalCaseId.HasValue) {
                    _logger.LogError($"No eval case found for eval run {evalRunId.Value} and input text '{inputText}'");
                    return 0;
                }

                // Find matching inference row
                var evalInferenceId = await conn.QueryFirstOrDefaultAsync<long?>(
                    "SELECT eval_inference_id FROM eval_case_inference WHERE eval_case_id = $EvalCaseId AND provider_name = $Provider AND model_name = $Model AND (completed_at IS NULL OR completed_at = '') ORDER BY eval_inference_id DESC LIMIT 1",
                    new { EvalCaseId = evalCaseId.Value, Provider = llmProvider, Model = llmModel });

                if (!evalInferenceId.HasValue) {
                    _logger.LogError($"No matching eval inference found for eval case {evalCaseId.Value}, provider '{llmProvider}', model '{llmModel}' to update with assistant message {assistantMessageId}");
                    return 0;
                }

                var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                var updateSql = @"
UPDATE eval_case_inference
SET generated_sql = $GeneratedSql,
    inference_result_file = $InferenceResultFile,
    latency_ms = $LatencyMs,
    prompt_tokens = $PromptTokens,
    completion_tokens = $CompletionTokens,
    verdict = $Verdict,
    completed_at = $CompletedAt
WHERE eval_inference_id = $Id;
";

                var rows = await conn.ExecuteAsync(updateSql, new
                {
                    GeneratedSql = generatedSql ?? string.Empty,
                    InferenceResultFile = inferenceResultFile ?? string.Empty,
                    LatencyMs = result?.LatencyMs,
                    PromptTokens = result?.PromptTokens,
                    CompletionTokens = result?.CompletionTokens,
                    Verdict = result?.Verdict ?? string.Empty,
                    CompletedAt = now,
                    Id = evalInferenceId.Value
                });

                return rows;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    $"Error in UpdateInferenceFromAssistantMessageAsync for user '{userName}', database "+
                    $"'{database}' for messageid {assistantMessageId} : {ex}");
                return 0;
            }
        }
    }
}
