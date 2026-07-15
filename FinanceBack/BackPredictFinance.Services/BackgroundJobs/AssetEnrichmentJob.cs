using BackPredictFinance.Common;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.TwelveDataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BackPredictFinance.Services.BackgroundJobs
{
    public sealed class AssetEnrichmentJob : BackgroundService
    {
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AssetEnrichmentJob> _logger;
        private readonly IOptions<MarketDataOptions> _options;

        public AssetEnrichmentJob(IServiceScopeFactory scopeFactory, ILogger<AssetEnrichmentJob> logger, IOptions<MarketDataOptions> options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(StartupDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await EnrichUnsectoredAssetsAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "AssetEnrichmentJob: erreur lors du cycle d'enrichissement");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task EnrichUnsectoredAssetsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
            var catalogProvider = scope.ServiceProvider.GetRequiredService<IMarketCatalogProvider>();
            var fundamentalsProvider = scope.ServiceProvider.GetRequiredService<IFundamentalsProvider>();

            var assetIds = await ResolveUnenrichedAssetIdsAsync(db, ct);

            _logger.LogInformation("AssetEnrichmentJob: {Count} actif(s) à enrichir", assetIds.Count);

            foreach (var assetId in assetIds)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                await EnrichSingleAssetAsync(db, catalogProvider, assetId, ct);
                await PersistFundamentalsSnapshotAsync(db, fundamentalsProvider, assetId, ct);
                await Task.Delay(TimeSpan.FromMilliseconds(_options.Value.RefreshThrottleMilliseconds), ct);
            }
        }

        internal async Task PersistFundamentalsSnapshotAsync(
            FinanceDbContext db,
            IFundamentalsProvider fundamentalsProvider,
            (string Id, string Symbol) assetRef,
            CancellationToken ct)
        {
            try
            {
                var fundamentals = await fundamentalsProvider.GetFundamentalsAsync(assetRef.Symbol, ct);

                if (!fundamentals.MarketCap.HasValue || fundamentals.MarketCap.Value <= 0m)
                {
                    _logger.LogWarning(
                        "AssetEnrichmentJob: fondamentaux rejetés pour {Symbol} (MarketCap invalide)",
                        assetRef.Symbol);
                    return;
                }

                var snapshot = new AssetFundamentalsSnapshot
                {
                    AssetId = assetRef.Id,
                    AsOfUtc = DateTime.UtcNow,
                    MarketCap = fundamentals.MarketCap,
                    TrailingPE = fundamentals.TrailingPe is > 0m ? fundamentals.TrailingPe : null,
                    DividendYield = fundamentals.DividendYield,
                    Source = fundamentals.ProviderId
                };

                db.AssetFundamentalsSnapshots.Add(snapshot);
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "AssetEnrichmentJob: fondamentaux indisponibles pour {Symbol} ({ExceptionType}), snapshot ignoré",
                    assetRef.Symbol, ex.GetType().Name);
            }
        }

        private static async Task<IReadOnlyList<(string Id, string Symbol)>> ResolveUnenrichedAssetIdsAsync(
            FinanceDbContext db,
            CancellationToken ct)
        {
            var rows = await db.Assets
                .AsNoTracking()
                .Where(a => string.IsNullOrEmpty(a.Sector) || string.IsNullOrEmpty(a.Country))
                .Select(a => new { a.Id, a.Symbol })
                .ToListAsync(ct);

            return rows.Select(x => (x.Id, x.Symbol)).ToList();
        }

        private async Task EnrichSingleAssetAsync(
            FinanceDbContext db,
            IMarketCatalogProvider catalogProvider,
            (string Id, string Symbol) assetRef,
            CancellationToken ct)
        {
            try
            {
                var profile = await catalogProvider.GetAssetProfileAsync(assetRef.Symbol, ct);

                var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetRef.Id, ct);
                if (asset is null)
                {
                    return;
                }

                var updated = false;

                if (!string.IsNullOrWhiteSpace(profile.Sector) && string.IsNullOrEmpty(asset.Sector))
                {
                    asset.Sector = profile.Sector;
                    updated = true;
                }

                if (!string.IsNullOrWhiteSpace(profile.Country) && string.IsNullOrEmpty(asset.Country))
                {
                    asset.Country = profile.Country;
                    updated = true;
                }

                if (!string.IsNullOrWhiteSpace(profile.Summary) && string.IsNullOrEmpty(asset.Summary))
                {
                    asset.Summary = profile.Summary;
                    updated = true;
                }

                if (!string.IsNullOrWhiteSpace(profile.Category) && string.IsNullOrEmpty(asset.Category))
                {
                    asset.Category = profile.Category;
                    updated = true;
                }

                if (updated)
                {
                    asset.LastProfileSyncUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                    _logger.LogInformation("AssetEnrichmentJob: profil enrichi pour {Symbol}", assetRef.Symbol);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "AssetEnrichmentJob: enrichissement échoué pour {Symbol} ({ExceptionType}), actif ignoré",
                    assetRef.Symbol, ex.GetType().Name);
            }
        }
    }
}
