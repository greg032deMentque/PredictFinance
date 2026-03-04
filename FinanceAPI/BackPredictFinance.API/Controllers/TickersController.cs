using BackPredictFinance.Services.TwelveDataServices;
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

        [HttpGet("exchanges")]
        public async Task<IActionResult> GetExchanges()
            => Ok(await _tickerService.GetExchangesAsync());

        [HttpGet("symbols")]
        public async Task<IActionResult> GetSymbols([FromQuery] string exchange)
            => Ok(await _tickerService.GetSymbolsByExchangeAsync(exchange));

        [HttpGet("symbols/all")]
        public async Task<IActionResult> GetAllSymbols()
            => Ok(await _tickerService.GetAllSymbolsAsync());

        [HttpGet("timeseries/{symbol}")]
        public async Task<IActionResult> GetTimeSeries([FromRoute] string symbol, [FromQuery] string interval = "1day", [FromQuery] int outputSize = 100)
        {
            var data = await _tickerService.GetTimeSeriesAsync(symbol, interval, outputSize);
            return Ok(data);
        }
    }

}
