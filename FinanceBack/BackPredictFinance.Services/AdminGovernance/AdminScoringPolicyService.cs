using BackPredictFinance.Common.Fundamentals;
using BackPredictFinance.Services.Fundamentals;
using BackPredictFinance.ViewModels.AdminViewModels.ScoringPolicy;

namespace BackPredictFinance.Services.AdminGovernance
{
    public interface IAdminScoringPolicyService
    {
        Task<AdminScoringPolicyViewModel> GetAsync(CancellationToken ct = default);
    }

    public sealed class AdminScoringPolicyService : BaseService, IAdminScoringPolicyService
    {

        public AdminScoringPolicyService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public Task<AdminScoringPolicyViewModel> GetAsync(CancellationToken ct = default)
        {
            return Task.FromResult(new AdminScoringPolicyViewModel
            {
                SupportedUniverseId = FundamentalScoringPolicyDefaults.SupportedUniverseId,
                ScoringVersion = FundamentalScoringPolicyDefaults.ScoringVersion,
                EligibilityPolicyVersion = FundamentalScoringPolicyDefaults.EligibilityPolicyVersion,
                ProviderId = FundamentalScoringPolicyDefaults.ProviderId,
                AsOfUtcSemantics = FundamentalScoringPolicyDefaults.AsOfUtcSemantics,
                CategoryCodes = [.. FundamentalScoringPolicyDefaults.CategoryCodes],
                MetricCodes = [.. FundamentalScoringPolicyDefaults.HigherIsBetterMetricCodes, .. FundamentalScoringPolicyDefaults.LowerIsBetterMetricCodes],
                HigherIsBetterMetricCodes = [.. FundamentalScoringPolicyDefaults.HigherIsBetterMetricCodes],
                LowerIsBetterMetricCodes = [.. FundamentalScoringPolicyDefaults.LowerIsBetterMetricCodes],
                MinimumCategoriesRequiredFloor = FundamentalScoringPolicyDefaults.MinimumCategoriesRequiredFloor,
                MinimumCategoriesRequiredCeiling = FundamentalScoringPolicyDefaults.MinimumCategoriesRequiredCeiling,
                CoveragePenaltySupported = true
            });
        }
    }
}
