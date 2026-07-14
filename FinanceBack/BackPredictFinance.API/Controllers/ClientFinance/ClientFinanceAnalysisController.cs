using BackPredictFinance.Patterns;
using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.Services.ClientFinanceServices.Patterns;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Patterns;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Simulation;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Snapshots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.ClientFinance
{
    [Authorize(Policy = "Bearer")]
    [Route("api/ClientFinance")]
    [ApiController]
    public class ClientFinanceAnalysisController(
        IClientFinanceService clientFinanceService,
        IClientFinanceDashboardHistoryService dashboardHistoryService,
        IClientFinanceSnapshotComparisonService snapshotComparisonService,
        IClientFinanceParameterDetailService parameterDetailService,
        IPatternExplorerService patternExplorerService) : ControllerBase
    {
        [HttpGet("patterns/catalog")]
        public IActionResult GetPatternCatalog()
        {
            var payload = PatternCatalog.GetTargetPatterns()
                .Select(pattern => new PatternCatalogViewModel
                {
                    Id = pattern.PatternId,
                    Label = pattern.DisplayName,
                    Family = pattern.Family,
                    Description = pattern.Description,
                    Direction = pattern.Direction
                })
                .ToList();

            return Ok(payload);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(CancellationToken ct)
        {
            return Ok(await dashboardHistoryService.GetDashboardAsync(ct));
        }

        [HttpGet("analysis/{analysisId}")]
        public async Task<IActionResult> GetAnalysisDetail([FromRoute] string analysisId, CancellationToken ct)
        {
            var payload = await dashboardHistoryService.GetAnalysisDetailAsync(analysisId, ct);
            return payload is null ? NotFound() : Ok(payload);
        }

        [HttpPost("analysis/run")]
        public async Task<IActionResult> RunAnalysis([FromBody] AnalysisRunRequestViewModel model, CancellationToken ct)
        {
            return Ok(await clientFinanceService.RunAnalysisAsync(model, ct));
        }

        [HttpGet("analysis/recent")]
        public async Task<IActionResult> GetRecentAnalyses([FromQuery] int take = 10, CancellationToken ct = default)
        {
            return Ok(await dashboardHistoryService.GetRecentAnalysesAsync(take, ct));
        }

        [HttpPost("snapshots/compare")]
        public async Task<IActionResult> CompareSnapshots([FromBody] SnapshotComparisonRequestViewModel model, CancellationToken ct)
        {
            var payload = await snapshotComparisonService.CompareAsync(model, ct);
            return payload is null ? NotFound() : Ok(payload);
        }

        [HttpPost("simulation/run")]
        public async Task<IActionResult> RunSimulation([FromBody] SimulationRequestViewModel model, CancellationToken ct)
        {
            return Ok(await clientFinanceService.RunSimulationAsync(model, ct));
        }

        [HttpGet("parameters/{analysisId}/{parameterId}")]
        public async Task<IActionResult> GetParameterDetail(
            [FromRoute] string analysisId,
            [FromRoute] string parameterId,
            CancellationToken ct)
        {
            var payload = await parameterDetailService.GetParameterDetailAsync(analysisId, parameterId, ct);
            return payload is null ? NotFound() : Ok(payload);
        }

        [HttpPost("patterns/evaluate")]
        public async Task<IActionResult> EvaluatePatterns([FromBody] PatternEvaluateRequestViewModel model, CancellationToken ct)
        {
            return Ok(await patternExplorerService.EvaluateAsync(model, ct));
        }

        [HttpGet("patterns/{analysisId}/{patternId}")]
        public async Task<IActionResult> GetPatternDetail([FromRoute] string analysisId, [FromRoute] string patternId, [FromQuery] bool holds, CancellationToken ct)
        {
            var detail = await patternExplorerService.GetPatternDetailAsync(analysisId, patternId, holds, ct);
            return detail is null ? NotFound() : Ok(detail);
        }
    }
}
