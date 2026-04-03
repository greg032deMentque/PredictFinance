using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices.AnalysisV1
{
    public sealed class AnalysisLegacyCompatibilityService : BaseService, IAnalysisLegacyCompatibilityService
    {
        public AnalysisLegacyCompatibilityService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public AnalysisResultViewModel MapRunResult(AnalysisResponse response)
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
                Pattern = MapPatternId(primaryPattern?.PatternId),
                Phase = primaryPattern?.Detection.CurrentPhaseCode ?? string.Empty,
                Probability = confidence,
                RecommendationAction = recommendationAction,
                RecommendationReason = recommendation?.Rationale ?? response.PedagogicalSummary,
                RiskLevel = InferRiskLevel(confidence, isActionable),
                RecommendationHorizonDays = recommendation?.ReviewHorizonDays ?? 0,
                PredictedAt = response.GeneratedAtUtc,
                IsActionable = isActionable,
                ModelStatus = ModelStatusEnum.NoGo,
                ModelMessage = string.Join(" ", response.Warnings),
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
                return _mapper.Map<List<AnalysisResultViewModel>>(analysisRuns);
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

        private static TradingPatternEnum MapPatternId(string? patternId)
        {
            return (patternId ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "HEAD_AND_SHOULDERS" => TradingPatternEnum.HeadAndShoulders,
                "DOUBLE_TOP" => TradingPatternEnum.DoubleTop,
                "DOUBLE_BOTTOM" => TradingPatternEnum.DoubleBottom,
                "CUP_AND_HANDLE" => TradingPatternEnum.CupAndHandle,
                "TRIANGLE" => TradingPatternEnum.Triangle,
                _ => TradingPatternEnum.DoubleTop
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
