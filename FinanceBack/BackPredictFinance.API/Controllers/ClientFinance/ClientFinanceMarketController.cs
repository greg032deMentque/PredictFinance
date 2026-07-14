using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.History;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Watchlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.ClientFinance
{
    [Authorize(Policy = "Bearer")]
    [Route("api/ClientFinance")]
    [ApiController]
    public class ClientFinanceMarketController(
        IClientFinanceService clientFinanceService,
        IClientFinanceWatchlistPortfolioService watchlistPortfolioService,
        IClientFinanceInstrumentDetailService instrumentDetailService,
        IClientFinanceHistoryReadService historyReadService) : ControllerBase
    {
        [HttpGet("assets/search")]
        public async Task<IActionResult> SearchAssets([FromQuery] string? query, [FromQuery] bool peaEligibleOnly = false, CancellationToken ct = default)
        {
            return Ok(await clientFinanceService.SearchAssetsAsync(query ?? string.Empty, peaEligibleOnly, ct));
        }

        [HttpGet("watchlist")]
        public async Task<IActionResult> GetWatchlist(CancellationToken ct)
        {
            return Ok(await watchlistPortfolioService.GetWatchlistAsync(ct));
        }

        [HttpPost("watchlist")]
        public async Task<IActionResult> AddToWatchlist([FromBody] WatchlistUpsertRequestViewModel model, CancellationToken ct)
        {
            return Ok(await watchlistPortfolioService.AddToWatchlistAsync(model, ct));
        }

        [HttpDelete("watchlist/{symbol}")]
        public async Task<IActionResult> RemoveFromWatchlist([FromRoute] string symbol, CancellationToken ct)
        {
            await watchlistPortfolioService.RemoveFromWatchlistAsync(symbol, ct);
            return NoContent();
        }

        [HttpGet("quote/{symbol}")]
        public async Task<IActionResult> GetLiveQuote([FromRoute] string symbol, CancellationToken ct)
        {
            return Ok(await watchlistPortfolioService.GetLiveQuoteAsync(symbol, ct));
        }

        [HttpGet("instruments/{symbol}")]
        public async Task<IActionResult> GetInstrumentDetail([FromRoute] string symbol, CancellationToken ct)
        {
            var payload = await instrumentDetailService.GetInstrumentDetailAsync(symbol, ct);
            return payload is null ? NotFound() : Ok(payload);
        }

        [HttpGet("instruments/{symbol}/analysis-history")]
        public async Task<IActionResult> GetInstrumentHistory([FromRoute] string symbol, [FromQuery] InstrumentHistoryQueryViewModel query, CancellationToken ct = default)
        {
            var payload = await historyReadService.GetInstrumentHistoryAsync(symbol, query, ct);
            return payload is null ? NotFound() : Ok(payload);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] HistoryQueryViewModel query, CancellationToken ct = default)
        {
            return Ok(await historyReadService.GetHistoryFeedAsync(query, ct));
        }
    }
}
