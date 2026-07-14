using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.ViewModels.AdminViewModels.DataQuality;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.AdminGovernance
{
    public interface IAdminDataQualityService
    {
        Task<AdminDataQualityViewModel> GetAsync(CancellationToken ct = default);
    }

    public sealed class AdminDataQualityService : BaseService, IAdminDataQualityService
    {
        public AdminDataQualityService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<AdminDataQualityViewModel> GetAsync(CancellationToken ct = default)
        {
            var assetsMissingProfileSyncCount = await _financeDbContext.Assets.CountAsync(x => !x.LastProfileSyncUtc.HasValue, ct);
            var assetsWithoutPeaRegistryCount = await _financeDbContext.Assets.CountAsync(x => !x.PeaEligibilities.Any(), ct);
            var peaRegistryUnknownStatusCount = await _financeDbContext.AssetPeaEligibilities.CountAsync(x => x.EligibilityStatus == PeaEligibilityStatusEnum.Unknown, ct);
            var completedAnalysisRunsWithoutModelSnapshotCount = await _financeDbContext.AnalysisRuns.CountAsync(x => x.Status == AnalysisRunStatusEnum.Completed && x.ModelSnapshot == null, ct);
            var completedAnalysisRunsWithoutDecisionSignalCount = await _financeDbContext.AnalysisRuns.CountAsync(x => x.Status == AnalysisRunStatusEnum.Completed && x.DecisionSignal == null, ct);

            var issueSummaries = new List<string>();
            if (assetsMissingProfileSyncCount > 0)
            {
                issueSummaries.Add($"{assetsMissingProfileSyncCount} asset(s) have no profile synchronization timestamp.");
            }

            if (assetsWithoutPeaRegistryCount > 0)
            {
                issueSummaries.Add($"{assetsWithoutPeaRegistryCount} asset(s) have no PEA registry entry.");
            }

            if (peaRegistryUnknownStatusCount > 0)
            {
                issueSummaries.Add($"{peaRegistryUnknownStatusCount} PEA registry entrie(s) remain in UNKNOWN status.");
            }

            if (completedAnalysisRunsWithoutModelSnapshotCount > 0)
            {
                issueSummaries.Add($"{completedAnalysisRunsWithoutModelSnapshotCount} completed analysis run(s) have no model snapshot.");
            }

            if (completedAnalysisRunsWithoutDecisionSignalCount > 0)
            {
                issueSummaries.Add($"{completedAnalysisRunsWithoutDecisionSignalCount} completed analysis run(s) have no decision signal.");
            }

            return new AdminDataQualityViewModel
            {
                AssetsMissingProfileSyncCount = assetsMissingProfileSyncCount,
                AssetsWithoutPeaRegistryCount = assetsWithoutPeaRegistryCount,
                PeaRegistryUnknownStatusCount = peaRegistryUnknownStatusCount,
                CompletedAnalysisRunsWithoutModelSnapshotCount = completedAnalysisRunsWithoutModelSnapshotCount,
                CompletedAnalysisRunsWithoutDecisionSignalCount = completedAnalysisRunsWithoutDecisionSignalCount,
                IssueSummaries = issueSummaries
            };
        }
    }
}
