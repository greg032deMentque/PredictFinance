using BackPredictFinance.Services.ClientFinanceServices.Tax;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.ClientFinance
{
    [Authorize(Policy = "Bearer")]
    [Route("api/ClientFinance")]
    [ApiController]
    public class ClientFinanceTaxController(ITaxService taxService) : ControllerBase
    {
        [HttpGet("tax-summary")]
        public async Task<IActionResult> GetTaxSummary([FromQuery] int year, CancellationToken ct)
        {
            if (year < 2000 || year > 2100)
                return BadRequest("Année invalide.");

            var result = await taxService.GetAllPortfoliosTaxSummaryAsync(year, ct);
            return Ok(result);
        }
    }
}
