using BackPredictFinance.Services.ClientFinanceServices.Indicators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.ClientFinance
{
    [Authorize(Policy = "Bearer")]
    [Route("api/ClientFinance")]
    [ApiController]
    public class ClientFinanceTechnicalIndicatorsController(
        ITechnicalIndicatorsService indicatorsService) : ControllerBase
    {
        [HttpGet("indicators/{symbol}")]
        public async Task<IActionResult> GetIndicators([FromRoute] string symbol, CancellationToken ct)
        {
            var result = await indicatorsService.GetIndicatorsAsync(symbol, ct);
            if (result == null) return NotFound();
            return Ok(result);
        }
    }
}
