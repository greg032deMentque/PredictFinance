using BackPredictFinance.Services.PythonServices;
using BackPredictFinance.Services.PythonServices.Models;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.Services.UserServices;
using BackPredictFinance.ViewModels.CommonViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BackPredictFinance.API.Controllers
{
    [Authorize(Policy = "Bearer")]
    [Route("api/[controller]")]
    [ApiController]
    public class TradingController : ControllerBase
    {
        private readonly IPythonApiService _py;
        public TradingController(IPythonApiService py) => _py = py;

        [HttpPost("predict")]
        public async Task<IActionResult> Predict([FromBody] AssetIn asset)
            => Ok(await _py.PredictAsync(asset));

        [HttpGet("predict/{symbol}")]
        public async Task<IActionResult> PredictBySymbol([FromRoute] string symbol)
            => Ok(await _py.PredictAsync(new AssetIn { Symbol = symbol }));

        [HttpPost("recommend")]
        public async Task<IActionResult> Recommend([FromBody] RecommendationIn rec)
            => Ok(await _py.RecommendAsync(rec));
    }
}
