using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Contracts;

namespace BackPredictFinance.Patterns.Common
{
    public abstract class ContinuationPatternDefinitionBase : PatternDefinitionBase<ContinuationPatternAnalysisState>
    {
        protected ContinuationPatternDefinitionBase(IPatternMarketDataProvider marketDataProvider)
            : base(marketDataProvider)
        {
        }

        protected override ExecutedPatternArtifact BuildArtifact(AnalysisRequest request, IReadOnlyList<TickerCandle> candles, ContinuationPatternAnalysisState state)
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
                NecklinePrice = state.ReferencePrice.HasValue ? decimal.Round(state.ReferencePrice.Value, 4) : null,
                TargetPrice = state.TargetPrice.HasValue ? decimal.Round(state.TargetPrice.Value, 4) : null,
                InvalidationPrice = state.InvalidationPrice.HasValue ? decimal.Round(state.InvalidationPrice.Value, 4) : null,
                IsPrimary = state.IsPrimary,
                ContractAssessment = assessment
            };
        }
    }
}
