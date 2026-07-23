using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Contracts;

namespace BackPredictFinance.Patterns.Common
{
    public abstract class ReversalPatternDefinitionBase : PatternDefinitionBase<ReversalPatternAnalysisState>
    {
        protected ReversalPatternDefinitionBase(IPatternMarketDataProvider marketDataProvider)
            : base(marketDataProvider)
        {
        }

        protected override ExecutedPatternArtifact BuildArtifact(AnalysisRequest request, IReadOnlyList<TickerCandle> candles, ReversalPatternAnalysisState state)
        {
            var assessmentId = Guid.NewGuid().ToString("N");
            var confidence = PatternTechnicals.Clamp01(state.Confidence);
            var latestTimestamp = candles[^1].Date;

            var assessment = BuildAssessment(request, candles, state, assessmentId, confidence, latestTimestamp);

            return new ExecutedPatternArtifact
            {
                PatternId = PatternId,
                Phase = state.PhaseCode,
                Probability = HistoricalReliability,
                Confidence = confidence,
                CurrentPrice = decimal.Round(state.CurrentPrice, 4),
                NecklinePrice = state.NecklinePrice.HasValue ? decimal.Round(state.NecklinePrice.Value, 4) : null,
                TargetPrice = state.TargetPrice.HasValue ? decimal.Round(state.TargetPrice.Value, 4) : null,
                InvalidationPrice = state.InvalidationPrice.HasValue ? decimal.Round(state.InvalidationPrice.Value, 4) : null,
                FirstPeakAtUtc = ResolveIndexDate(candles, state.FirstPeakIndex ?? state.LeftShoulderIndex),
                SecondPeakAtUtc = ResolveIndexDate(candles, state.SecondPeakIndex ?? state.RightShoulderIndex),
                IsPrimary = state.IsPrimary,
                ContractAssessment = assessment
            };
        }

        // FirstPeakIndex/SecondPeakIndex couvrent les doubles sommets/creux, LeftShoulderIndex/
        // RightShoulderIndex les figures tete-epaules ; l'appelant passe l'un ou l'autre selon la
        // figure concrete (voir BuildArtifact), cette methode se contente de dater l'indice fourni.
        private static DateTime? ResolveIndexDate(IReadOnlyList<TickerCandle> candles, int? index)
        {
            if (!index.HasValue || index.Value < 0 || index.Value >= candles.Count)
            {
                return null;
            }

            return candles[index.Value].Date;
        }
    }
}
