using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.AdminViewModels.Instruments;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.AdminGovernance
{
    public interface IAdminInstrumentRegistryService
    {
        Task<List<AdminInstrumentRegistryItemViewModel>> GetListAsync(CancellationToken ct = default);
        Task<AdminInstrumentDetailViewModel> GetDetailAsync(string assetId, CancellationToken ct = default);
    }

    public sealed class AdminInstrumentRegistryService : BaseService, IAdminInstrumentRegistryService
    {
        public AdminInstrumentRegistryService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<List<AdminInstrumentRegistryItemViewModel>> GetListAsync(CancellationToken ct = default)
        {
            return await _financeDbContext.Assets
                .AsNoTracking()
                .Select(asset => new AdminInstrumentRegistryItemViewModel
                {
                    AssetId = asset.Id,
                    Symbol = asset.Symbol,
                    ProviderSymbol = asset.ProviderSymbol,
                    DisplayName = asset.Name ?? asset.Symbol,
                    Exchange = asset.Exchange,
                    Currency = asset.Currency,
                    AssetType = asset.AssetType.ToString(),
                    Country = asset.Country,
                    LastProfileSyncUtc = asset.LastProfileSyncUtc,
                    ActiveUniverseIds = asset.PeaEligibilities
                        .Where(x => x.EligibilityStatus == PeaEligibilityStatusEnum.ConfirmedEligible)
                        .Select(x => x.UniverseId)
                        .Distinct()
                        .OrderBy(x => x)
                        .ToList(),
                    HasConfirmedPeaEligibility = asset.PeaEligibilities.Any(x => x.EligibilityStatus == PeaEligibilityStatusEnum.ConfirmedEligible),
                    HasAnalysisHistory = asset.AnalysisRuns.Any()
                })
                .OrderBy(x => x.Symbol)
                .ToListAsync(ct);
        }

        public async Task<AdminInstrumentDetailViewModel> GetDetailAsync(string assetId, CancellationToken ct = default)
        {
            var normalizedAssetId = (assetId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedAssetId))
            {
                throw new ArgumentException("Asset id is required.", nameof(assetId));
            }

            var detail = await _financeDbContext.Assets
                .AsNoTracking()
                .Where(asset => asset.Id == normalizedAssetId)
                .Select(asset => new AdminInstrumentDetailViewModel
                {
                    AssetId = asset.Id,
                    Symbol = asset.Symbol,
                    ProviderSymbol = asset.ProviderSymbol,
                    DisplayName = asset.Name ?? asset.Symbol,
                    Exchange = asset.Exchange,
                    Currency = asset.Currency,
                    AssetType = asset.AssetType.ToString(),
                    Country = asset.Country,
                    Sector = asset.Sector,
                    Category = asset.Category,
                    Summary = asset.Summary,
                    LastProfileSyncUtc = asset.LastProfileSyncUtc,
                    ActiveUniverseIds = asset.PeaEligibilities
                        .Where(x => x.EligibilityStatus == PeaEligibilityStatusEnum.ConfirmedEligible)
                        .Select(x => x.UniverseId)
                        .Distinct()
                        .OrderBy(x => x)
                        .ToList(),
                    PeaRegistryEntriesCount = asset.PeaEligibilities.Count,
                    QuoteSnapshotsCount = asset.QuoteSnapshots.Count,
                    CandleSnapshotsCount = asset.CandleSnapshots.Count,
                    AnalysisRunsCount = asset.AnalysisRuns.Count
                })
                .FirstOrDefaultAsync(ct);

            return detail ?? throw new KeyNotFoundException($"Asset '{normalizedAssetId}' was not found.");
        }
    }
}
