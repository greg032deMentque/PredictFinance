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
    public class TickersController : ControllerBase
    {
        private readonly ITickerService _tickerService;

        public TickersController(ITickerService tickerService)
            => _tickerService = tickerService;

        [HttpGet("Exchanges")]
        public async Task<IActionResult> GetExchanges()
            => Ok(await _tickerService.GetExchangesAsync());

        [HttpGet("GetSymbols")]
        public async Task<IActionResult> GetSymbols(string exchange)
            => Ok(await _tickerService.GetSymbolsByExchangeAsync(exchange));

        [HttpGet("GetAllSymbols")]
        public async Task<IActionResult> GetAllSymbols()
            => Ok(await _tickerService.GetAllSymbolsAsync());

        [HttpGet("GetTimeSeries")]
        public async Task<IActionResult> GetTimeSeries( string symbol,[FromQuery] string interval = "1day", [FromQuery] int outputSize = 100)
        {
            var data = await _tickerService.GetTimeSeriesAsync(symbol, interval, outputSize);
            return Ok(data);
        }
    }

}
