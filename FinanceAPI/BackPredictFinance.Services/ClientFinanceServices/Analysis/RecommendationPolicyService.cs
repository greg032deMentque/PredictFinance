using BackPredictFinance.Common.enums;
using BackPredictFinance.Contracts.Analysis;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public interface IRecommendationPolicyService
    {
        AnalysisRecommendation EvaluateAnalysis(AnalysisRequest request, IReadOnlyList<PatternAssessment> compatiblePatterns, AnalysisOutcomeEnum outcome);
    }

    public sealed class RecommendationPolicyService : IRecommendationPolicyService
    {
        private const string PolicyVersion = "analysis-v1-policy@prompt10";

        public AnalysisRecommendation EvaluateAnalysis(AnalysisRequest request, IReadOnlyList<PatternAssessment> compatiblePatterns, AnalysisOutcomeEnum outcome)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(compatiblePatterns);

            var holdingContext = request.PortfolioContext.HoldsInstrument ? "HELD" : "NOT_HELD";
            var basedOnPatternIds = compatiblePatterns.Select(x => x.PatternId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (compatiblePatterns.Count == 0 || outcome == AnalysisOutcomeEnum.NoCrediblePattern)
            {
                return BuildNoCrediblePatternRecommendation(request, holdingContext, basedOnPatternIds);
            }

            var primaryPattern = compatiblePatterns
                .OrderByDescending(x => x.Trace.IsPrimaryDisplayCandidate)
                .ThenByDescending(x => x.Scoring.ConfidenceScore)
                .First();

            var action = ResolveRecommendationAction(request.PortfolioContext.HoldsInstrument, primaryPattern);
            var situationCode = ResolveSituationCode(primaryPattern, outcome);
            var scenarioCode = ResolveAdviceScenarioCode(request.PortfolioContext.HoldsInstrument, primaryPattern, action);
            var parameters = BuildParameters(request, compatiblePatterns, primaryPattern, action);
            var strength = ResolveRecommendationStrength(primaryPattern, parameters, action);

            return new AnalysisRecommendation
            {
                RecommendationId = Guid.NewGuid().ToString("N"),
                SituationCode = situationCode,
                AdviceScenarioCode = scenarioCode,
                RecommendationAction = action,
                RecommendationStrength = strength,
                Parameters = parameters,
                HoldingContext = holdingContext,
                Rationale = BuildRationale(scenarioCode, strength, parameters),
                BasedOnPatternIds = basedOnPatternIds,
                ReviewHorizonDays = primaryPattern.Detection.Status == PatternStatusEnum.Confirmed ? 20 : 10,
                PolicyVersion = PolicyVersion,
                WarningText = compatiblePatterns.Count > 1 ? "Plusieurs scenarios compatibles restent actifs." : null
            };
        }

        private static AnalysisRecommendation BuildNoCrediblePatternRecommendation(AnalysisRequest request, string holdingContext, List<string> basedOnPatternIds)
        {
            var action = request.PortfolioContext.HoldsInstrument ? RecommendationActionEnum.Hold : RecommendationActionEnum.Wait;
            return new AnalysisRecommendation
            {
                RecommendationId = Guid.NewGuid().ToString("N"),
                SituationCode = RecommendationSituationCode.NoCrediblePattern,
                AdviceScenarioCode = request.PortfolioContext.HoldsInstrument ? AdviceScenarioCode.NoCrediblePatternHeld : AdviceScenarioCode.NoCrediblePatternNotHeld,
                RecommendationAction = action,
                RecommendationStrength = RecommendationStrengthEnum.Low,
                Parameters = new AnalysisRecommendationParameters
                {
                    HoldsInstrument = request.PortfolioContext.HoldsInstrument,
                    IsActionable = false,
                    HasMultipleCompatiblePatterns = false,
                    HasRiskPlan = false
                },
                HoldingContext = holdingContext,
                Rationale = request.PortfolioContext.HoldsInstrument ? "Aucun pattern credible n'impose de modifier votre position a ce stade." : "Aucun pattern credible ne justifie une prise de position immediate.",
                BasedOnPatternIds = basedOnPatternIds,
                ReviewHorizonDays = 5,
                PolicyVersion = PolicyVersion
            };
        }

        private static RecommendationActionEnum ResolveRecommendationAction(bool holdsInstrument, PatternAssessment primaryPattern)
        {
            var isBearish = string.Equals(primaryPattern.BiasCode, "BEARISH", StringComparison.OrdinalIgnoreCase);
            var isValidated = string.Equals(primaryPattern.Validation.State, "VALIDATED", StringComparison.OrdinalIgnoreCase) || primaryPattern.Detection.Status == PatternStatusEnum.Confirmed;
            var highConfidence = primaryPattern.Scoring.ConfidenceScore >= 0.75m;
            var moderateConfidence = primaryPattern.Scoring.ConfidenceScore >= 0.45m;
            var hasRiskPlan = primaryPattern.RiskHints.HasRiskPlan;

            if (!primaryPattern.Detection.IsCompatible || primaryPattern.Detection.Status == PatternStatusEnum.Invalidated)
            {
                return holdsInstrument ? RecommendationActionEnum.Wait : RecommendationActionEnum.Wait;
            }

            if (isBearish)
            {
                if (!holdsInstrument)
                {
                    return isValidated && highConfidence ? RecommendationActionEnum.Monitor : RecommendationActionEnum.Wait;
                }

                if (isValidated && highConfidence && hasRiskPlan)
                {
                    return RecommendationActionEnum.Sell;
                }

                if ((isValidated && moderateConfidence) || primaryPattern.Invalidation.InvalidationLevel.HasValue)
                {
                    return RecommendationActionEnum.Lighten;
                }

                return RecommendationActionEnum.Wait;
            }

            if (!holdsInstrument)
            {
                if (isValidated && moderateConfidence)
                {
                    return RecommendationActionEnum.Buy;
                }

                return primaryPattern.Detection.Status is PatternStatusEnum.Forming or PatternStatusEnum.Confirmed
                    ? RecommendationActionEnum.Monitor
                    : RecommendationActionEnum.Wait;
            }

            if (isValidated && highConfidence && hasRiskPlan)
            {
                return RecommendationActionEnum.Reinforce;
            }

            return primaryPattern.Detection.IsCompatible ? RecommendationActionEnum.Hold : RecommendationActionEnum.Wait;
        }

        private static string ResolveSituationCode(PatternAssessment primaryPattern, AnalysisOutcomeEnum outcome)
        {
            if (outcome == AnalysisOutcomeEnum.NoCrediblePattern || !primaryPattern.Detection.IsCompatible)
            {
                return RecommendationSituationCode.NoCrediblePattern;
            }

            return primaryPattern.Detection.Status switch
            {
                PatternStatusEnum.Forming => RecommendationSituationCode.CompatiblePatternForming,
                PatternStatusEnum.Monitoring => RecommendationSituationCode.CompatiblePatternMonitoring,
                PatternStatusEnum.Confirmed => RecommendationSituationCode.CompatiblePatternConfirmed,
                PatternStatusEnum.Completed => RecommendationSituationCode.CompatiblePatternCompleted,
                PatternStatusEnum.Invalidated => RecommendationSituationCode.CompatiblePatternInvalidated,
                _ => RecommendationSituationCode.NoCrediblePattern
            };
        }

        private static string ResolveAdviceScenarioCode(bool holdsInstrument, PatternAssessment primaryPattern, RecommendationActionEnum action)
        {
            var isBearish = string.Equals(primaryPattern.BiasCode, "BEARISH", StringComparison.OrdinalIgnoreCase);
            if (holdsInstrument)
            {
                if (isBearish)
                {
                    return AdviceScenarioCode.PrimaryPatternHeldAdverse;
                }

                return action == RecommendationActionEnum.Reinforce ? AdviceScenarioCode.PrimaryPatternHeldBullish : AdviceScenarioCode.PrimaryPatternHeldNeutral;
            }

            if (isBearish)
            {
                return action == RecommendationActionEnum.Monitor ? AdviceScenarioCode.PrimaryPatternNotHeldAdverse : AdviceScenarioCode.PrimaryPatternNotHeldNeutral;
            }

            return action == RecommendationActionEnum.Buy ? AdviceScenarioCode.PrimaryPatternNotHeldBullish : AdviceScenarioCode.PrimaryPatternNotHeldNeutral;
        }

        private static AnalysisRecommendationParameters BuildParameters(AnalysisRequest request, IReadOnlyList<PatternAssessment> compatiblePatterns, PatternAssessment primaryPattern, RecommendationActionEnum action)
        {
            return new AnalysisRecommendationParameters
            {
                HoldsInstrument = request.PortfolioContext.HoldsInstrument,
                IsActionable = action is RecommendationActionEnum.Buy or RecommendationActionEnum.Reinforce or RecommendationActionEnum.Lighten or RecommendationActionEnum.Sell,
                HasMultipleCompatiblePatterns = compatiblePatterns.Count > 1,
                HasRiskPlan = primaryPattern.RiskHints.HasRiskPlan,
                PrimaryPatternId = primaryPattern.PatternId,
                PrimaryPatternDisplayName = primaryPattern.DisplayName,
                PatternFamilyId = primaryPattern.FamilyId,
                BiasCode = primaryPattern.BiasCode,
                CurrentPhaseCode = primaryPattern.Detection.CurrentPhaseCode,
                CurrentPhaseLabel = primaryPattern.Detection.CurrentPhaseLabel,
                ValidationState = primaryPattern.Validation.State,
                InvalidationState = primaryPattern.Invalidation.State,
                ConfidenceScore = primaryPattern.Scoring.ConfidenceScore,
                ConfidenceLabel = primaryPattern.Scoring.ConfidenceLabel,
                SuggestedStopLoss = primaryPattern.RiskHints.SuggestedStopLoss,
                SuggestedTakeProfit = primaryPattern.RiskHints.SuggestedTakeProfit,
                InvalidationLevel = primaryPattern.Invalidation.InvalidationLevel,
                RiskRewardRatio = primaryPattern.RiskHints.RiskRewardRatio
            };
        }

        private static RecommendationStrengthEnum ResolveRecommendationStrength(PatternAssessment primaryPattern, AnalysisRecommendationParameters parameters, RecommendationActionEnum action)
        {
            if (!parameters.IsActionable)
            {
                return RecommendationStrengthEnum.Low;
            }

            var highConfidence = primaryPattern.Scoring.ConfidenceScore >= 0.75m;
            var moderateConfidence = primaryPattern.Scoring.ConfidenceScore >= 0.45m;
            var hasPositiveRiskReward = !parameters.RiskRewardRatio.HasValue || parameters.RiskRewardRatio.Value >= 1.2m;
            if ((action is RecommendationActionEnum.Buy or RecommendationActionEnum.Reinforce or RecommendationActionEnum.Sell) && highConfidence && hasPositiveRiskReward)
            {
                return RecommendationStrengthEnum.High;
            }

            if (moderateConfidence)
            {
                return RecommendationStrengthEnum.Moderate;
            }

            return RecommendationStrengthEnum.Low;
        }

        private static string BuildRationale(string adviceScenarioCode, RecommendationStrengthEnum RecommendationStrengthEnum, AnalysisRecommendationParameters parameters)
        {
            var patternName = string.IsNullOrWhiteSpace(parameters.PrimaryPatternDisplayName) ? "le scenario principal" : parameters.PrimaryPatternDisplayName;
            var phaseLabel = string.IsNullOrWhiteSpace(parameters.CurrentPhaseLabel) ? "en cours" : parameters.CurrentPhaseLabel.ToLowerInvariant();
            var confidenceLabel = string.IsNullOrWhiteSpace(parameters.ConfidenceLabel) ? "non classee" : parameters.ConfidenceLabel.ToLowerInvariant();
            var strengthLabel = RecommendationStrengthEnum.ToString().ToUpperInvariant();
            return adviceScenarioCode switch
            {
                AdviceScenarioCode.PrimaryPatternHeldBullish => $"{patternName} reste {phaseLabel} avec une confiance {confidenceLabel}. La posture retenue est un renforcement prudent d'intensite {strengthLabel}.",
                AdviceScenarioCode.PrimaryPatternHeldAdverse => $"{patternName} indique un scenario adverse {phaseLabel} avec une confiance {confidenceLabel}. La posture retenue est de reduire l'exposition avec une intensite {strengthLabel}.",
                AdviceScenarioCode.PrimaryPatternHeldNeutral => $"{patternName} reste {phaseLabel} avec une confiance {confidenceLabel}. La posture retenue est de conserver une lecture prudente.",
                AdviceScenarioCode.PrimaryPatternNotHeldBullish => $"{patternName} reste {phaseLabel} avec une confiance {confidenceLabel}. Une entree pedagogique peut etre envisagee avec une intensite {strengthLabel}.",
                AdviceScenarioCode.PrimaryPatternNotHeldAdverse => $"{patternName} reste {phaseLabel} avec une confiance {confidenceLabel}. Le scenario reste baissier et la posture est de surveiller sans entrer.",
                AdviceScenarioCode.PrimaryPatternNotHeldNeutral => $"{patternName} reste {phaseLabel} avec une confiance {confidenceLabel}. La posture retenue reste d'attendre un signal plus net.",
                AdviceScenarioCode.NoCrediblePatternHeld => "Aucun pattern credible n'impose de modifier votre position a ce stade.",
                AdviceScenarioCode.NoCrediblePatternNotHeld => "Aucun pattern credible ne justifie une prise de position immediate.",
                _ => "Aucune prise de position immediate n'est recommandee."
            };
        }
    }
}
