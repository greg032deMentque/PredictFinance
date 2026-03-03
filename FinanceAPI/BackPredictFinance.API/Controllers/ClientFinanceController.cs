using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackPredictFinance.API.Controllers
{
    [Authorize(Policy = "Bearer")]
    [Route("[controller]")]
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
            var userId = GetCurrentUserId();
            return Ok(await _clientFinanceService.GetDashboardAsync(userId, ct));
        }

        [HttpGet("assets/search")]
        public async Task<IActionResult> SearchAssets([FromQuery] string query, CancellationToken ct)
        {
            return Ok(await _clientFinanceService.SearchAssetsAsync(query, ct));
        }

        [HttpGet("watchlist")]
        public async Task<IActionResult> GetWatchlist(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            return Ok(await _clientFinanceService.GetWatchlistAsync(userId, ct));
        }

        [HttpPost("watchlist")]
        public async Task<IActionResult> AddToWatchlist([FromBody] WatchlistUpsertRequestViewModel model, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            return Ok(await _clientFinanceService.AddToWatchlistAsync(userId, model, ct));
        }

        [HttpDelete("watchlist/{symbol}")]
        public async Task<IActionResult> RemoveFromWatchlist([FromRoute] string symbol, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            await _clientFinanceService.RemoveFromWatchlistAsync(userId, symbol, ct);
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
            var userId = GetCurrentUserId();
            return Ok(await _clientFinanceService.GetTransactionsAsync(userId, take, ct));
        }

        [HttpPost("transactions")]
        public async Task<IActionResult> RegisterTransaction([FromBody] TransactionCreateRequestViewModel model, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            return Ok(await _clientFinanceService.RegisterTransactionAsync(userId, model, ct));
        }

        [HttpDelete("transactions/{id}")]
        public async Task<IActionResult> DeleteTransaction([FromRoute] string id, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            await _clientFinanceService.DeleteTransactionAsync(userId, id, ct);
            return NoContent();
        }

        [HttpPost("analysis/run")]
        public async Task<IActionResult> RunAnalysis([FromBody] AnalysisRunRequestViewModel model, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            return Ok(await _clientFinanceService.RunAnalysisAsync(userId, model, ct));
        }

        [HttpGet("analysis/recent")]
        public async Task<IActionResult> GetRecentAnalyses([FromQuery] int take = 10, CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            return Ok(await _clientFinanceService.GetRecentAnalysesAsync(userId, take, ct));
        }

        [HttpPost("simulation/run")]
        public async Task<IActionResult> RunSimulation([FromBody] SimulationRequestViewModel model, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            return Ok(await _clientFinanceService.RunSimulationAsync(userId, model, ct));
        }

        private string GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Sid);
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Utilisateur non authentifie.");
            }

            return userId;
        }
    }
}
