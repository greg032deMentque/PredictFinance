using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.Services.TwelveDataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BackPredictFinance.Services.BackgroundJobs
{
    public sealed class MarketDataRefreshJob : BackgroundService
    {
        // Exécution quotidienne à 19h00 UTC — les marchés européens ferment vers 17h30 UTC,
        // le délai de 90 min laisse le temps aux données journalières d'être publiées par Yahoo Finance.
        private const int ScheduledHourUtc = 19;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MarketDataRefreshJob> _logger;

        public MarketDataRefreshJob(IServiceScopeFactory scopeFactory, ILogger<MarketDataRefreshJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(ComputeDelayUntilNextRun(), stoppingToken);

                try
                {
                    await RefreshTrackedAssetCandlesAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "MarketDataRefreshJob: erreur lors du cycle de rafraîchissement");
                }
            }
        }

        private static TimeSpan ComputeDelayUntilNextRun()
        {
            var now = DateTime.UtcNow;
            var nextRunUtc = now.Date.AddHours(ScheduledHourUtc);
            if (nextRunUtc <= now)
            {
                nextRunUtc = nextRunUtc.AddDays(1);
            }

            return nextRunUtc - now;
        }

        private async Task RefreshTrackedAssetCandlesAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
            var priceProvider = scope.ServiceProvider.GetRequiredService<IMarketPriceProvider>();
            var persistenceService = scope.ServiceProvider.GetRequiredService<IAnalysisSnapshotPersistenceService>();

            var assetIds = await ResolveTrackedAssetIdsAsync(db, ct);

            _logger.LogInformation("MarketDataRefreshJob: {Count} actif(s) à rafraîchir", assetIds.Count);

            foreach (var assetId in assetIds)
            {
                await RefreshSingleAssetAsync(db, priceProvider, persistenceService, assetId, ct);
            }
        }

        private static async Task<IReadOnlyList<string>> ResolveTrackedAssetIdsAsync(
            FinanceDbContext db,
            CancellationToken ct)
        {
            var watchlistAssetIds = await db.UserAssets
                .AsNoTracking()
                .Select(ua => ua.AssetId)
                .ToListAsync(ct);

            var openSignalAssetIds = await db.SignalOutcomes
                .AsNoTracking()
                .Where(so => so.Outcome == SignalOutcomeEnum.StillOpen)
                .Select(so => so.AnalysisRun.AssetId)
                .ToListAsync(ct);

            return watchlistAssetIds
                .Union(openSignalAssetIds, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task RefreshSingleAssetAsync(
            FinanceDbContext db,
            IMarketPriceProvider priceProvider,
            IAnalysisSnapshotPersistenceService persistenceService,
            string assetId,
            CancellationToken ct)
        {
            var asset = await db.Assets
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == assetId, ct);

            if (asset is null)
            {
                _logger.LogWarning("MarketDataRefreshJob: actif {AssetId} introuvable, ignoré", assetId);
                return;
            }

            var symbol = string.IsNullOrWhiteSpace(asset.ProviderSymbol) ? asset.Symbol : asset.ProviderSymbol;

            try
            {
                var candles = await priceProvider.GetChartAsync(symbol, "1d", "5d", ct);
                if (candles.Count == 0)
                {
                    _logger.LogInformation("MarketDataRefreshJob: aucune bougie retournée pour {Symbol}, ignoré", symbol);
                    return;
                }

                await persistenceService.UpsertCandlesForRefreshAsync(assetId, candles, ct);
                _logger.LogInformation("MarketDataRefreshJob: {Count} bougie(s) rafraîchie(s) pour {Symbol}", candles.Count, symbol);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException)
            {
                _logger.LogWarning(ex, "MarketDataRefreshJob: rafraîchissement échoué pour {Symbol} ({ExceptionType}), actif ignoré", symbol, ex.GetType().Name);
            }
        }
    }
}
