using BackPredictFinance.Common.Fundamentals;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.Fundamentals
{
    public interface IFundamentalScoringPolicyService
    {
        Task<FundamentalScoringPolicySnapshot> ResolveActiveAsync(CancellationToken ct = default);
    }

    public sealed class FundamentalScoringPolicyService : BaseService, IFundamentalScoringPolicyService
    {
        public FundamentalScoringPolicyService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<FundamentalScoringPolicySnapshot> ResolveActiveAsync(CancellationToken ct = default)
        {
            var activeVersion = await _financeDbContext.FundamentalScoringPolicyVersions
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.ActivatedAtUtc)
                .ThenBy(x => x.Id)
                .FirstOrDefaultAsync(ct);

            if (activeVersion is null)
            {
                return FundamentalScoringPolicySnapshot.Defaults();
            }

            return new FundamentalScoringPolicySnapshot
            {
                PolicyVersionId = activeVersion.Id,
                IsActivePolicyPresent = true,
                MinimumCategoriesRequiredFloor = activeVersion.MinimumCategoriesRequiredFloor,
                MinimumCategoriesRequiredCeiling = activeVersion.MinimumCategoriesRequiredCeiling,
                MinimumCategoriesRequiredDefault = activeVersion.MinimumCategoriesRequiredDefault,
                MinimumSectorSampleSize = activeVersion.MinimumSectorSampleSize,
                CoveragePenaltySupported = activeVersion.CoveragePenaltySupported
            };
        }
    }
}
