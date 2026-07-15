using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Portfolios;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.ClientFinance
{
    [Authorize(Policy = "Bearer")]
    [Route("api/ClientFinance")]
    [ApiController]
    public class ClientFinancePortfolioController(
        IClientFinanceWatchlistPortfolioService watchlistPortfolioService,
        IPortfolioService portfolioService,
        IClientFinanceTransactionService transactionService) : ControllerBase
    {
        [HttpGet("portfolio")]
        public async Task<IActionResult> GetPortfolio([FromQuery] string? portfolioId, CancellationToken ct)
        {
            return Ok(await watchlistPortfolioService.GetPortfolioAsync(portfolioId, ct));
        }

        [HttpGet("portfolios")]
        public async Task<IActionResult> GetPortfolios(CancellationToken ct)
        {
            return Ok(await portfolioService.GetPortfoliosAsync(ct));
        }

        [HttpPost("portfolios")]
        public async Task<IActionResult> CreatePortfolio([FromBody] PortfolioCreateRequestViewModel model, CancellationToken ct)
        {
            var created = await portfolioService.CreatePortfolioAsync(model, ct);
            return CreatedAtAction(nameof(GetPortfolios), created);
        }

        [HttpPut("portfolios/{id}")]
        public async Task<IActionResult> RenamePortfolio([FromRoute] string id, [FromBody] PortfolioRenameRequestViewModel model, CancellationToken ct)
        {
            return Ok(await portfolioService.RenamePortfolioAsync(id, model, ct));
        }

        [HttpPut("portfolios/{id}/archive")]
        public async Task<IActionResult> ArchivePortfolio([FromRoute] string id, CancellationToken ct)
        {
            await portfolioService.ArchivePortfolioAsync(id, ct);
            return NoContent();
        }

        [HttpPut("portfolios/{id}/restore")]
        public async Task<IActionResult> RestorePortfolio([FromRoute] string id, CancellationToken ct)
        {
            await portfolioService.RestorePortfolioAsync(id, ct);
            return NoContent();
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] int take = 100, [FromQuery] string? portfolioId = null, CancellationToken ct = default)
        {
            return Ok(await transactionService.GetTransactionsAsync(take, portfolioId, ct));
        }

        [HttpPost("transactions")]
        public async Task<IActionResult> RegisterTransaction([FromBody] TransactionCreateRequestViewModel model, CancellationToken ct)
        {
            return Ok(await transactionService.RegisterTransactionAsync(model, ct));
        }

        [HttpDelete("transactions/{id}")]
        public async Task<IActionResult> DeleteTransaction([FromRoute] string id, CancellationToken ct)
        {
            await transactionService.DeleteTransactionAsync(id, ct);
            return NoContent();
        }
    }
}
