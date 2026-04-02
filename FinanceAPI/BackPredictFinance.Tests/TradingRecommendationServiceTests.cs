using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.ClientFinanceServices;

namespace BackPredictFinance.Tests
{
    public class TradingRecommendationServiceTests
    {
        private readonly TradingRecommendationService _service = new();

        [Fact]
        public void EvaluateAnalysis_ReturnsSell_ForConfirmedBearishDoubleTop()
        {
            var result = _service.EvaluateAnalysis(
                TradingPatternEnum.DoubleTop,
                "neckline_break_confirmed",
                0.72m,
                88m,
                103m);

            Assert.Equal(RecommendationActionEnum.Sell, result.Action);
            Assert.True(result.IsActionable);
            Assert.Equal(RiskLevelEnum.Moderate, result.RiskLevel);
            Assert.Contains("Sell", result.Reason, StringComparison.Ordinal);
        }

        [Fact]
        public void EvaluateAnalysis_ReturnsHold_WhenPatternIsStillObservational()
        {
            var result = _service.EvaluateAnalysis(
                TradingPatternEnum.DoubleTop,
                "second_peak_candidate",
                0.48m,
                null,
                null);

            Assert.Equal(RecommendationActionEnum.Hold, result.Action);
            Assert.False(result.IsActionable);
            Assert.Equal(RiskLevelEnum.Information, result.RiskLevel);
        }
    }
}
