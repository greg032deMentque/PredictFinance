using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers
{
    [Authorize(Policy = "Bearer")]
    [Route("api/[controller]")]
    [ApiController]
    public class ClientFinanceController : ControllerBase
    {
        private readonly IClientFinanceService _clientFinanceService;

        public ClientFinanceController(IClientFinanceService clientFinanceService)
        {
            _clientFinanceService = clientFinanceService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(CancellationToken ct)
        {
            return Ok(await _clientFinanceService.GetDashboardAsync(ct));
        }

        [HttpGet("assets/search")]
        public async Task<IActionResult> SearchAssets([FromQuery] string query, CancellationToken ct)
        {
            return Ok(await _clientFinanceService.SearchAssetsAsync(query, ct));
        }

        [HttpGet("watchlist")]
        public async Task<IActionResult> GetWatchlist(CancellationToken ct)
        {
            return Ok(await _clientFinanceService.GetWatchlistAsync(ct));
        }

        [HttpPost("watchlist")]
        public async Task<IActionResult> AddToWatchlist([FromBody] WatchlistUpsertRequestViewModel model, CancellationToken ct)
        {
            return Ok(await _clientFinanceService.AddToWatchlistAsync(model, ct));
        }

        [HttpDelete("watchlist/{symbol}")]
        public async Task<IActionResult> RemoveFromWatchlist([FromRoute] string symbol, CancellationToken ct)
        {
            await _clientFinanceService.RemoveFromWatchlistAsync(symbol, ct);
            return NoContent();
        }

        [HttpGet("quote/{symbol}")]
        public async Task<IActionResult> GetLiveQuote([FromRoute] string symbol, CancellationToken ct)
        {
            return Ok(await _clientFinanceService.GetLiveQuoteAsync(symbol, ct));
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] int take = 100, CancellationToken ct = default)
        {
            return Ok(await _clientFinanceService.GetTransactionsAsync(take, ct));
        }

        [HttpPost("transactions")]
        public async Task<IActionResult> RegisterTransaction([FromBody] TransactionCreateRequestViewModel model, CancellationToken ct)
        {
            return Ok(await _clientFinanceService.RegisterTransactionAsync(model, ct));
        }

        [HttpDelete("transactions/{id}")]
        public async Task<IActionResult> DeleteTransaction([FromRoute] string id, CancellationToken ct)
        {
            await _clientFinanceService.DeleteTransactionAsync(id, ct);
            return NoContent();
        }

        [HttpPost("analysis/run")]
        public async Task<IActionResult> RunAnalysis([FromBody] AnalysisRunRequestViewModel model, CancellationToken ct)
        {
            return Ok(await _clientFinanceService.RunAnalysisAsync(model, ct));
        }

        [HttpGet("analysis/recent")]
        public async Task<IActionResult> GetRecentAnalyses([FromQuery] int take = 10, CancellationToken ct = default)
        {
            return Ok(await _clientFinanceService.GetRecentAnalysesAsync(take, ct));
        }

        [HttpPost("simulation/run")]
        public async Task<IActionResult> RunSimulation([FromBody] SimulationRequestViewModel model, CancellationToken ct)
        {
            return Ok(await _clientFinanceService.RunSimulationAsync(model, ct));
        }
    }
}
