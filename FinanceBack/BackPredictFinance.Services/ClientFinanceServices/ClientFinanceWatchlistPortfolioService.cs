using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Assets;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Portfolio;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Watchlist;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    /// <summary>
    /// Expose les lectures watchlist et portefeuille de l'utilisateur courant.
    /// </summary>
    public interface IClientFinanceWatchlistPortfolioService
    {
        Task<List<WatchlistItemViewModel>> GetWatchlistAsync(CancellationToken ct = default);
        Task<PortfolioViewModel> GetPortfolioAsync(string? portfolioId, CancellationToken ct = default);
        /// <summary>
        /// Ajoute un instrument à la watchlist.
        /// </summary>
        Task<WatchlistItemViewModel> AddToWatchlistAsync(WatchlistUpsertRequestViewModel request, CancellationToken ct = default);
        /// <summary>
        /// Retire un instrument de la watchlist.
        /// </summary>
        Task RemoveFromWatchlistAsync(string symbol, CancellationToken ct = default);
        /// <summary>
        /// Retourne la cotation live projetée d'un instrument.
        /// </summary>
        Task<LiveQuoteViewModel> GetLiveQuoteAsync(string symbol, CancellationToken ct = default);
    }

    /// <summary>
    /// Implémente la composition de la watchlist et du portefeuille utilisateur.
    /// </summary>
    public sealed class ClientFinanceWatchlistPortfolioService : BaseService, IClientFinanceWatchlistPortfolioService
    {
        private readonly IClientFinanceAssetSupportService _assetSupportService;
        private readonly IClientFinanceProjectionService _projectionService;
        private readonly IPortfolioService _portfolioService;
        private readonly IPortfolioAllocationService _allocationService;
        private readonly ILogger<ClientFinanceWatchlistPortfolioService> _holdingLogger;

        public ClientFinanceWatchlistPortfolioService(
            IServiceProvider serviceProvider,
            IClientFinanceAssetSupportService assetSupportService,
            IClientFinanceProjectionService projectionService,
            IPortfolioService portfolioService,
            IPortfolioAllocationService allocationService,
            ILogger<ClientFinanceWatchlistPortfolioService> holdingLogger)
            : base(serviceProvider)
        {
            _assetSupportService = assetSupportService;
            _projectionService = projectionService;
            _portfolioService = portfolioService;
            _allocationService = allocationService;
            _holdingLogger = holdingLogger;
        }

        public Task<List<WatchlistItemViewModel>> GetWatchlistAsync(CancellationToken ct = default)
            => BuildWatchlistAsync(includeOnlyHeld: false, excludeHeld: true, portfolioId: null, ct);

        public async Task<PortfolioViewModel> GetPortfolioAsync(string? portfolioId, CancellationToken ct = default)
        {
            var userId = _assetSupportService.GetRequiredCurrentUserId();

            if (!string.IsNullOrWhiteSpace(portfolioId))
            {
                await _portfolioService.GetRequiredPortfolioForUserAsync(portfolioId, userId, ct);
            }

            var watchlist = await BuildWatchlistAsync(includeOnlyHeld: true, excludeHeld: false, portfolioId, ct);
            var allocation = await _allocationService.ComputeAllocationAsync(userId, portfolioId, ct);

            return new PortfolioViewModel
            {
                Positions = watchlist
                    .Where(x => x.HeldQuantity > 0m)
                    .Select(x => new PortfolioPositionViewModel
                    {
                        UserAssetId = x.UserAssetId,
                        Instrument = x.Instrument,
                        QuantityHeld = x.HeldQuantity,
                        AverageCost = x.AverageBuyPrice,
                        Fees = decimal.Round(Math.Max(x.InvestedAmount - (x.AverageBuyPrice * x.HeldQuantity), 0m), 2),
                        OutstandingAmount = x.OutstandingAmount,
                        CurrentPriceNative = x.LastPrice,
                        Currency = x.Currency,
                        ForexRateUsed = x.ForexRateUsed,
                        MarketReading = x.MarketReading,
                        SupportReading = x.SupportReading,
                        Recommendation = x.Recommendation,
                        RiskHint = x.MarketReading.RiskHint,
                        HistoryEntryUrl = $"/api/ClientFinance/instruments/{x.Instrument.Symbol}/analysis-history",
                        SimulationUrl = $"/api/ClientFinance/simulation/run"
                    })
                    .ToList(),
                TotalInvestedAmount = decimal.Round(watchlist.Sum(x => x.InvestedAmount), 2),
                TotalOutstandingAmount = decimal.Round(watchlist.Sum(x => x.OutstandingAmount), 2),
                OpenPositionCount = watchlist.Count,
                Allocation = allocation
            };
        }

        public async Task<WatchlistItemViewModel> AddToWatchlistAsync(WatchlistUpsertRequestViewModel request, CancellationToken ct = default)
        {
            var symbol = _assetSupportService.NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request));
            }

            var asset = await _assetSupportService.EnsureAssetAsync(symbol, request.CompanyName, ct);
            var userAsset = await _financeDbContext.UserAssets
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.UserId == _currentUserId && x.AssetId == asset.Id, ct);

            if (userAsset == null)
            {
                userAsset = new UserAsset
                {
                    UserId = _assetSupportService.GetRequiredCurrentUserId(),
                    AssetId = asset.Id,
                    Quantity = 0m
                };

                await _financeDbContext.UserAssets.AddAsync(userAsset, ct);
                await _financeDbContext.SaveChangesAsync(ct);
            }
            else if (userAsset.IsDeleted)
            {
                userAsset.IsDeleted = false;
                await _financeDbContext.SaveChangesAsync(ct);
            }

            var quote = await _assetSupportService.BuildQuoteAsync(symbol, ct);
            var forexRate = await _assetSupportService.GetForexRateToEurAsync(asset.Currency, ct);

            return new WatchlistItemViewModel
            {
                UserAssetId = userAsset.Id,
                Instrument = _projectionService.BuildInstrumentIdentity(asset),
                Symbol = asset.Symbol,
                AssetType = _assetSupportService.MapAssetType(asset.AssetType),
                CompanyName = asset.Name ?? asset.Symbol,
                Market = asset.Exchange,
                Currency = asset.Currency,
                LastPrice = quote.LastPrice,
                LastPriceEur = decimal.Round(quote.LastPrice * forexRate, 4),
                ForexRateUsed = decimal.Round(forexRate, 6),
                DayVariationPct = quote.DayVariationPct,
                HeldQuantity = userAsset.Quantity,
                AverageBuyPrice = 0m,
                InvestedAmount = 0m,
                OutstandingAmount = decimal.Round(userAsset.Quantity * quote.LastPrice * forexRate, 2),
                HoldingStatus = userAsset.Quantity > 0m ? HoldingStatusEnum.Held : HoldingStatusEnum.NotHeld,
                MarketReading = _projectionService.BuildEmptyMarketReading(),
                SupportReading = _projectionService.BuildSupportReadingSummary(PeaEligibilityStatusEnum.Unknown),
                Recommendation = _projectionService.BuildDefaultRecommendation(userAsset.Quantity > 0m),
                Freshness = _projectionService.BuildFreshness(asset.LastProfileSyncUtc)
            };
        }

        public async Task RemoveFromWatchlistAsync(string symbol, CancellationToken ct = default)
        {
            var normalizedSymbol = _assetSupportService.NormalizeSymbol(symbol);
            if (string.IsNullOrWhiteSpace(normalizedSymbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(symbol));
            }

            var userAsset = await _financeDbContext.UserAssets
                .Include(x => x.Asset)
                .FirstOrDefaultAsync(x => x.UserId == _currentUserId && x.Asset.Symbol == normalizedSymbol, ct);

            if (userAsset == null)
            {
                return;
            }

            if (userAsset.Quantity > 0m)
            {
                throw new InvalidOperationException("Impossible de supprimer une valeur avec une position ouverte.");
            }

            userAsset.IsDeleted = true;
            await _financeDbContext.SaveChangesAsync(ct);
        }

        public async Task<LiveQuoteViewModel> GetLiveQuoteAsync(string symbol, CancellationToken ct = default)
        {
            var normalizedSymbol = _assetSupportService.NormalizeSymbol(symbol);
            if (string.IsNullOrWhiteSpace(normalizedSymbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(symbol));
            }

            var asset = await _assetSupportService.EnsureAssetAsync(normalizedSymbol, null, ct);
            var quote = await _assetSupportService.BuildQuoteAsync(normalizedSymbol, ct);
            await _assetSupportService.SavePriceHistoryAsync(asset.Id, quote.LastPrice, ct);

            return new LiveQuoteViewModel
            {
                Symbol = normalizedSymbol,
                AssetType = _assetSupportService.MapAssetType(asset.AssetType),
                LastPrice = quote.LastPrice,
                DayVariationPct = quote.DayVariationPct,
                AsOfUtc = DateTime.UtcNow
            };
        }

        private async Task<List<WatchlistItemViewModel>> BuildWatchlistAsync(bool includeOnlyHeld, bool excludeHeld, string? portfolioId, CancellationToken ct)
        {
            var userAssets = await _financeDbContext.UserAssets
                .AsNoTracking()
                .Include(x => x.Asset)
                .Where(x => x.UserId == _currentUserId)
                .OrderBy(x => x.Asset.Symbol)
                .ToListAsync(ct);

            if (includeOnlyHeld)
            {
                userAssets = userAssets.Where(x => x.Quantity > 0m).ToList();
            }
            else if (excludeHeld)
            {
                userAssets = userAssets.Where(x => x.Quantity == 0m).ToList();
            }

            var assetIds = userAssets.Select(x => x.AssetId).Distinct().ToList();
            var userAssetIds = userAssets.Select(x => x.Id).ToList();

            var transactionQuery = _financeDbContext.AssetTransactions
                .AsNoTracking()
                .Where(x => userAssetIds.Contains(x.UserAssetId));

            if (!string.IsNullOrWhiteSpace(portfolioId))
            {
                // Accès ciblé : un portefeuille archivé reste consultable directement.
                transactionQuery = transactionQuery.Where(x => x.PortfolioId == portfolioId);
            }

            // Agrégat global (portfolioId vide) : les portefeuilles archivés restent inclus pour rester
            // cohérent avec UserAsset.Quantity, qui n'a jamais été décrémentée par l'archivage.
            var transactions = await transactionQuery.ToListAsync(ct);

            var latestPeaStatusByAssetId = await _financeDbContext.AssetPeaEligibilities
                .AsNoTracking()
                .Where(x => assetIds.Contains(x.AssetId))
                .GroupBy(x => x.AssetId)
                .Select(x => x.OrderByDescending(y => y.CheckedUtc ?? y.CreatedAtUtc).First())
                .ToDictionaryAsync(x => x.AssetId, x => x.EligibilityStatus, ct);

            var latestAnalysisByAssetId = await _projectionService.LoadLatestAnalysisByAssetIdAsync(assetIds, ct);
            var groupedTransactions = transactions
                .GroupBy(x => x.UserAssetId)
                .ToDictionary(x => x.Key, x => x.ToList());

            var symbols = userAssets.Select(x => x.Asset.Symbol).Distinct().ToList();
            var quotesBySymbol = await _assetSupportService.BuildQuotesAsync(symbols, ct);

            var distinctCurrencies = userAssets
                .Select(x => x.Asset.Currency)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var forexRateByCurrency = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var currency in distinctCurrencies)
            {
                forexRateByCurrency[currency] = await _assetSupportService.GetForexRateToEurAsync(currency, ct);
            }

            var response = new List<WatchlistItemViewModel>(userAssets.Count);

            foreach (var userAsset in userAssets)
            {
                if (!quotesBySymbol.TryGetValue(userAsset.Asset.Symbol, out var quote))
                {
                    continue;
                }

                var forexRate = forexRateByCurrency.GetValueOrDefault(userAsset.Asset.Currency, 1m);
                groupedTransactions.TryGetValue(userAsset.Id, out var history);
                history ??= [];

                var holding = PortfolioHoldingCalculator.Compute(history, _holdingLogger);

                // Quantité segmentée par portefeuille quand un portfolioId est fourni ; sinon la
                // détention reste la vérité globale portée par UserAsset (cf. AssetTransaction).
                var heldQuantity = string.IsNullOrWhiteSpace(portfolioId) ? userAsset.Quantity : holding.Quantity;
                var averageBuyPrice = holding.AverageBuyPrice;
                var investedAmount = holding.InvestedAmount;
                var outstandingAmount = heldQuantity * quote.LastPrice * forexRate;

                latestAnalysisByAssetId.TryGetValue(userAsset.AssetId, out var latestAnalysis);
                latestPeaStatusByAssetId.TryGetValue(userAsset.AssetId, out var peaStatus);

                var marketReading = latestAnalysis == null
                    ? _projectionService.BuildEmptyMarketReading()
                    : _projectionService.BuildMarketReadingSummary(latestAnalysis, latestAnalysis.PrimaryPattern);
                var recommendation = latestAnalysis == null
                    ? _projectionService.BuildDefaultRecommendation(heldQuantity > 0m)
                    : _projectionService.BuildRecommendationSummary(
                        latestAnalysis.Recommendation?.RecommendationPayload,
                        heldQuantity > 0m,
                        marketReading.RecommendationStrength);
                var checkedAtUtc = latestAnalysis?.CompletedAtUtc ?? userAsset.Asset.LastProfileSyncUtc;
                var earningsHorizonDays = latestAnalysis?.Recommendation?.RecommendationPayload?.ReviewHorizonDays ?? 0;
                var earningsWithinHorizonWarning = latestAnalysis != null
                    && EarningsHorizonEvaluator.IsWithinHorizon(latestAnalysis.EarningsDateUtc, earningsHorizonDays, latestAnalysis.CompletedAtUtc);

                response.Add(new WatchlistItemViewModel
                {
                    UserAssetId = userAsset.Id,
                    Instrument = _projectionService.BuildInstrumentIdentity(userAsset.Asset),
                    Symbol = userAsset.Asset.Symbol,
                    AssetType = _assetSupportService.MapAssetType(userAsset.Asset.AssetType),
                    CompanyName = userAsset.Asset.Name ?? userAsset.Asset.Symbol,
                    Market = userAsset.Asset.Exchange,
                    Currency = userAsset.Asset.Currency,
                    LastPrice = quote.LastPrice,
                    LastPriceEur = decimal.Round(quote.LastPrice * forexRate, 4),
                    ForexRateUsed = decimal.Round(forexRate, 6),
                    DayVariationPct = quote.DayVariationPct,
                    HeldQuantity = heldQuantity,
                    AverageBuyPrice = averageBuyPrice,
                    InvestedAmount = investedAmount,
                    OutstandingAmount = decimal.Round(outstandingAmount, 2),
                    HoldingStatus = heldQuantity > 0m ? HoldingStatusEnum.Held : HoldingStatusEnum.NotHeld,
                    HasPersistedAnalysis = latestAnalysis != null,
                    MarketReading = marketReading,
                    SupportReading = _projectionService.BuildSupportReadingSummary(peaStatus),
                    Recommendation = recommendation,
                    LastAnalysisAtUtc = latestAnalysis?.CompletedAtUtc,
                    Freshness = _projectionService.BuildFreshness(checkedAtUtc),
                    NextEarningsDateUtc = latestAnalysis?.EarningsDateUtc,
                    EarningsWithinHorizonWarning = earningsWithinHorizonWarning
                });
            }

            return response;
        }
    }
}
