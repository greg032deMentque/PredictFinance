using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{

public interface IAnalysisLegacyCompatibilityService
{
    AnalysisResultViewModel MapRunResult(AnalysisResponseViewModel response);
    Task<List<AnalysisResultViewModel>> GetRecentAnalysesAsync(string userId, int take, CancellationToken ct = default);
}


    public sealed class AnalysisLegacyCompatibilityService : BaseService, IAnalysisLegacyCompatibilityService
    {
        public AnalysisLegacyCompatibilityService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public AnalysisResultViewModel MapRunResult(AnalysisResponseViewModel response)
        {
            ArgumentNullException.ThrowIfNull(response);

            var primaryPattern = response.MainPattern;
            var recommendation = response.Recommendation;
            var confidence = primaryPattern?.Scoring.ConfidenceScore ?? 0m;
            var recommendationAction = MapRecommendationKind(recommendation?.Kind);
            var isActionable = recommendationAction is RecommendationActionEnum.Buy or RecommendationActionEnum.Sell;

            return new AnalysisResultViewModel
            {
                Id = response.AnalysisId,
                Symbol = response.Instrument.Symbol,
                CompanyName = response.Instrument.DisplayName,
                Pattern = MapPatternId(primaryPattern?.PatternId ?? response.ExecutedPatternIds.FirstOrDefault()),
                Phase = primaryPattern?.Detection.CurrentPhaseCode ?? string.Empty,
                Probability = confidence,
                RecommendationAction = recommendationAction,
                RecommendationReason = recommendation?.Rationale ?? response.PedagogicalSummary,
                RiskLevel = InferRiskLevel(confidence, isActionable),
                RecommendationHorizonDays = recommendation?.ReviewHorizonDays ?? 0,
                PredictedAt = response.GeneratedAtUtc,
                IsActionable = isActionable,
                ModelStatus = response.ModelStatus,
                ModelMessage = string.IsNullOrWhiteSpace(response.ModelMessage) ? string.Join(" ", response.Warnings) : response.ModelMessage,
                CurrentPrice = primaryPattern?.Detection.CurrentPrice ?? 0m,
                NecklinePrice = null,
                TargetPrice = primaryPattern?.RiskHints.SuggestedTakeProfit,
                InvalidationPrice = primaryPattern?.Invalidation.InvalidationLevel
            };
        }

        public async Task<List<AnalysisResultViewModel>> GetRecentAnalysesAsync(string userId, int take, CancellationToken ct = default)
        {
            var size = Math.Clamp(take, 1, 100);

            var analysisRuns = await _financeDbContext.AnalysisRuns
                .AsNoTracking()
                .Include(x => x.Asset)
                .Include(x => x.PatternAssessments)
                .Include(x => x.DecisionSignal)
                .Include(x => x.ModelSnapshot)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CompletedAtUtc ?? x.StartedAtUtc)
                .Take(size)
                .ToListAsync(ct);

            if (analysisRuns.Count > 0)
            {
                return analysisRuns
                    .Select(MapAnalysisRunResult)
                    .ToList();
            }

            var recommendations = await _financeDbContext.Set<BackPredictFinance.Datas.Entities.Recommendation>()
                .AsNoTracking()
                .Include(x => x.UserAsset)
                .ThenInclude(x => x.Asset)
                .Where(x => x.UserAsset.UserId == userId)
                .OrderByDescending(x => x.RecommendedAtUtc)
                .Take(size)
                .ToListAsync(ct);

            return recommendations.Select(recommendation => new AnalysisResultViewModel
            {
                Id = recommendation.Id,
                Symbol = recommendation.UserAsset.Asset.Symbol,
                CompanyName = recommendation.UserAsset.Asset.Name ?? recommendation.UserAsset.Asset.Symbol,
                Pattern = TradingPatternEnum.DoubleTop,
                Probability = recommendation.Confidence,
                RecommendationAction = recommendation.Action,
                RecommendationReason = recommendation.Reason ?? "Aucune justification",
                RiskLevel = InferRiskLevel(recommendation.Confidence, recommendation.Action is RecommendationActionEnum.Buy or RecommendationActionEnum.Sell),
                PredictedAt = recommendation.RecommendedAtUtc,
                IsActionable = recommendation.Action is RecommendationActionEnum.Buy or RecommendationActionEnum.Sell,
                ModelStatus = ModelStatusEnum.NoGo
            }).ToList();
        }


        private static AnalysisResultViewModel MapAnalysisRunResult(BackPredictFinance.Datas.Entities.AnalysisRun analysisRun)
        {
            var primaryPattern = analysisRun.PatternAssessments
                .OrderByDescending(pattern => pattern.IsPrimary)
                .ThenByDescending(pattern => pattern.Confidence)
                .ThenByDescending(pattern => pattern.Probability)
                .FirstOrDefault();

            var recommendationAction = analysisRun.DecisionSignal?.Action ?? RecommendationActionEnum.Hold;
            var isActionable = analysisRun.DecisionSignal?.IsActionable ?? false;
            var modelStatus = analysisRun.ModelSnapshot?.ModelStatus ?? ModelStatusEnum.NoGo;
            var modelMessage = analysisRun.ModelSnapshot?.ModelMessage ?? string.Empty;

            return new AnalysisResultViewModel
            {
                Id = analysisRun.Id,
                Symbol = analysisRun.Asset.Symbol,
                CompanyName = analysisRun.Asset.Name ?? analysisRun.Asset.Symbol,
                Pattern = primaryPattern?.Pattern ?? analysisRun.RequestedPattern,
                Phase = primaryPattern?.Phase ?? string.Empty,
                Probability = primaryPattern?.Confidence ?? analysisRun.DecisionSignal?.Confidence ?? 0m,
                RecommendationAction = recommendationAction,
                RecommendationReason = analysisRun.DecisionSignal?.Reason ?? "Aucune justification",
                RiskLevel = InferRiskLevel(primaryPattern?.Confidence ?? analysisRun.DecisionSignal?.Confidence ?? 0m, isActionable),
                RecommendationHorizonDays = analysisRun.DecisionSignal?.HorizonDays ?? 0,
                PredictedAt = analysisRun.CompletedAtUtc ?? analysisRun.StartedAtUtc,
                IsActionable = isActionable,
                ModelStatus = modelStatus,
                ModelMessage = modelMessage,
                CurrentPrice = primaryPattern?.CurrentPrice ?? 0m,
                NecklinePrice = primaryPattern?.NecklinePrice,
                TargetPrice = primaryPattern?.TargetPrice,
                InvalidationPrice = primaryPattern?.InvalidationPrice
            };
        }

        private static TradingPatternEnum MapPatternId(string? patternId)
        {
            var normalizedPatternId = (patternId ?? string.Empty).Trim().ToUpperInvariant();
            return normalizedPatternId switch
            {
                "DOUBLE_TOP" => TradingPatternEnum.DoubleTop,
                _ => throw new InvalidOperationException($"Le runtime V1 actif ne prend pas en charge le pattern {normalizedPatternId}.")
            };
        }

        private static RecommendationActionEnum MapRecommendationKind(RecommendationKind? kind)
        {
            return kind switch
            {
                RecommendationKind.Buy => RecommendationActionEnum.Buy,
                RecommendationKind.Reinforce => RecommendationActionEnum.Buy,
                RecommendationKind.Lighten => RecommendationActionEnum.Sell,
                RecommendationKind.Sell => RecommendationActionEnum.Sell,
                _ => RecommendationActionEnum.Hold
            };
        }

        private static RiskLevelEnum InferRiskLevel(decimal confidence, bool actionable)
        {
            if (!actionable)
            {
                return RiskLevelEnum.Information;
            }

            if (confidence >= 0.75m)
            {
                return RiskLevelEnum.Low;
            }

            if (confidence >= 0.45m)
            {
                return RiskLevelEnum.Moderate;
            }

            return RiskLevelEnum.High;
        }
    }
}
