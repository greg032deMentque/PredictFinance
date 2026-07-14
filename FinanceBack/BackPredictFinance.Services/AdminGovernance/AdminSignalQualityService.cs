using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.AdminViewModels.SignalQuality;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.AdminGovernance
{
    public interface IAdminSignalQualityService
    {
        Task<AdminSignalQualityKpiViewModel> GetSignalQualityKpiAsync(int windowDays, string? policyVersion = null, CancellationToken ct = default);
    }

    public sealed class AdminSignalQualityService : BaseService, IAdminSignalQualityService
    {
        public AdminSignalQualityService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public async Task<AdminSignalQualityKpiViewModel> GetSignalQualityKpiAsync(
            int windowDays,
            string? policyVersion = null,
            CancellationToken ct = default)
        {
            var since = DateTime.UtcNow.AddDays(-windowDays);

            var query = _financeDbContext.SignalOutcomes
                .AsNoTracking()
                .Where(signalOutcome => signalOutcome.EvaluatedAtUtc >= since);

            if (!string.IsNullOrWhiteSpace(policyVersion))
            {
                query = query.Where(signalOutcome => signalOutcome.PolicyVersion == policyVersion);
            }

            var outcomes = await query
                .Include(signalOutcome => signalOutcome.PatternAssessment)
                .Include(signalOutcome => signalOutcome.AnalysisRun)
                    .ThenInclude(analysisRun => analysisRun.ModelSnapshot)
                .ToListAsync(ct);

            var resolvedOutcomes = outcomes
                .Where(signalOutcome =>
                    signalOutcome.Outcome == SignalOutcomeEnum.TargetHit
                    || signalOutcome.Outcome == SignalOutcomeEnum.InvalidationHit
                    || signalOutcome.Outcome == SignalOutcomeEnum.TargetMiss)
                .ToList();

            var totalEvaluated = resolvedOutcomes.Count;
            var totalTargetHits = resolvedOutcomes.Count(signalOutcome => signalOutcome.Outcome == SignalOutcomeEnum.TargetHit);
            var overallTargetHitRate = totalEvaluated > 0
                ? Math.Round((decimal)totalTargetHits / totalEvaluated, 4)
                : 0m;

            var confidenceCalibration = new[]
                {
                    ConfidenceLabelEnum.Low,
                    ConfidenceLabelEnum.Medium,
                    ConfidenceLabelEnum.High
                }
                .Select(label =>
                {
                    var rows = resolvedOutcomes.Where(signalOutcome => signalOutcome.ConfidenceLabel == label).ToList();
                    var targetHits = rows.Count(signalOutcome => signalOutcome.Outcome == SignalOutcomeEnum.TargetHit);
                    var totalSignals = rows.Count;

                    return new ConfidenceCalibrationRowViewModel
                    {
                        Label = label.ToString(),
                        TotalSignals = totalSignals,
                        TargetHits = targetHits,
                        HitRate = totalSignals > 0 ? Math.Round((decimal)targetHits / totalSignals, 4) : 0m
                    };
                })
                .ToList();

            var patternPerformance = resolvedOutcomes
                .GroupBy(signalOutcome => signalOutcome.PatternAssessment.PatternId)
                .Select(group =>
                {
                    var targetHits = group.Count(signalOutcome => signalOutcome.Outcome == SignalOutcomeEnum.TargetHit);
                    var evaluatedCount = group.Count();

                    return new PatternPerformanceRowViewModel
                    {
                        PatternId = group.Key,
                        TotalEvaluated = evaluatedCount,
                        TargetHitRate = evaluatedCount > 0 ? Math.Round((decimal)targetHits / evaluatedCount, 4) : 0m,
                        AvgConfidence = evaluatedCount > 0 ? Math.Round(group.Average(signalOutcome => signalOutcome.PatternAssessment.Confidence), 4) : 0m
                    };
                })
                .OrderByDescending(row => row.TotalEvaluated)
                .ThenBy(row => row.PatternId)
                .ToList();

            var modelPerformance = resolvedOutcomes
                .GroupBy(signalOutcome => signalOutcome.AnalysisRun.ModelSnapshot?.ModelVersion ?? "unknown")
                .Select(group =>
                {
                    var targetHits = group.Count(signalOutcome => signalOutcome.Outcome == SignalOutcomeEnum.TargetHit);
                    var evaluatedCount = group.Count();

                    return new ModelPerformanceRowViewModel
                    {
                        ModelVersion = group.Key,
                        TotalEvaluated = evaluatedCount,
                        TargetHitRate = evaluatedCount > 0 ? Math.Round((decimal)targetHits / evaluatedCount, 4) : 0m
                    };
                })
                .OrderByDescending(row => row.TotalEvaluated)
                .ThenBy(row => row.ModelVersion)
                .ToList();

            return new AdminSignalQualityKpiViewModel
            {
                Window = $"D{windowDays}",
                PolicyVersionFilter = policyVersion,
                OverallTargetHitRate = overallTargetHitRate,
                TotalEvaluated = totalEvaluated,
                OpenSignals = outcomes.Count(signalOutcome => signalOutcome.Outcome == SignalOutcomeEnum.StillOpen),
                NotEvaluable = outcomes.Count(signalOutcome => signalOutcome.Outcome == SignalOutcomeEnum.NotEvaluable),
                ConfidenceCalibration = confidenceCalibration,
                PatternPerformance = patternPerformance,
                ModelPerformance = modelPerformance
            };
        }
    }
}
