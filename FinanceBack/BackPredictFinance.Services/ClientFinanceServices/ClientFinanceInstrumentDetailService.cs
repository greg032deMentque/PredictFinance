using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.ClientFinanceServices.PortfolioCostBasis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    /// <summary>
    /// Expose le détail instrument projeté pour le front client finance.
    /// </summary>
    public interface IClientFinanceInstrumentDetailService
    {
        /// <summary>
        /// Retourne le détail projeté d'un instrument pour l'utilisateur courant.
        /// </summary>
        Task<InstrumentDetailViewModel?> GetInstrumentDetailAsync(string symbol, CancellationToken ct = default);
    }

    /// <summary>
    /// Implémente la composition de la vue détail d'un instrument.
    /// </summary>
    public sealed class ClientFinanceInstrumentDetailService : BaseService, IClientFinanceInstrumentDetailService
    {
        private readonly IClientFinanceAssetSupportService _assetSupportService;
        private readonly IClientFinanceProjectionService _projectionService;

        public ClientFinanceInstrumentDetailService(
            IServiceProvider serviceProvider,
            IClientFinanceAssetSupportService assetSupportService,
            IClientFinanceProjectionService projectionService)
            : base(serviceProvider)
        {
            _assetSupportService = assetSupportService;
            _projectionService = projectionService;
        }

        public async Task<InstrumentDetailViewModel?> GetInstrumentDetailAsync(string symbol, CancellationToken ct = default)
        {
            var normalizedSymbol = _assetSupportService.NormalizeSymbol(symbol);
            if (string.IsNullOrWhiteSpace(normalizedSymbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(symbol));
            }

            var userAsset = await _financeDbContext.UserAssets
                .AsNoTracking()
                .Include(x => x.Asset)
                .FirstOrDefaultAsync(x => x.UserId == _assetSupportService.GetRequiredCurrentUserId() && x.Asset.Symbol == normalizedSymbol, ct);
            if (userAsset == null)
            {
                return null;
            }

            var latestRun = await _financeDbContext.AnalysisRuns
                .AsNoTracking()
                .Include(x => x.Asset)
                .Where(x => x.UserId == _assetSupportService.GetRequiredCurrentUserId()
                    && x.AssetId == userAsset.AssetId
                    && x.Status == _projectionService.CompletedStatus)
                .OrderByDescending(x => x.CompletedAtUtc ?? x.StartedAtUtc)
                .FirstOrDefaultAsync(ct);

            var snapshot = latestRun == null ? null : _projectionService.TryReadSnapshot(latestRun.RawPayload);
            var holdsInstrument = userAsset.Quantity > 0m;
            var marketReadingSummary = snapshot == null
                ? _projectionService.BuildEmptyMarketReading()
                : _projectionService.BuildMarketReadingSummary(snapshot, snapshot.PrimaryPattern);
            var recommendation = BuildRecommendation(snapshot, holdsInstrument, marketReadingSummary.RecommendationStrength);
            var latestPeaEligibility = await _projectionService.GetLatestPeaEligibilityAsync(userAsset.AssetId, ct);
            var freshness = _projectionService.BuildFreshness(snapshot?.CompletedAtUtc ?? userAsset.Asset.LastProfileSyncUtc);
            var costBasis = await ComputeAverageUnitCostAsync(userAsset.Id, snapshot, ct);
            int? openLineCount = snapshot?.PortfolioContextSnapshot.OpenLineCount > 0
                ? snapshot.PortfolioContextSnapshot.OpenLineCount
                : null;
            var currencyCode = snapshot?.PortfolioContextSnapshot.CurrencyCode;
            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                currencyCode = userAsset.Asset.Currency;
            }

            return new InstrumentDetailViewModel
            {
                Symbol = normalizedSymbol,
                InstrumentSummary = _projectionService.BuildInstrumentSummary(
                    userAsset.Asset,
                    latestPeaEligibility?.EligibilityStatus ?? PeaEligibilityStatusEnum.Unknown,
                    freshness,
                    snapshot != null,
                    latestRun?.Id,
                    snapshot?.SnapshotId),
                MarketReading = _projectionService.BuildDetailedMarketReading(snapshot, marketReadingSummary),
                SupportReading = _projectionService.BuildDetailedSupportReading(latestPeaEligibility),
                PersonalSituation = _projectionService.BuildPersonalSituation(
                    holdsInstrument,
                    userAsset.Quantity,
                    costBasis.AverageUnitCost,
                    openLineCount,
                    currencyCode ?? "EUR",
                    costBasis.HasDataIntegrityWarning,
                    recommendation),
                NavigationLinks = _projectionService.BuildInstrumentNavigationLinks(normalizedSymbol),
                LatestAnalysisId = latestRun?.Id,
                LatestSnapshotId = snapshot?.SnapshotId
            };
        }

        private RecommendationSummaryViewModel BuildRecommendation(
            PersistedAnalysisSnapshotPayloadReadModel? snapshot,
            bool holdsInstrument,
            RecommendationStrengthEnum? recommendationStrength)
        {
            if (snapshot == null)
            {
                return _projectionService.BuildDefaultRecommendation(holdsInstrument);
            }

            return _projectionService.BuildRecommendationSummary(
                snapshot.Recommendation?.RecommendationPayload,
                holdsInstrument,
                recommendationStrength);
        }

        private async Task<(decimal? AverageUnitCost, bool HasDataIntegrityWarning)> ComputeAverageUnitCostAsync(
            string userAssetId,
            PersistedAnalysisSnapshotPayloadReadModel? snapshot,
            CancellationToken ct)
        {
            if (snapshot?.PortfolioContextSnapshot.HoldsInstrument == true
                && snapshot.PortfolioContextSnapshot.AverageUnitCost.HasValue
                && snapshot.PortfolioContextSnapshot.AverageUnitCost.Value > 0m)
            {
                return (
                    decimal.Round(snapshot.PortfolioContextSnapshot.AverageUnitCost.Value, 4),
                    snapshot.PortfolioContextSnapshot.HasDataIntegrityWarning);
            }

            var transactions = await _financeDbContext.AssetTransactions
                .AsNoTracking()
                .Where(x => x.UserAssetId == userAssetId && !x.IsDeleted)
                .ExcludeArchivedPortfolios()
                .ToListAsync(ct);

            var result = PortfolioCostBasisCalculator.Compute(transactions);
            return (result.AverageUnitCost, !result.IsHistoryConsistent);
        }
    }
}
