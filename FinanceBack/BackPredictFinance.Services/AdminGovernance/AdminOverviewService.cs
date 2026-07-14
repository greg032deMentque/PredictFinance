using BackPredictFinance.Common.enums;
using Microsoft.Extensions.DependencyInjection;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.ViewModels.AdminViewModels.Overview;
using BackPredictFinance.ViewModels.AdminViewModels.Kpi;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.AdminGovernance
{
    public interface IAdminOverviewService
    {
        Task<AdminOverviewViewModel> GetAsync(CancellationToken ct = default);
    }

    public sealed class AdminOverviewService : BaseService, IAdminOverviewService
    {
        public AdminOverviewService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<AdminOverviewViewModel> GetAsync(CancellationToken ct = default)
        {
            var kpiService = _serviceProvider.GetRequiredService<IAdminKpiService>();
            var kpiCards = await kpiService.GetKpiCardsAsync(ct);

            return new AdminOverviewViewModel
            {
                KpiCards = kpiCards,
                TotalUsers = await _financeDbContext.Users.CountAsync(ct),
                ActiveUsers = await _financeDbContext.Users.CountAsync(x => x.IsActive, ct),
                TotalAssets = await _financeDbContext.Assets.CountAsync(ct),
                TotalAnalysisRuns = await _financeDbContext.AnalysisRuns.CountAsync(ct),
                CompletedAnalysisRuns = await _financeDbContext.AnalysisRuns.CountAsync(x => x.Status == AnalysisRunStatusEnum.Completed, ct),
                FailedAnalysisRuns = await _financeDbContext.AnalysisRuns.CountAsync(x => x.Status == AnalysisRunStatusEnum.Failed, ct),
                ConfirmedEligiblePeaEntries = await _financeDbContext.AssetPeaEligibilities.CountAsync(x => x.EligibilityStatus == PeaEligibilityStatusEnum.ConfirmedEligible, ct),
                UnknownPeaEntries = await _financeDbContext.AssetPeaEligibilities.CountAsync(x => x.EligibilityStatus == PeaEligibilityStatusEnum.Unknown, ct),
                PublishedParameterEntries = await _financeDbContext.ParameterDictionaryEntries.CountAsync(x => x.IsPublished, ct),
                LatestCompletedAnalysisUtc = await _financeDbContext.AnalysisRuns
                    .Where(x => x.CompletedAtUtc.HasValue)
                    .OrderByDescending(x => x.CompletedAtUtc)
                    .Select(x => x.CompletedAtUtc)
                    .FirstOrDefaultAsync(ct)
            };
        }
    }
}
