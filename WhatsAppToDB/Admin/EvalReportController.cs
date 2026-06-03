using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WhatsAppToDB.Services;
using WhatsAppToDB.Eval;
using WhatsAppToDB.Constants;
using WhatsAppToDB.Abstractions;

namespace WhatsAppToDB.Admin
{
    [ApiController]
    [Route("admin/api/evalreport")]
    public class EvalReportController : ControllerBase
    {
        private readonly EvalReportRepository _reportRepo;
        private readonly ILogger _logger;

        public EvalReportController(
            EvalReportRepository reportRepo,
            ILogger logger)
        {
            _reportRepo = reportRepo;
            _logger = logger;
        }

        [HttpGet("/eval/api/dashboard/runs")]
        public async Task<IActionResult> GetDashboardRuns([FromQuery] string? database, [FromQuery] string? status)
        {
            try
            {
                var userToken = HttpContext.Items[Constants.ContextItems.Token]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(userToken))
                {
                    return Unauthorized();
                }

                var (summary, runs) = await _reportRepo.GetRunsDashboardDataAsync(database, status);

                long totalRuns = summary?.TotalRuns ?? 0;
                long successfulRuns = summary?.SuccessfulRuns ?? 0;
                long failedRuns = summary?.FailedRuns ?? 0;
                double failureRate = totalRuns > 0 ? (double)failedRuns / totalRuns * 100.0 : 0.0;

                long totalCases = summary?.TotalCases ?? 0;
                long totalSuccessfulInferences = summary?.TotalSuccessfulInferences ?? 0;
                long totalFailedInferences = summary?.TotalFailedInferences ?? 0;
                double failureRateInferences = (totalSuccessfulInferences + totalFailedInferences) > 0 
                    ? (double)totalFailedInferences / (totalSuccessfulInferences + totalFailedInferences) * 100.0 
                    : 0.0;

                return Ok(new
                {
                    summary = new
                    {
                        totalRuns,
                        successfulRuns,
                        failedRuns,
                        failureRate = Math.Round(failureRate, 2),
                        totalCases,
                        totalSuccessfulInferences,
                        totalFailedInferences,
                        failureRateInferences = Math.Round(failureRateInferences, 2)
                    },
                    runs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetDashboardRuns: {ex}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("/eval/api/dashboard/run/{runId}")]
        public async Task<IActionResult> GetDashboardRunDetails(long runId)
        {
            try
            {
                var userToken = HttpContext.Items[Constants.ContextItems.Token]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(userToken))
                {
                    return Unauthorized();
                }

                var cases = await _reportRepo.GetRunDetailsAsync(runId);
                return Ok(cases);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetDashboardRunDetails for run {runId}: {ex}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("/eval/api/dashboard/runvsrun")]
        public async Task<IActionResult> GetRunVsRunComparison([FromQuery] long runId1, [FromQuery] long runId2)
        {
            try
            {
                var userToken = HttpContext.Items[Constants.ContextItems.Token]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(userToken))
                {
                    return Unauthorized();
                }

                var comparison = await _reportRepo.GetRunVsRunAsync(runId1, runId2);
                return Ok(comparison);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetRunVsRunComparison for runs {runId1} and {runId2}: {ex}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("/eval/api/dashboard/moduleperformance")]
        public async Task<IActionResult> GetModulePerformance([FromQuery] List<long> runIds)
        {
            try
            {
                var userToken = HttpContext.Items[Constants.ContextItems.Token]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(userToken))
                {
                    return Unauthorized();
                }

                if (runIds == null || runIds.Count == 0)
                {
                    return BadRequest(new { error = "At least one run ID must be provided." });
                }

                var performance = await _reportRepo.GetModulePerformanceAsync(runIds);
                return Ok(performance);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetModulePerformance for runs {string.Join(",", runIds ?? new())}: {ex}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("/eval/api/dashboard/llmperformance")]
        public async Task<IActionResult> GetLlmPerformance([FromQuery] List<long> runIds)
        {
            try
            {
                var userToken = HttpContext.Items[Constants.ContextItems.Token]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(userToken))
                {
                    return Unauthorized();
                }

                if (runIds == null || runIds.Count == 0)
                {
                    return BadRequest(new { error = "At least one run ID must be provided." });
                }

                var performance = await _reportRepo.GetLlmPerformanceAsync(runIds);
                return Ok(performance);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetLlmPerformance for runs {string.Join(",", runIds ?? new())}: {ex}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("/eval/api/dashboard/casedetails")]
        public async Task<IActionResult> GetCaseDetails([FromQuery] List<long> runIds)
        {
            try
            {
                var userToken = HttpContext.Items[Constants.ContextItems.Token]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(userToken))
                {
                    return Unauthorized();
                }

                if (runIds == null || runIds.Count == 0)
                {
                    return BadRequest(new { error = "At least one run ID must be provided." });
                }

                var details = await _reportRepo.GetCaseDetailsAsync(runIds);
                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCaseDetails for runs {string.Join(",", runIds ?? new())}: {ex}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("/eval/api/dashboard/failureanalysis")]
        public async Task<IActionResult> GetFailureAnalysis([FromQuery] List<long> runIds)
        {
            try
            {
                var userToken = HttpContext.Items[Constants.ContextItems.Token]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(userToken))
                {
                    return Unauthorized();
                }

                if (runIds == null || runIds.Count == 0)
                {
                    return BadRequest(new { error = "At least one run ID must be provided." });
                }

                var details = await _reportRepo.GetFailureAnalysisAsync(runIds);
                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetFailureAnalysis for runs {string.Join(",", runIds ?? new())}: {ex}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("/eval/api/dashboard/passtrend")]
        public async Task<IActionResult> GetPassTrend([FromQuery] List<long> runIds)
        {
            try
            {
                var userToken = HttpContext.Items[Constants.ContextItems.Token]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(userToken))
                {
                    return Unauthorized();
                }

                if (runIds == null || runIds.Count == 0)
                {
                    return BadRequest(new { error = "At least one run ID must be provided." });
                }

                var trend = await _reportRepo.GetPassTrendAsync(runIds);
                return Ok(trend);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetPassTrend for runs {string.Join(",", runIds ?? new())}: {ex}");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
