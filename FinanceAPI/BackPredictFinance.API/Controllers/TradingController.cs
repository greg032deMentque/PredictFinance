using BackPredictFinance.Services.PythonServices;
using BackPredictFinance.Services.PythonServices.Models;
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
    }
}
