using AutoMapper;
using BackPredictFinance.Common.Fundamentals;
using BackPredictFinance.Services.ClientFinanceServices.Fundamentals;
using BackPredictFinance.Services.Fundamentals;
using BackPredictFinance.ViewModels.Fundamentals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.ClientFinance
{
    [Authorize(Policy = "Bearer")]
    [Route("api/ClientFinance")]
    [ApiController]
    public sealed class ClientFinanceFundamentalsController(
        IClientFinanceFundamentalsService fundamentalsService,
        IFundamentalScoringService fundamentalScoringService,
        IMapper mapper) : ControllerBase
    {
        [HttpGet("instruments/{symbol}/fundamentals")]
        public async Task<IActionResult> GetFundamentals([FromRoute] string symbol, CancellationToken ct)
        {
            var result = await fundamentalsService.GetFundamentalsAsync(symbol, ct);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("fundamentals/score")]
        public async Task<IActionResult> Score([FromBody] FundamentalScoreRequestViewModel model, CancellationToken ct)
        {
            var response = await fundamentalScoringService.ScoreAsync(new FundamentalScoreRequest
            {
                UniverseId = model.UniverseId,
                Symbols = model.Symbols,
                MinCategoriesRequired = model.MinCategoriesRequired,
                CoveragePenaltyEnabled = model.CoveragePenaltyEnabled,
                IncludeRankPosition = model.IncludeRankPosition
            }, ct);

            return Ok(mapper.Map<FundamentalScoreResponseViewModel>(response));
        }
    }
}
