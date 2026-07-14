using BackPredictFinance.ViewModels.AdminViewModels.Pea;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.AdminGovernance
{
    public interface IAdminPeaRegistryService
    {
        Task<List<AdminPeaRegistryItemViewModel>> GetListAsync(CancellationToken ct = default);
    }

    public sealed class AdminPeaRegistryService : BaseService, IAdminPeaRegistryService
    {
        public AdminPeaRegistryService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<List<AdminPeaRegistryItemViewModel>> GetListAsync(CancellationToken ct = default)
        {
            return await _financeDbContext.AssetPeaEligibilities
                .AsNoTracking()
                .Include(x => x.Asset)
                .OrderBy(x => x.Asset.Symbol)
                .ThenBy(x => x.UniverseId)
                .Select(x => new AdminPeaRegistryItemViewModel
                {
                    EntryId = x.Id,
                    AssetId = x.AssetId,
                    Symbol = x.Asset.Symbol,
                    DisplayName = x.Asset.Name ?? x.Asset.Symbol,
                    UniverseId = x.UniverseId,
                    EligibilityStatus = x.EligibilityStatus,
                    SourceType = x.SourceType,
                    SourceReference = x.SourceReference,
                    CheckedUtc = x.CheckedUtc,
                    PolicyVersion = x.PolicyVersion,
                    ReviewerNote = x.ReviewerNote
                })
                .ToListAsync(ct);
        }
    }
}
