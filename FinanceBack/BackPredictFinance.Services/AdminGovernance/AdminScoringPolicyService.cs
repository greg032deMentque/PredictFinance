using BackPredictFinance.Common.Fundamentals;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.Fundamentals;
using BackPredictFinance.ViewModels.AdminViewModels.ScoringPolicy;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.AdminGovernance
{
    public interface IAdminScoringPolicyService
    {
        Task<AdminScoringPolicyViewModel> GetAsync(CancellationToken ct = default);
        Task<List<AdminScoringPolicyVersionViewModel>> GetVersionsAsync(CancellationToken ct = default);
        Task<AdminScoringPolicyVersionViewModel> GetVersionByIdAsync(string id, CancellationToken ct = default);
        Task<AdminScoringPolicyVersionViewModel> CreateVersionAsync(AdminScoringPolicyVersionCreateRequestViewModel request, CancellationToken ct = default);
        Task<AdminScoringPolicyVersionViewModel> ActivateVersionAsync(string id, CancellationToken ct = default);
    }

    public sealed class AdminScoringPolicyService : BaseService, IAdminScoringPolicyService
    {
        private readonly IFundamentalScoringPolicyService _fundamentalScoringPolicyService;

        public AdminScoringPolicyService(IServiceProvider serviceProvider, IFundamentalScoringPolicyService fundamentalScoringPolicyService)
            : base(serviceProvider)
        {
            _fundamentalScoringPolicyService = fundamentalScoringPolicyService;
        }

        public async Task<AdminScoringPolicyViewModel> GetAsync(CancellationToken ct = default)
        {
            var activePolicy = await _fundamentalScoringPolicyService.ResolveActiveAsync(ct);

            return new AdminScoringPolicyViewModel
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
                MinimumCategoriesRequiredFloor = activePolicy.MinimumCategoriesRequiredFloor,
                MinimumCategoriesRequiredCeiling = activePolicy.MinimumCategoriesRequiredCeiling,
                MinimumCategoriesRequiredDefault = activePolicy.MinimumCategoriesRequiredDefault,
                MinimumSectorSampleSize = activePolicy.MinimumSectorSampleSize,
                CoveragePenaltySupported = activePolicy.CoveragePenaltySupported,
                ActivePolicyVersionId = activePolicy.PolicyVersionId
            };
        }

        public async Task<List<AdminScoringPolicyVersionViewModel>> GetVersionsAsync(CancellationToken ct = default)
        {
            var versions = await _financeDbContext.FundamentalScoringPolicyVersions
                .AsNoTracking()
                .OrderByDescending(x => x.IsActive)
                .ThenByDescending(x => x.CreatedAtUtc)
                .ToListAsync(ct);

            return [.. versions.Select(MapToViewModel)];
        }

        public async Task<AdminScoringPolicyVersionViewModel> GetVersionByIdAsync(string id, CancellationToken ct = default)
        {
            var normalizedId = (id ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedId))
            {
                throw new ArgumentException("Policy version id is required.", nameof(id));
            }

            var version = await _financeDbContext.FundamentalScoringPolicyVersions
                .AsNoTracking()
                .Where(x => x.Id == normalizedId)
                .FirstOrDefaultAsync(ct);

            return version is null
                ? throw new KeyNotFoundException($"Fundamental scoring policy version '{normalizedId}' was not found.")
                : MapToViewModel(version);
        }

        public async Task<AdminScoringPolicyVersionViewModel> CreateVersionAsync(AdminScoringPolicyVersionCreateRequestViewModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var version = new FundamentalScoringPolicyVersion
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = request.DisplayName,
                IsActive = false,
                ActivatedAtUtc = null,
                MinimumCategoriesRequiredFloor = request.MinimumCategoriesRequiredFloor,
                MinimumCategoriesRequiredCeiling = request.MinimumCategoriesRequiredCeiling,
                MinimumCategoriesRequiredDefault = request.MinimumCategoriesRequiredDefault,
                MinimumSectorSampleSize = request.MinimumSectorSampleSize,
                CoveragePenaltySupported = request.CoveragePenaltySupported
            };

            _financeDbContext.FundamentalScoringPolicyVersions.Add(version);
            await _financeDbContext.SaveChangesAsync(ct);

            return MapToViewModel(version);
        }

        public async Task<AdminScoringPolicyVersionViewModel> ActivateVersionAsync(string id, CancellationToken ct = default)
        {
            var normalizedId = (id ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedId))
            {
                throw new ArgumentException("Policy version id is required.", nameof(id));
            }

            var targetVersion = await _financeDbContext.FundamentalScoringPolicyVersions
                .Where(x => x.Id == normalizedId)
                .FirstOrDefaultAsync(ct)
                ?? throw new KeyNotFoundException($"Fundamental scoring policy version '{normalizedId}' was not found.");

            var currentlyActiveVersions = await _financeDbContext.FundamentalScoringPolicyVersions
                .Where(x => x.IsActive && x.Id != normalizedId)
                .ToListAsync(ct);

            foreach (var currentlyActiveVersion in currentlyActiveVersions)
            {
                currentlyActiveVersion.IsActive = false;
            }

            targetVersion.IsActive = true;
            targetVersion.ActivatedAtUtc = DateTime.UtcNow;

            await _financeDbContext.SaveChangesAsync(ct);

            return MapToViewModel(targetVersion);
        }

        private static AdminScoringPolicyVersionViewModel MapToViewModel(FundamentalScoringPolicyVersion version)
        {
            return new AdminScoringPolicyVersionViewModel
            {
                Id = version.Id,
                DisplayName = version.DisplayName,
                IsActive = version.IsActive,
                ActivatedAtUtc = version.ActivatedAtUtc,
                MinimumCategoriesRequiredFloor = version.MinimumCategoriesRequiredFloor,
                MinimumCategoriesRequiredCeiling = version.MinimumCategoriesRequiredCeiling,
                MinimumCategoriesRequiredDefault = version.MinimumCategoriesRequiredDefault,
                MinimumSectorSampleSize = version.MinimumSectorSampleSize,
                CoveragePenaltySupported = version.CoveragePenaltySupported,
                CreatedAtUtc = version.CreatedAtUtc
            };
        }
    }
}
