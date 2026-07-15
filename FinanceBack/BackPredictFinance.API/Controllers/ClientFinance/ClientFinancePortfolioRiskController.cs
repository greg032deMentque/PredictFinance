using BackPredictFinance.Services.ClientFinanceServices.PortfolioMetrics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.ClientFinance
{
    [Authorize(Policy = "Bearer")]
    [Route("api/ClientFinance")]
    [ApiController]
    public class ClientFinancePortfolioRiskController(
        IPortfolioRiskMetricsService portfolioRiskMetricsService) : ControllerBase
    {
        [HttpGet("portfolio/{portfolioId}/risk-metrics")]
        public async Task<IActionResult> GetRiskMetrics([FromRoute] string portfolioId, CancellationToken ct)
        {
            var result = await portfolioRiskMetricsService.GetMetricsAsync(portfolioId, ct);
            if (result == null) return NotFound();
            return Ok(result);
        }
    }
}
