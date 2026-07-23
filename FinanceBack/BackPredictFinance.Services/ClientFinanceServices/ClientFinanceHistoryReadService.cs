using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Common;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.History;
using BackPredictFinance.ViewModels.WebViewModels.PaginateViewModels;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface IClientFinanceHistoryReadService
    {
        Task<PagedResultViewModel<HistoryItemViewModel>> GetHistoryFeedAsync(HistoryQueryViewModel query, CancellationToken ct = default);
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

        public async Task<PagedResultViewModel<HistoryItemViewModel>> GetHistoryFeedAsync(HistoryQueryViewModel query, CancellationToken ct = default)
        {
            var userId = _assetSupportService.GetRequiredCurrentUserId();
            var (page, pageSize) = PaginationExtensions.NormalizePagination(query.Page, query.PageSize);
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

            // Pourquoi pas de Skip/Take SQL ici (contrairement à GetInstrumentHistoryAsync) :
            // le filtre Recommendation et l'exclusion des payloads illisibles ne s'appliquent
            // qu'après désérialisation de RawPayload, qui n'est pas requêtable en SQL. Paginer
            // avant ce filtre fausserait Total et produirait des pages incomplètes. Corrigible
            // uniquement en exposant Recommendation comme colonne dédiée (migration EF).
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

            return new PagedResultViewModel<HistoryItemViewModel>
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

            var (page, pageSize) = PaginationExtensions.NormalizePagination(query.Page, query.PageSize);
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
            var items = new List<HistoryItemViewModel>(analysisRuns.Count);
            foreach (var analysisRun in analysisRuns)
            {
                var snapshot = _projectionService.TryReadSnapshot(analysisRun.RawPayload);
                if (snapshot == null)
                {
                    continue;
                }

                var historyItem = _projectionService.BuildHistoryItem(analysisRun.Asset, analysisRun.Id, snapshot, peaStatus);
                historyItem.Instrument = null;
                historyItem.HistoryUrl = null;
                items.Add(historyItem);
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
