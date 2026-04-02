using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1;

namespace BackPredictFinance.Services.ClientFinanceServices.AnalysisV1
{
    public sealed class RecommendationPolicyService : IRecommendationPolicyService
    {
        private readonly ITradingRecommendationService _tradingRecommendationService;

        public RecommendationPolicyService(ITradingRecommendationService tradingRecommendationService)
        {
            _tradingRecommendationService = tradingRecommendationService;
        }

        public Recommendation EvaluateAnalysis(ResolvedAnalysisRunRequest request, AnalysisExecutionArtifact executionArtifact)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(executionArtifact);

            var primaryPattern = executionArtifact.Patterns
                .OrderByDescending(executedPattern => executedPattern.IsPrimary)
                .ThenByDescending(executedPattern => executedPattern.Confidence)
                .ThenByDescending(executedPattern => executedPattern.Probability)
                .FirstOrDefault();

            if (primaryPattern == null)
            {
                return new Recommendation
                {
                    RecommendationId = Guid.NewGuid().ToString("N"),
                    Kind = RecommendationKind.Wait,
                    HoldingContext = request.HoldsInstrument ? "HELD" : "NOT_HELD",
                    Rationale = "Aucun signal exploitable n'a ete conserve pour formuler une recommandation.",
                    BasedOnPatternIds = [],
                    ReviewHorizonDays = null,
                    PolicyVersion = "legacy-compat-v1"
                };
            }

            var legacyResult = _tradingRecommendationService.EvaluateAnalysis(
                primaryPattern.Pattern,
                primaryPattern.Phase,
                primaryPattern.Probability,
                primaryPattern.TargetPrice,
                primaryPattern.InvalidationPrice);

            return new Recommendation
            {
                RecommendationId = Guid.NewGuid().ToString("N"),
                Kind = MapRecommendationKind(legacyResult.Action),
                HoldingContext = request.HoldsInstrument ? "HELD" : "NOT_HELD",
                Rationale = legacyResult.Reason,
                BasedOnPatternIds = executionArtifact.Patterns
                    .Select(executedPattern => executedPattern.ContractAssessment.PatternId)
                    .Where(patternId => !string.IsNullOrWhiteSpace(patternId))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                ReviewHorizonDays = legacyResult.HorizonDays > 0 ? legacyResult.HorizonDays : null,
                PolicyVersion = "legacy-compat-v1",
                WarningText = executionArtifact.Patterns.Count > 1
                    ? "Plusieurs patterns restent compatibles; la recommandation privilegie le pattern principal."
                    : null
            };
        }

        private static RecommendationKind MapRecommendationKind(RecommendationActionEnum action)
        {
            return action switch
            {
                RecommendationActionEnum.Buy => RecommendationKind.Buy,
                RecommendationActionEnum.Sell => RecommendationKind.Sell,
                _ => RecommendationKind.Wait
            };
        }
    }
}
