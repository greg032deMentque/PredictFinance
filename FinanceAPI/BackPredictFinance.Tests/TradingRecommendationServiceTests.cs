using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.Services.PythonServices.Models;

namespace BackPredictFinance.Tests
{
    public class TradingRecommendationServiceTests
    {
        private readonly TradingRecommendationService _service = new();

        [Fact]
        public void EvaluateAnalysis_ReturnsSell_ForConfirmedBearishDoubleTop()
        {
            var prediction = new PredictOut
            {
                Pattern = TradingPatternEnum.DoubleTop,
                Phase = "neckline_break_confirmed",
                LastProbability = 0.72m,
                TargetPrice = 88m,
                InvalidationPrice = 103m
            };

            var result = _service.EvaluateAnalysis(prediction);

            Assert.Equal(RecommendationActionEnum.Sell, result.Action);
            Assert.True(result.IsActionable);
            Assert.Equal(RiskLevelEnum.Moderate, result.RiskLevel);
            Assert.Contains("Sell", result.Reason, StringComparison.Ordinal);
        }

        [Fact]
        public void EvaluateAnalysis_ReturnsHold_WhenPatternIsStillObservational()
        {
            var prediction = new PredictOut
            {
                Pattern = TradingPatternEnum.DoubleTop,
                Phase = "second_peak_candidate",
                LastProbability = 0.48m
            };

            var result = _service.EvaluateAnalysis(prediction);

            Assert.Equal(RecommendationActionEnum.Hold, result.Action);
            Assert.False(result.IsActionable);
            Assert.Equal(RiskLevelEnum.Information, result.RiskLevel);
        }
    }
}
