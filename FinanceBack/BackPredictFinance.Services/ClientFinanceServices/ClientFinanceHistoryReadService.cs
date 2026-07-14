using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.History;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface IClientFinanceHistoryReadService
    {
        Task<PagedHistoryFeedViewModel> GetHistoryFeedAsync(HistoryQueryViewModel query, CancellationToken ct = default);
        Task<PagedInstrumentHistoryViewModel?> GetInstrumentHistoryAsync(string symbol, InstrumentHistoryQueryViewModel query, CancellationToken ct = default);
    }

    public sealed class ClientFinanceHistoryReadService : BaseService, IClientFinanceHistoryReadService
    {
        private readonly IClientFinanceAssetSupportService _assetSupportService;
        private readonly IClientFinanceProjectionService _projectionService;

        public ClientFinanceHistoryReadService(
            IServiceProvider serviceProvider,
            IClientFinanceAssetSupportService assetSupportService,
            IClientFinanceProjectionService projectionService)
            : base(serviceProvider)
        {
            _assetSupportService = assetSupportService;
            _projectionService = projectionService;
        }

        public async Task<PagedHistoryFeedViewModel> GetHistoryFeedAsync(HistoryQueryViewModel query, CancellationToken ct = default)
        {
            var userId = _assetSupportService.GetRequiredCurrentUserId();
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var symbolFilter = string.IsNullOrWhiteSpace(query.Symbol)
                ? null
                : query.Symbol.Trim().ToUpperInvariant();

            var dbQuery = _financeDbContext.AnalysisRuns
                .AsNoTracking()
                .Include(x => x.Asset)
                .Where(x => x.UserId == userId && x.Status == _projectionService.CompletedStatus);

            if (symbolFilter != null)
            {
                dbQuery = dbQuery.Where(x => x.Asset.Symbol.ToUpper() == symbolFilter);
            }

            var sortAscending = string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            dbQuery = sortAscending
                ? dbQuery.OrderBy(x => x.CompletedAtUtc ?? x.StartedAtUtc)
                : dbQuery.OrderByDescending(x => x.CompletedAtUtc ?? x.StartedAtUtc);

            var analysisRuns = await dbQuery.ToListAsync(ct);

            var peaStatusByAssetId = await LoadLatestPeaStatusesAsync(
                analysisRuns.Select(x => x.AssetId).Distinct().ToList(), ct);

            var allItems = new List<HistoryItemViewModel>(analysisRuns.Count);
            foreach (var analysisRun in analysisRuns)
            {
                var snapshot = _projectionService.TryReadSnapshot(analysisRun.RawPayload);
                if (snapshot == null)
                {
                    continue;
                }

                if (query.Recommendation.HasValue)
                {
                    var kind = snapshot.Recommendation?.RecommendationPayload.Kind;
                    if (kind != query.Recommendation.Value)
                    {
                        continue;
                    }
                }

                peaStatusByAssetId.TryGetValue(analysisRun.AssetId, out var peaStatus);
                allItems.Add(_projectionService.BuildHistoryItem(analysisRun.Asset, analysisRun.Id, snapshot, peaStatus));
            }

            var total = allItems.Count;
            var pagedItems = allItems
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedHistoryFeedViewModel
            {
                Items = pagedItems,
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedInstrumentHistoryViewModel?> GetInstrumentHistoryAsync(string symbol, InstrumentHistoryQueryViewModel query, CancellationToken ct = default)
        {
            var normalizedSymbol = _assetSupportService.NormalizeSymbol(symbol);
            if (string.IsNullOrWhiteSpace(normalizedSymbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(symbol));
            }

            var userId = _assetSupportService.GetRequiredCurrentUserId();
            var userAsset = await _financeDbContext.UserAssets
                .AsNoTracking()
                .Include(x => x.Asset)
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Asset.Symbol == normalizedSymbol, ct);

            if (userAsset == null)
            {
                return null;
            }

            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var sortAscending = string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            var dbQuery = _financeDbContext.AnalysisRuns
                .AsNoTracking()
                .Include(x => x.Asset)
                .Where(x => x.UserId == userId
                    && x.AssetId == userAsset.AssetId
                    && x.Status == _projectionService.CompletedStatus);

            dbQuery = sortAscending
                ? dbQuery.OrderBy(x => x.CompletedAtUtc ?? x.StartedAtUtc)
                : dbQuery.OrderByDescending(x => x.CompletedAtUtc ?? x.StartedAtUtc);

            var total = await dbQuery.CountAsync(ct);
            var analysisRuns = await dbQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var peaStatus = await _projectionService.GetLatestPeaEligibilityStatusAsync(userAsset.AssetId, ct);
            var items = new List<InstrumentHistoryItemViewModel>(analysisRuns.Count);
            foreach (var analysisRun in analysisRuns)
            {
                var snapshot = _projectionService.TryReadSnapshot(analysisRun.RawPayload);
                if (snapshot == null)
                {
                    continue;
                }

                var historyItem = _projectionService.BuildHistoryItem(analysisRun.Asset, analysisRun.Id, snapshot, peaStatus);
                items.Add(new InstrumentHistoryItemViewModel
                {
                    AnalysisId = historyItem.AnalysisId,
                    SnapshotId = historyItem.SnapshotId,
                    TimestampUtc = historyItem.TimestampUtc,
                    Outcome = historyItem.Outcome,
                    OutcomeDisplayLabel = historyItem.OutcomeDisplayLabel,
                    PrimaryPatternLabel = historyItem.PrimaryPatternLabel,
                    RecommendationSummary = historyItem.RecommendationSummary,
                    SupportAvailabilitySummary = historyItem.SupportAvailabilitySummary,
                    PeaEligibilityStatus = historyItem.PeaEligibilityStatus,
                    PeaSummary = historyItem.PeaSummary,
                    AnalysisEngineVersion = historyItem.AnalysisEngineVersion,
                    RecommendationPolicyVersion = historyItem.RecommendationPolicyVersion,
                    ExplanationPolicyVersion = historyItem.ExplanationPolicyVersion,
                    DetailUrl = historyItem.DetailUrl,
                    ComparisonUrl = historyItem.ComparisonUrl
                });
            }

            return new PagedInstrumentHistoryViewModel
            {
                Instrument = _projectionService.BuildInstrumentIdentity(userAsset.Asset),
                Symbol = normalizedSymbol,
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        private async Task<Dictionary<string, PeaEligibilityStatusEnum>> LoadLatestPeaStatusesAsync(List<string> assetIds, CancellationToken ct)
        {
            if (assetIds.Count == 0)
            {
                return new Dictionary<string, PeaEligibilityStatusEnum>(StringComparer.Ordinal);
            }

            return await _financeDbContext.AssetPeaEligibilities
                .AsNoTracking()
                .Where(x => assetIds.Contains(x.AssetId))
                .GroupBy(x => x.AssetId)
                .Select(x => x.OrderByDescending(y => y.CheckedUtc ?? y.CreatedAtUtc).First())
                .ToDictionaryAsync(x => x.AssetId, x => x.EligibilityStatus, ct);
        }
    }
}
