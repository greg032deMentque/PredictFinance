using BackPredictFinance.Common.Fundamentals;
using AutoMapper;
using BackPredictFinance.Services.Fundamentals;
using BackPredictFinance.ViewModels.Fundamentals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers
{
    [Authorize(Policy = "Bearer")]
    [Route("api/[controller]")]
    [ApiController]
    public class FundamentalsController : ControllerBase
    {
        private readonly IFundamentalScoringService _fundamentalScoringService;
        private readonly IMapper _mapper;

        public FundamentalsController(IFundamentalScoringService fundamentalScoringService, IMapper mapper)
        {
            _fundamentalScoringService = fundamentalScoringService;
            _mapper = mapper;
        }

        [HttpPost("score")]
        public async Task<IActionResult> Score([FromBody] FundamentalScoreRequestViewModel model, CancellationToken ct)
        {
            var response = await _fundamentalScoringService.ScoreAsync(new FundamentalScoreRequest
            {
                UniverseId = model.UniverseId,
                Symbols = model.Symbols,
                MinCategoriesRequired = model.MinCategoriesRequired,
                CoveragePenaltyEnabled = model.CoveragePenaltyEnabled,
                IncludeRankPosition = model.IncludeRankPosition
            }, ct);

            return Ok(_mapper.Map<FundamentalScoreResponseViewModel>(response));
        }
    }
}
