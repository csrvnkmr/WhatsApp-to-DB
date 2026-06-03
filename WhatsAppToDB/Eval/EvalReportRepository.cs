using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;
using WhatsAppToDB.Services;
using WhatsAppToDB.Settings;

namespace WhatsAppToDB.Eval
{
    public class EvalCaseDetailDto
    {
        public long CaseId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string GroundTruthSql { get; set; } = string.Empty;
        public string DatabaseResult { get; set; } = string.Empty;
        public List<EvalInferenceDetailDto> Runs { get; set; } = new();
    }

    public class EvalInferenceDetailDto
    {
        public string ModelName { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string LlmResult { get; set; } = string.Empty;
        public string Comparison { get; set; } = string.Empty;
        public int AnswersQuestion { get; set; }
        public string Difference { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class EvalRunSummaryDto
    {
        public long RunId { get; set; }
        public string Database { get; set; } = string.Empty;
        public string StartedAt { get; set; } = string.Empty;
        public string EndedAt { get; set; } = string.Empty;
        public int TotalCases { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public int ErrorCount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class EvalRunVsRunDto
    {
        public string ModuleName { get; set; } = string.Empty;
        public string InputText { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string VerdictRunA { get; set; } = string.Empty;
        public string VerdictRunB { get; set; } = string.Empty;
    }

    public class EvalModulePerformanceDto
    {
        public string ModuleName { get; set; } = string.Empty;
        public int TotalCases { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Errors { get; set; }
        public double PassRatePct { get; set; }
    }

    public class EvalLlmPerformanceDto
    {
        public string ProviderName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public int TotalCases { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Errors { get; set; }
        public double PassRatePct { get; set; }
        public double AvgLatencyMs { get; set; }
        public long TotalPromptTokens { get; set; }
        public long TotalCompletionTokens { get; set; }
    }

    public class EvalCaseDetailQueryDto
    {
        public int Sequence { get; set; }
        public string Module { get; set; } = string.Empty;
        public string InputText { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string Verdict { get; set; } = string.Empty;
        public string JudgeConfidence { get; set; } = string.Empty;
        public int WasOverridden { get; set; }
        public int? AnswersQuestion { get; set; }
        public string JudgeReasoning { get; set; } = string.Empty;
        public string Differences { get; set; } = string.Empty;
        public string GeneratedSql { get; set; } = string.Empty;
        public int? LatencyMs { get; set; }
        public string StartedAt { get; set; } = string.Empty;
        public string CompletedAt { get; set; } = string.Empty;
    }

    public class EvalPassTrendDto
    {
        public long EvalRunId { get; set; }
        public string Database { get; set; } = string.Empty;
        public string StartedAt { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public int TotalCases { get; set; }
        public double PassRatePct { get; set; }
    }

    public class EvalReportRepository
    {
        private readonly string _connectionString;
        private readonly FolderUtils _folderUtils;
        private readonly ILogger _logger;
        private readonly ChatDbRepository _chatRepo;
        private readonly DefaultFolders _defaultFolders;

        public EvalReportRepository(FolderUtils folderUtils, ILogger logger, ChatDbRepository chatRepo, JsonConfigService jsonConfigService)
        {
            _folderUtils = folderUtils;
            _logger = logger ?? new AppLogger();
            _chatRepo = chatRepo;
            _defaultFolders = jsonConfigService.GetDefaultFolders();

            var dbPath = _folderUtils.GetChatHistoryDBPath();
            _connectionString = $"Data Source={dbPath}";
        }

        private SqliteConnection GetConnection() => new SqliteConnection(_connectionString);

        private async Task<string> SafeReadFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return string.Empty;
            try
            {
                string fullPath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(AppContext.BaseDirectory, filePath);
                if (File.Exists(fullPath))
                {
                    return await File.ReadAllTextAsync(fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EvalReportRepository] Error reading result file {filePath}: {ex.Message}");
            }
            return string.Empty;
        }

        public async Task<List<EvalCaseDetailDto>> GetRunDetailsAsync(long runId)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                string casesSql = @"
                    SELECT 
                        eval_case_id as CaseId,
                        module_name as Module,
                        input_text as Question,
                        ground_truth_sql as GroundTruthSql,
                        ground_truth_result_file as GroundTruthResultFile
                    FROM eval_case
                    WHERE eval_run_id = @RunId
                    ORDER BY sequence ASC";

                var cases = (await conn.QueryAsync<dynamic>(casesSql, new { RunId = runId })).ToList();
                var result = new List<EvalCaseDetailDto>();

                foreach (var c in cases)
                {
                    long caseId = c.CaseId;
                    string dbResultFilePath = c.GroundTruthResultFile ?? string.Empty;
                    string dbResultJson = await SafeReadFileAsync(dbResultFilePath);

                    var caseDto = new EvalCaseDetailDto
                    {
                        CaseId = caseId,
                        Question = c.Question ?? string.Empty,
                        Module = c.Module ?? string.Empty,
                        GroundTruthSql = c.GroundTruthSql ?? string.Empty,
                        DatabaseResult = dbResultJson,
                        Runs = new List<EvalInferenceDetailDto>()
                    };

                    string infSql = @"
                        SELECT 
                            provider_name as ProviderName,
                            model_name as ModelName,
                            generated_sql as GeneratedSql,
                            inference_result_file as InferenceResultFile,
                            verdict as Comparison,
                            answers_question as AnswersQuestion,
                            differences as Difference,
                            judge_reasoning as Reason
                        FROM eval_case_inference
                        WHERE eval_case_id = @CaseId
                        ORDER BY eval_inference_id ASC";

                    var infs = (await conn.QueryAsync<dynamic>(infSql, new { CaseId = caseId })).ToList();

                    foreach (var inf in infs)
                    {
                        string infResultFile = inf.InferenceResultFile ?? string.Empty;
                        string llmResultJson = string.Empty;
                        if (!string.IsNullOrWhiteSpace(infResultFile))
                        {
                            string fullInfPath = Path.Combine(AppContext.BaseDirectory, "Data", "Results", infResultFile);
                            llmResultJson = await SafeReadFileAsync(fullInfPath);
                        }

                        caseDto.Runs.Add(new EvalInferenceDetailDto
                        {
                            ModelName = inf.ModelName ?? string.Empty,
                            ProviderName = inf.ProviderName ?? string.Empty,
                            LlmResult = llmResultJson,
                            Comparison = inf.Comparison ?? "Failed",
                            AnswersQuestion = Convert.ToInt32(inf.AnswersQuestion ?? 0),
                            Difference = inf.Difference ?? string.Empty,
                            Reason = inf.Reason ?? string.Empty
                        });
                    }

                    result.Add(caseDto);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EvalReportRepository] Error fetching run details: {ex.Message}");
                throw;
            }
        }
        public async Task<(dynamic Summary, List<EvalRunSummaryDto> Runs)> GetRunsDashboardDataAsync(string? database, string? status)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                var conditions = new List<string>();
                var parameters = new DynamicParameters();

                if (!string.IsNullOrWhiteSpace(database))
                {
                    conditions.Add("database = @Database");
                    parameters.Add("Database", database);
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    conditions.Add("status = @Status");
                    parameters.Add("Status", status);
                }

                string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

                // Query the list of runs
                string runsSql = $@"
                    SELECT 
                        eval_run_id as RunId,
                        database as Database,
                        started_at as StartedAt,
                        completed_at as EndedAt,
                        total_cases as TotalCases,
                        passed_count as PassCount,
                        failed_count as FailCount,
                        error_count as ErrorCount,
                        status as Status
                    FROM eval_run
                    {whereClause}
                    ORDER BY eval_run_id DESC";

                var runsList = (await conn.QueryAsync<EvalRunSummaryDto>(runsSql, parameters)).ToList();

                // Query summary metrics
                string summarySql = $@"
                    SELECT
                        COUNT(*) as TotalRuns,
                        SUM(CASE WHEN status = 'Completed' AND COALESCE(failed_count, 0) = 0 AND COALESCE(error_count, 0) = 0 THEN 1 ELSE 0 END) as SuccessfulRuns,
                        SUM(CASE WHEN (COALESCE(failed_count, 0) > 0 OR COALESCE(error_count, 0) > 0 OR status = 'Aborted' OR status = 'Running') THEN 1 ELSE 0 END) as FailedRuns,
                        SUM(COALESCE(total_cases, 0)) as TotalCases,
                        SUM(COALESCE(passed_count, 0)) as TotalSuccessfulInferences,
                        SUM(COALESCE(failed_count, 0) + COALESCE(error_count, 0)) as TotalFailedInferences
                    FROM eval_run
                    {whereClause}";

                var summaryRow = await conn.QueryFirstOrDefaultAsync<dynamic>(summarySql, parameters);

                return (summaryRow ?? (dynamic)new System.Dynamic.ExpandoObject(), runsList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EvalReportRepository] Error fetching dashboard data: {ex.Message}");
                throw;
            }
        }

        public async Task<List<EvalRunVsRunDto>> GetRunVsRunAsync(long runIdA, long runIdB)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                string compareSql = @"
                    SELECT 
                        c.module_name AS ModuleName, 
                        c.input_text AS InputText, 
                        i.provider_name AS ProviderName, 
                        i.model_name AS ModelName, 
                        MAX(CASE WHEN c.eval_run_id = @RunA THEN i.verdict END) AS VerdictRunA, 
                        MAX(CASE WHEN c.eval_run_id = @RunB THEN i.verdict END) AS VerdictRunB
                    FROM eval_case c 
                    JOIN eval_case_inference i ON i.eval_case_id = c.eval_case_id 
                    WHERE c.eval_run_id IN (@RunA, @RunB) 
                    GROUP BY c.module_name, c.input_text, i.provider_name, i.model_name 
                    ORDER BY c.module_name";

                var results = (await conn.QueryAsync<EvalRunVsRunDto>(compareSql, new { RunA = runIdA, RunB = runIdB })).ToList();
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EvalReportRepository] Error running GetRunVsRun comparison for {runIdA} vs {runIdB}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<EvalModulePerformanceDto>> GetModulePerformanceAsync(List<long> runIds)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                string sql = @"
                    SELECT
                        c.module_name AS ModuleName,
                        COUNT(*) AS TotalCases,
                        SUM(CASE WHEN i.verdict = 'Pass' THEN 1 ELSE 0 END) AS Passed,
                        SUM(CASE WHEN i.verdict = 'Fail' THEN 1 ELSE 0 END) AS Failed,
                        SUM(CASE WHEN i.verdict = 'Error' THEN 1 ELSE 0 END) AS Errors,
                        ROUND(SUM(CASE WHEN i.verdict = 'Pass' THEN 1 ELSE 0 END)
                              * 100.0 / NULLIF(COUNT(*), 0), 1) AS PassRatePct
                    FROM eval_case c
                    JOIN eval_case_inference i ON i.eval_case_id = c.eval_case_id
                    WHERE c.eval_run_id IN @RunIds
                    GROUP BY c.module_name
                    ORDER BY PassRatePct ASC";

                var results = (await conn.QueryAsync<EvalModulePerformanceDto>(sql, new { RunIds = runIds })).ToList();
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EvalReportRepository] Error running GetModulePerformance for {string.Join(",", runIds)}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<EvalLlmPerformanceDto>> GetLlmPerformanceAsync(List<long> runIds)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                string sql = @"
                    SELECT
                        i.provider_name AS ProviderName,
                        i.model_name AS ModelName,
                        COUNT(*) AS TotalCases,
                        SUM(CASE WHEN i.verdict = 'Pass' THEN 1 ELSE 0 END) AS Passed,
                        SUM(CASE WHEN i.verdict = 'Fail' THEN 1 ELSE 0 END) AS Failed,
                        SUM(CASE WHEN i.verdict = 'Error' THEN 1 ELSE 0 END) AS Errors,
                        ROUND(SUM(CASE WHEN i.verdict = 'Pass' THEN 1 ELSE 0 END)
                              * 100.0 / NULLIF(COUNT(*), 0), 1) AS PassRatePct,
                        ROUND(AVG(i.latency_ms), 0) AS AvgLatencyMs,
                        SUM(i.prompt_tokens) AS TotalPromptTokens,
                        SUM(i.completion_tokens) AS TotalCompletionTokens
                    FROM eval_case_inference i
                    JOIN eval_case c ON c.eval_case_id = i.eval_case_id
                    WHERE c.eval_run_id IN @RunIds
                    GROUP BY i.provider_name, i.model_name
                    ORDER BY PassRatePct DESC";

                var results = (await conn.QueryAsync<EvalLlmPerformanceDto>(sql, new { RunIds = runIds })).ToList();
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EvalReportRepository] Error running GetLlmPerformance for {string.Join(",", runIds)}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<EvalCaseDetailQueryDto>> GetCaseDetailsAsync(List<long> runIds)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                string sql = @"
                    SELECT
                        c.sequence AS Sequence,
                        c.module AS Module,
                        c.input_text AS InputText,
                        i.provider_name AS ProviderName,
                        i.model_name AS ModelName,
                        i.verdict AS Verdict,
                        i.judge_confidence AS JudgeConfidence,
                        i.was_overridden AS WasOverridden,
                        i.answers_question AS AnswersQuestion,
                        i.judge_reasoning AS JudgeReasoning,
                        i.differences AS Differences,
                        i.generated_sql AS GeneratedSql,
                        i.latency_ms AS LatencyMs,
                        i.started_at AS StartedAt,
                        i.completed_at AS CompletedAt
                    FROM eval_case c
                    JOIN eval_case_inference i ON i.eval_case_id = c.eval_case_id
                    WHERE c.eval_run_id IN @RunIds
                    ORDER BY c.sequence, i.provider_name";

                var results = (await conn.QueryAsync<EvalCaseDetailQueryDto>(sql, new { RunIds = runIds })).ToList();
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EvalReportRepository] Error running GetCaseDetails for {string.Join(",", runIds)}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<EvalCaseDetailQueryDto>> GetFailureAnalysisAsync(List<long> runIds)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                string sql = @"
                    SELECT
                        c.sequence AS Sequence,
                        c.module AS Module,
                        c.input_text AS InputText,
                        i.provider_name AS ProviderName,
                        i.model_name AS ModelName,
                        i.verdict AS Verdict,
                        i.judge_confidence AS JudgeConfidence,
                        i.was_overridden AS WasOverridden,
                        i.answers_question AS AnswersQuestion,
                        i.judge_reasoning AS JudgeReasoning,
                        i.differences AS Differences,
                        i.generated_sql AS GeneratedSql,
                        i.latency_ms AS LatencyMs,
                        i.started_at AS StartedAt,
                        i.completed_at AS CompletedAt
                    FROM eval_case c
                    JOIN eval_case_inference i ON i.eval_case_id = c.eval_case_id
                    WHERE c.eval_run_id IN @RunIds
                      AND i.verdict IN ('Fail', 'Error')
                    ORDER BY c.sequence, i.provider_name";

                var results = (await conn.QueryAsync<EvalCaseDetailQueryDto>(sql, new { RunIds = runIds })).ToList();
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EvalReportRepository] Error running GetFailureAnalysis for {string.Join(",", runIds)}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<EvalPassTrendDto>> GetPassTrendAsync(List<long> runIds)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                string sql = @"
                    SELECT
                        r.eval_run_id AS EvalRunId,
                        r.database AS Database,
                        r.started_at AS StartedAt,
                        c.module AS Module,
                        i.provider_name AS ProviderName,
                        i.model_name AS ModelName,
                        COUNT(*) AS TotalCases,
                        ROUND(SUM(CASE WHEN i.verdict = 'Pass' THEN 1 ELSE 0 END)
                              * 100.0 / NULLIF(COUNT(*), 0), 1) AS PassRatePct
                    FROM eval_run r
                    JOIN eval_case c       ON c.eval_run_id      = r.eval_run_id
                    JOIN eval_case_inference i ON i.eval_case_id = c.eval_case_id 
                    WHERE r.eval_run_id IN @RunIds
                    GROUP BY r.eval_run_id, r.database, r.started_at, c.module,
                             i.provider_name, i.model_name
                    ORDER BY r.started_at, c.module";

                var results = (await conn.QueryAsync<EvalPassTrendDto>(sql, new { RunIds = runIds })).ToList();
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EvalReportRepository] Error running GetPassTrend for {string.Join(",", runIds)}: {ex.Message}");
                throw;
            }
        }
    }
}
