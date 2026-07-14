using System.Text.Json;
using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Contracts;

namespace BackPredictFinance.Patterns.Common
{
    public abstract class ContinuationPatternDefinitionBase : IAnalysisPatternDefinition
    {
        private readonly IPatternMarketDataProvider _marketDataProvider;

        protected ContinuationPatternDefinitionBase(IPatternMarketDataProvider marketDataProvider)
        {
            _marketDataProvider = marketDataProvider;
        }

        public abstract string PatternId { get; }
        public abstract string ModelVersion { get; }
        public abstract int HistoryLookbackMonths { get; }
        protected abstract int MinimumRequiredCandles { get; }
        protected abstract string DisplayName { get; }
        protected abstract string PedagogicalDescription { get; }
        protected abstract ContinuationPatternAnalysisState Analyze(AnalysisRequest request, IReadOnlyList<TickerCandle> candles);

        public ResolvedAnalysisPattern BuildResolvedPattern()
        {
            return new ResolvedAnalysisPattern
            {
                PatternId = PatternId,
                ModelVersion = ModelVersion,
                HistoryLookbackMonths = HistoryLookbackMonths
            };
        }

        public async Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var requestedCandleCount = PatternTechnicals.BuildRequestedCandleCount(request.HistoryStartDate, request.HistoryEndDate, MinimumRequiredCandles);
            var timeSeries = await _marketDataProvider.GetTimeSeriesAsync(
                request.Instrument.Symbol,
                string.IsNullOrWhiteSpace(request.CandleInterval) ? "1d" : request.CandleInterval.Trim(),
                requestedCandleCount,
                ct);

            var candles = timeSeries.Candles
                .OrderBy(candle => candle.Date)
                .ToList();

            if (candles.Count == 0)
            {
                throw new InvalidOperationException("Aucune donnee de marche exploitable n'a ete retournee pour l'analyse.");
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
                ModelMessage = "Analyse V1 produite par le moteur deterministe API.",
                ModelVersion = ModelVersion,
                RawProviderPayloadJson = rawPayload,
                Candles = candles
            };
        }

        private ExecutedPatternArtifact BuildArtifact(AnalysisRequest request, IReadOnlyList<TickerCandle> candles, ContinuationPatternAnalysisState state)
        {
            var assessmentId = Guid.NewGuid().ToString("N");
            var confidence = PatternTechnicals.Clamp01(state.Confidence);
            var latestTimestamp = candles[^1].Date;
            var assessment = new PatternAssessmentContract
            {
                AssessmentId = assessmentId,
                PatternId = PatternId,
                DisplayName = DisplayName,
                PedagogicalDescription = PedagogicalDescription,
                AnalysisWindow = new PatternAnalysisWindow
                {
                    Interval = string.IsNullOrWhiteSpace(request.CandleInterval) ? "1d" : request.CandleInterval.Trim(),
                    StartDate = request.HistoryStartDate,
                    EndDate = request.HistoryEndDate,
                    RequiredCandles = MinimumRequiredCandles,
                    ActualCandles = candles.Count
                },
                Detection = new PatternDetection
                {
                    IsCompatible = state.IsCompatible,
                    Status = state.Status,
                    CurrentPhaseCode = state.PhaseCode,
                    CurrentPhaseLabel = state.PhaseLabel,
                    StatusReason = state.StatusReason,
                    CurrentPrice = decimal.Round(state.CurrentPrice, 4),
                    StructuralPoints = state.StructuralPoints
                },
                Validation = new PatternValidation
                {
                    State = state.IsValidated ? "VALIDATED" : "NOT_VALIDATED",
                    Reason = state.ValidationReason,
                    ValidatedAtDate = state.IsValidated ? DateOnly.FromDateTime(latestTimestamp) : null,
                    ValidatedAtPrice = state.IsValidated ? decimal.Round(state.CurrentPrice, 4) : null,
                    ValidationRuleCode = state.IsValidated ? state.ValidationRuleCode : null
                },
                Invalidation = new PatternInvalidation
                {
                    State = state.IsInvalidated ? "INVALIDATED" : "ACTIVE",
                    Reason = state.InvalidationReason,
                    InvalidationLevel = state.InvalidationPrice.HasValue ? decimal.Round(state.InvalidationPrice.Value, 4) : null,
                    BreachedAtDate = state.IsInvalidated ? DateOnly.FromDateTime(latestTimestamp) : null,
                    BreachedAtPrice = state.IsInvalidated ? decimal.Round(state.CurrentPrice, 4) : null,
                    InvalidationRuleCode = state.IsInvalidated ? state.InvalidationRuleCode : null
                },
                Scoring = new PatternScoring
                {
                    ConfidenceScore = confidence,
                    ConfidenceLabel = BuildConfidenceLabel(confidence),
                    IsCredible = state.IsCompatible,
                    ScoreReasons = state.ScoreReasons,
                    ScoreVersion = ModelVersion
                },
                RiskHints = new PatternRiskHints
                {
                    HasRiskPlan = state.TargetPrice.HasValue || state.InvalidationPrice.HasValue,
                    SuggestedStopLoss = state.InvalidationPrice,
                    SuggestedTakeProfit = state.TargetPrice,
                    RiskRewardRatio = BuildRiskRewardRatio(state.CurrentPrice, state.TargetPrice, state.InvalidationPrice)
                },
                Explanation = new PatternExplanation(),
                Trace = new PatternTrace
                {
                    PatternVersion = ModelVersion,
                    RuleSetVersion = ModelVersion,
                    IsPrimaryDisplayCandidate = state.IsPrimary,
                    ScoringVersion = ModelVersion
                }
            };

            return new ExecutedPatternArtifact
            {
                PatternId = PatternId,
                Phase = state.PhaseCode,
                Probability = confidence,
                Confidence = confidence,
                CurrentPrice = decimal.Round(state.CurrentPrice, 4),
                NecklinePrice = state.ReferencePrice.HasValue ? decimal.Round(state.ReferencePrice.Value, 4) : null,
                TargetPrice = state.TargetPrice.HasValue ? decimal.Round(state.TargetPrice.Value, 4) : null,
                InvalidationPrice = state.InvalidationPrice.HasValue ? decimal.Round(state.InvalidationPrice.Value, 4) : null,
                IsPrimary = state.IsPrimary,
                ContractAssessment = assessment
            };
        }

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
