using BackPredictFinance.Contracts.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public sealed class RiskEvaluationService : IRiskEvaluationService
    {
        public PatternRiskHints EvaluatePrimaryRisk(AnalysisExecutionArtifact executionArtifact, PatternAssessment patternAssessment)
        {
            ArgumentNullException.ThrowIfNull(executionArtifact);
            ArgumentNullException.ThrowIfNull(patternAssessment);

            var executedPattern = executionArtifact.Patterns
                .FirstOrDefault(x => string.Equals(x.ContractAssessment.AssessmentId, patternAssessment.AssessmentId, StringComparison.Ordinal));

            var currentPrice = patternAssessment.Detection.CurrentPrice;
            var suggestedStopLoss = patternAssessment.Invalidation.InvalidationLevel;
            var suggestedTakeProfit = executedPattern?.TargetPrice;
            var riskRewardRatio = BuildRiskRewardRatio(currentPrice, suggestedStopLoss, suggestedTakeProfit);
            var hasRiskPlan = suggestedStopLoss.HasValue || suggestedTakeProfit.HasValue || riskRewardRatio.HasValue;

            return new PatternRiskHints
            {
                HasRiskPlan = hasRiskPlan,
                SuggestedStopLoss = suggestedStopLoss,
                SuggestedTakeProfit = suggestedTakeProfit,
                RiskRewardRatio = riskRewardRatio,
                PositioningNote = BuildPositioningNote(patternAssessment, hasRiskPlan, suggestedStopLoss, suggestedTakeProfit)
            };
        }

        private static decimal? BuildRiskRewardRatio(decimal currentPrice, decimal? stopLoss, decimal? takeProfit)
        {
            if (!stopLoss.HasValue || !takeProfit.HasValue)
            {
                return null;
            }

            var risk = Math.Abs(currentPrice - stopLoss.Value);
            if (risk <= 0m)
            {
                return null;
            }

            var reward = Math.Abs(takeProfit.Value - currentPrice);
            if (reward <= 0m)
            {
                return null;
            }

            return Math.Round(reward / risk, 4);
        }

        private static string BuildPositioningNote(PatternAssessment patternAssessment, bool hasRiskPlan, decimal? stopLoss, decimal? takeProfit)
        {
            if (!patternAssessment.Detection.IsCompatible)
            {
                return "Aucun plan de risque n'est retenu car le pattern n'est pas compatible.";
            }

            if (patternAssessment.Invalidation.State == "INVALIDATED")
            {
                return "Le scenario est invalide a ce stade et ne justifie plus de plan de risque actif.";
            }

            if (hasRiskPlan && stopLoss.HasValue && takeProfit.HasValue)
            {
                return "Le plan de risque est derive des niveaux techniques detectes pour ce scenario.";
            }

            if (hasRiskPlan)
            {
                return "Le scenario reste suivi, mais le plan de risque est partiel car tous les niveaux techniques ne sont pas disponibles.";
            }

            return "Aucun plan de risque exploitable n'a pu etre derive de ce scenario.";
        }
    }
}
