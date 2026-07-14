using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Contact;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Education;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.History;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Simulation;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Snapshots;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Transactions;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Watchlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers
{
    [Authorize(Policy = "Bearer")]
    [Route("api/[controller]")]
    [ApiController]
    public class ClientFinanceController(
        IClientFinanceService clientFinanceService,
        IClientFinanceDashboardHistoryService dashboardHistoryService,
        IClientFinanceWatchlistPortfolioService watchlistPortfolioService,
        IClientFinanceTransactionService transactionService,
        IClientFinanceHistoryReadService historyReadService,
        IClientFinanceInstrumentDetailService instrumentDetailService,
        IClientFinanceContactService contactService,
        IClientFinanceSnapshotComparisonService snapshotComparisonService,
        IClientFinanceLearningService learningService,
        IClientFinanceParameterDetailService parameterDetailService,
        IClientGlossaryService clientGlossaryService,
        IEducationContentService educationContentService,
        IGlossaryTermService glossaryTermService) : ControllerBase
    {
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(CancellationToken ct)
        {
            return Ok(await dashboardHistoryService.GetDashboardAsync(ct));
        }

        [HttpGet("assets/search")]
        public async Task<IActionResult> SearchAssets([FromQuery] string query, CancellationToken ct)
        {
            return Ok(await clientFinanceService.SearchAssetsAsync(query, ct: ct));
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

        [HttpGet("portfolio")]
        public async Task<IActionResult> GetPortfolio([FromQuery] string? portfolioId, CancellationToken ct)
        {
            return Ok(await watchlistPortfolioService.GetPortfolioAsync(portfolioId, ct));
        }

        [HttpGet("analysis/{analysisId}")]
        public async Task<IActionResult> GetAnalysisDetail([FromRoute] string analysisId, CancellationToken ct)
        {
            var payload = await dashboardHistoryService.GetAnalysisDetailAsync(analysisId, ct);
            return payload is null ? NotFound() : Ok(payload);
        }

        [HttpPost("contact")]
        public async Task<IActionResult> Contact([FromBody] ContactSupportRequestViewModel model, CancellationToken ct)
        {
            await contactService.SendSupportMessageAsync(model, ct);
            return NoContent();
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] HistoryQueryViewModel query, CancellationToken ct)
        {
            return Ok(await historyReadService.GetHistoryFeedAsync(query, ct));
        }

        [HttpGet("instruments/{symbol}")]
        public async Task<IActionResult> GetInstrumentDetail([FromRoute] string symbol, CancellationToken ct)
        {
            var payload = await instrumentDetailService.GetInstrumentDetailAsync(symbol, ct);
            return payload is null ? NotFound() : Ok(payload);
        }

        [HttpGet("instruments/{symbol}/analysis-history")]
        public async Task<IActionResult> GetInstrumentHistory([FromRoute] string symbol, [FromQuery] InstrumentHistoryQueryViewModel query, CancellationToken ct)
        {
            var payload = await historyReadService.GetInstrumentHistoryAsync(symbol, query, ct);
            return payload is null ? NotFound() : Ok(payload);
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] int take = 100, CancellationToken ct = default)
        {
            return Ok(await transactionService.GetTransactionsAsync(take, ct: ct));
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

        [HttpPost("analysis/run")]
        public async Task<IActionResult> RunAnalysis([FromBody] AnalysisRunRequestViewModel model, CancellationToken ct)
        {
            return Ok(await clientFinanceService.RunAnalysisAsync(model, ct));
        }

        [HttpGet("analysis/recent")]
        public async Task<IActionResult> GetRecentAnalyses([FromQuery] int take = 10, CancellationToken ct = default)
        {
            return Ok(await dashboardHistoryService.GetRecentAnalysesAsync(take, ct));
        }

        [HttpPost("snapshots/compare")]
        public async Task<IActionResult> CompareSnapshots([FromBody] SnapshotComparisonRequestViewModel model, CancellationToken ct)
        {
            var payload = await snapshotComparisonService.CompareAsync(model, ct);
            return payload is null ? NotFound() : Ok(payload);
        }

        [HttpPost("simulation/run")]
        public async Task<IActionResult> RunSimulation([FromBody] SimulationRequestViewModel model, CancellationToken ct)
        {
            return Ok(await clientFinanceService.RunSimulationAsync(model, ct));
        }

        [HttpGet("learn")]
        public async Task<IActionResult> GetLearnOverview(CancellationToken ct)
        {
            return Ok(await learningService.GetLearnOverviewAsync(ct));
        }

        [HttpGet("onboarding")]
        public async Task<IActionResult> GetOnboarding(CancellationToken ct)
        {
            return Ok(await learningService.GetOnboardingGuidanceAsync(ct));
        }

        [HttpGet("parameters/{analysisId}/{parameterId}")]
        public async Task<IActionResult> GetParameterDetail(
            [FromRoute] string analysisId,
            [FromRoute] string parameterId,
            CancellationToken ct)
        {
            var payload = await parameterDetailService.GetParameterDetailAsync(analysisId, parameterId, ct);
            return payload is null ? NotFound() : Ok(payload);
        }

        [HttpGet("glossary")]
        public async Task<IActionResult> GetGlossary(CancellationToken ct)
        {
            return Ok(await clientGlossaryService.GetGlossaryAsync(ct));
        }

        [HttpGet("education")]
        public async Task<IActionResult> GetEducationArticles(CancellationToken ct)
        {
            return Ok(await educationContentService.GetPublishedAsync(ct));
        }

        [HttpGet("education/{slug}")]
        public async Task<IActionResult> GetEducationArticle([FromRoute] string slug, CancellationToken ct)
        {
            var article = await educationContentService.GetBySlugAsync(slug, ct);
            return article is null ? NotFound() : Ok(article);
        }

        [HttpGet("glossary-terms")]
        public async Task<IActionResult> GetGlossaryTerms([FromQuery] string? search, CancellationToken ct)
        {
            return Ok(await glossaryTermService.SearchAsync(search, ct));
        }
    }
}
