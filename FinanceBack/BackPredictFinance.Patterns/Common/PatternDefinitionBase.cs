using System.Text.Json;
using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Contracts;

namespace BackPredictFinance.Patterns.Common
{
    public abstract class PatternDefinitionBase<TState> : IAnalysisPatternDefinition
        where TState : PatternAnalysisStateBase
    {
        private readonly IPatternMarketDataProvider _marketDataProvider;

        protected PatternDefinitionBase(IPatternMarketDataProvider marketDataProvider)
        {
            _marketDataProvider = marketDataProvider;
        }

        public abstract string PatternId { get; }
        public abstract string ModelVersion { get; }
        public abstract int HistoryLookbackMonths { get; }
        protected abstract int MinimumRequiredCandles { get; }
        protected abstract string DisplayName { get; }
        protected abstract string PedagogicalDescription { get; }
        protected abstract decimal HistoricalReliability { get; }

        protected abstract TState Analyze(AnalysisRequest request, IReadOnlyList<TickerCandle> candles);

        protected abstract ExecutedPatternArtifact BuildArtifact(AnalysisRequest request, IReadOnlyList<TickerCandle> candles, TState state);

        public ResolvedAnalysisPattern BuildResolvedPattern()
        {
            return new ResolvedAnalysisPattern
            {
                PatternId = PatternId,
                ModelVersion = ModelVersion,
                HistoryLookbackMonths = HistoryLookbackMonths
            };
        }

        /// <summary>
        /// Pipeline commun a tous les patterns : recupere l'historique, delegue la detection
        /// geometrique a <see cref="Analyze"/> (implementee par chaque figure) puis serialise le
        /// resultat en artefact expose a l'API.
        /// </summary>
        public async Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var requestedCandleCount = PatternTechnicals.BuildRequestedCandleCount(request.HistoryStartDate, request.HistoryEndDate, MinimumRequiredCandles);
            var timeSeries = await _marketDataProvider.GetTimeSeriesAsync(
                request.Instrument.Symbol,
                string.IsNullOrWhiteSpace(request.CandleInterval) ? "1d" : request.CandleInterval.Trim(),
                requestedCandleCount,
                ct);

            // Tri chronologique explicite : les definitions de patterns dependent d'un ordre
            // ascendant strict (fenetres Tail/Take/Skip, pivots, regression) et ne doivent jamais
            // recevoir de bougies desordonnees, quel que soit l'ordre renvoye par le fournisseur.
            var candles = timeSeries.Candles
                .OrderBy(candle => candle.Date)
                .ToList();

            if (candles.Count == 0)
            {
                return BuildNoMarketDataArtifact(request);
            }

            var analysisState = Analyze(request, candles);
            var artifact = BuildArtifact(request, candles, analysisState);
            var rawPayload = JsonSerializer.Serialize(new
            {
                request.Instrument.Symbol,
                PatternId,
                ModelVersion,
                CandleCount = candles.Count,
                analysisState.PhaseCode,
                analysisState.StatusReason,
                analysisState.TargetPrice,
                analysisState.InvalidationPrice
            });

            return new AnalysisExecutionArtifact
            {
                Symbol = request.Instrument.Symbol,
                GeneratedAtUtc = candles[^1].Date,
                Patterns = [artifact],
                ModelStatus = ModelStatusEnum.Go,
                ModelMessage = "Analyse V1 produite par le moteur déterministe API.",
                ModelVersion = ModelVersion,
                RawProviderPayloadJson = rawPayload,
                Candles = candles
            };
        }

        /// <summary>
        /// Produit un artefact "donnees indisponibles" quand le fournisseur ne renvoie aucune bougie.
        /// Le pattern reste present (statut insufficient_history, non compatible) pour que l'explorateur
        /// puisse toujours lister la figure plutot que de faire echouer l'analyse complete.
        /// </summary>
        private AnalysisExecutionArtifact BuildNoMarketDataArtifact(AnalysisRequest request)
        {
            const string phaseCode = "insufficient_history";
            const string statusReason = "Aucune donnee de marche exploitable n'a ete retournee par le fournisseur pour ce symbole.";

            var assessment = new PatternAssessmentContract
            {
                AssessmentId = Guid.NewGuid().ToString("N"),
                PatternId = PatternId,
                DisplayName = DisplayName,
                PedagogicalDescription = PedagogicalDescription,
                AnalysisWindow = new PatternAnalysisWindow
                {
                    Interval = string.IsNullOrWhiteSpace(request.CandleInterval) ? "1d" : request.CandleInterval.Trim(),
                    StartDate = request.HistoryStartDate,
                    EndDate = request.HistoryEndDate,
                    RequiredCandles = MinimumRequiredCandles,
                    ActualCandles = 0
                },
                Detection = new PatternDetection
                {
                    IsCompatible = false,
                    Status = PatternStatus.Forming,
                    CurrentPhaseCode = phaseCode,
                    CurrentPhaseLabel = "Historique insuffisant",
                    StatusReason = statusReason,
                    CurrentPrice = 0m,
                    StructuralPoints = []
                },
                Validation = new PatternValidation
                {
                    State = "NOT_VALIDATED",
                    Reason = "Aucune validation possible sans donnee de marche."
                },
                Invalidation = new PatternInvalidation
                {
                    State = "ACTIVE",
                    Reason = "Aucune invalidation interpretable sans donnee de marche."
                },
                Scoring = new PatternScoring
                {
                    ConfidenceScore = 0m,
                    ConfidenceLabel = BuildConfidenceLabel(0m),
                    ProbabilityScore = HistoricalReliability,
                    ProbabilityLabel = BulkowskiReliability.BuildLabel(HistoricalReliability),
                    IsCredible = false,
                    ScoreReasons = ["Le fournisseur de donnees n'a retourne aucune bougie pour ce symbole."],
                    ScoreVersion = ModelVersion
                },
                RiskHints = new PatternRiskHints(),
                Explanation = new PatternExplanation(),
                Trace = new PatternTrace
                {
                    PatternVersion = ModelVersion,
                    RuleSetVersion = ModelVersion,
                    IsPrimaryDisplayCandidate = false,
                    ScoringVersion = ModelVersion
                }
            };

            var artifact = new ExecutedPatternArtifact
            {
                PatternId = PatternId,
                Phase = phaseCode,
                Probability = HistoricalReliability,
                Confidence = 0m,
                CurrentPrice = 0m,
                IsPrimary = false,
                ContractAssessment = assessment
            };

            return new AnalysisExecutionArtifact
            {
                Symbol = request.Instrument.Symbol,
                GeneratedAtUtc = DateTime.UtcNow,
                Patterns = [artifact],
                ModelStatus = ModelStatusEnum.NoGo,
                ModelMessage = statusReason,
                ModelVersion = ModelVersion,
                RawProviderPayloadJson = string.Empty,
                Candles = []
            };
        }

        protected PatternAssessmentContract BuildAssessment(
            AnalysisRequest request,
            IReadOnlyList<TickerCandle> candles,
            PatternAnalysisStateBase state,
            string assessmentId,
            decimal confidence,
            DateTime latestTimestamp)
        {
            return new PatternAssessmentContract
            {
                AssessmentId = assessmentId,
                PatternId = PatternId,
                DisplayName = DisplayName,
                PedagogicalDescription = PedagogicalDescription,
                Direction = PatternDirectionResolver.Resolve(state.TargetPrice, state.InvalidationPrice),
                AnalysisWindow = BuildAnalysisWindow(request, candles),
                Detection = BuildDetection(state),
                Validation = BuildValidation(state, latestTimestamp),
                Invalidation = BuildInvalidation(state, latestTimestamp),
                Scoring = BuildScoring(state, confidence),
                RiskHints = BuildRiskHints(state),
                Explanation = new PatternExplanation(),
                Trace = new PatternTrace
                {
                    PatternVersion = ModelVersion,
                    RuleSetVersion = ModelVersion,
                    IsPrimaryDisplayCandidate = state.IsPrimary,
                    ScoringVersion = ModelVersion
                }
            };
        }

        private PatternAnalysisWindow BuildAnalysisWindow(AnalysisRequest request, IReadOnlyList<TickerCandle> candles)
        {
            return new PatternAnalysisWindow
            {
                Interval = string.IsNullOrWhiteSpace(request.CandleInterval) ? "1d" : request.CandleInterval.Trim(),
                StartDate = request.HistoryStartDate,
                EndDate = request.HistoryEndDate,
                RequiredCandles = MinimumRequiredCandles,
                ActualCandles = candles.Count
            };
        }

        private static PatternDetection BuildDetection(PatternAnalysisStateBase state)
        {
            return new PatternDetection
            {
                IsCompatible = state.IsCompatible,
                Status = state.Status,
                CurrentPhaseCode = state.PhaseCode,
                CurrentPhaseLabel = state.PhaseLabel,
                StatusReason = state.StatusReason,
                CurrentPrice = decimal.Round(state.CurrentPrice, 4),
                StructuralPoints = state.StructuralPoints
            };
        }

        private static PatternValidation BuildValidation(PatternAnalysisStateBase state, DateTime latestTimestamp)
        {
            return new PatternValidation
            {
                State = state.IsValidated ? "VALIDATED" : "NOT_VALIDATED",
                Reason = state.ValidationReason,
                ValidatedAtDate = state.IsValidated ? DateOnly.FromDateTime(latestTimestamp) : null,
                ValidatedAtPrice = state.IsValidated ? decimal.Round(state.CurrentPrice, 4) : null,
                ValidationRuleCode = state.IsValidated ? state.ValidationRuleCode : null
            };
        }

        private static PatternInvalidation BuildInvalidation(PatternAnalysisStateBase state, DateTime latestTimestamp)
        {
            return new PatternInvalidation
            {
                State = state.IsInvalidated ? "INVALIDATED" : "ACTIVE",
                Reason = state.InvalidationReason,
                InvalidationLevel = state.InvalidationPrice.HasValue ? decimal.Round(state.InvalidationPrice.Value, 4) : null,
                BreachedAtDate = state.IsInvalidated ? DateOnly.FromDateTime(latestTimestamp) : null,
                BreachedAtPrice = state.IsInvalidated ? decimal.Round(state.CurrentPrice, 4) : null,
                InvalidationRuleCode = state.IsInvalidated ? state.InvalidationRuleCode : null
            };
        }

        private PatternScoring BuildScoring(PatternAnalysisStateBase state, decimal confidence)
        {
            return new PatternScoring
            {
                ConfidenceScore = confidence,
                ConfidenceLabel = BuildConfidenceLabel(confidence),
                ProbabilityScore = HistoricalReliability,
                ProbabilityLabel = BulkowskiReliability.BuildLabel(HistoricalReliability),
                IsCredible = state.IsCompatible,
                ScoreReasons = state.ScoreReasons,
                ScoreVersion = ModelVersion
            };
        }

        private static PatternRiskHints BuildRiskHints(PatternAnalysisStateBase state)
        {
            return new PatternRiskHints
            {
                HasRiskPlan = state.TargetPrice.HasValue || state.InvalidationPrice.HasValue,
                SuggestedStopLoss = state.InvalidationPrice,
                SuggestedTakeProfit = state.TargetPrice,
                RiskRewardRatio = BuildRiskRewardRatio(state.CurrentPrice, state.TargetPrice, state.InvalidationPrice)
            };
        }

        // Ratio recompense/risque = distance a la cible / distance a l'invalidation. Retourne null
        // tant que cible/invalidation ne sont pas encore projetees ou si le risque est nul.
        private static decimal? BuildRiskRewardRatio(decimal currentPrice, decimal? targetPrice, decimal? invalidationPrice)
        {
            if (currentPrice <= 0m || !targetPrice.HasValue || !invalidationPrice.HasValue)
            {
                return null;
            }

            var reward = Math.Abs(targetPrice.Value - currentPrice);
            var risk = Math.Abs(currentPrice - invalidationPrice.Value);
            if (risk <= 0m)
            {
                return null;
            }

            return decimal.Round(reward / risk, 4);
        }

        // Paliers de lecture de la confidence geometrique (distincte de la fiabilite Bulkowski) :
        // 0.80 = structure et cassure tres nettes, 0.60 = structure correcte, 0.35 = signal faible
        // mais encore exploitable, en dessous VERY_LOW.
        private static string BuildConfidenceLabel(decimal confidence)
        {
            if (confidence >= 0.80m)
            {
                return "HIGH";
            }

            if (confidence >= 0.60m)
            {
                return "MEDIUM";
            }

            if (confidence >= 0.35m)
            {
                return "LOW";
            }

            return "VERY_LOW";
        }
    }
}
