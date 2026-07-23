using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.ClientFinanceServices;

namespace BackPredictFinance.Tests.Analysis;

public sealed class TradingRecommendationServiceTests
{
    private readonly ITradingRecommendationService _service = new TradingRecommendationService();

    [Theory]
    [InlineData("bullish_breakout_confirmed", 0.60)]
    [InlineData("double_bottom_breakout_confirmed", 0.61)]
    [InlineData("inverse_hs_breakout_confirmed", 0.75)]
    public void EvaluateAnalysis_BullishConfirmedAboveThreshold_ReturnsActionableBuy(string phase, double confidence)
    {
        var result = _service.EvaluateAnalysis(phase, (decimal)confidence, targetPrice: 150m, invalidationPrice: 100m);

        Assert.Equal(RecommendationActionEnum.Buy, result.Action);
        Assert.True(result.IsActionable);
        Assert.Equal(20, result.HorizonDays);
        Assert.Equal((decimal)confidence, result.Confidence);
        Assert.Contains("haussier", result.Reason);
    }

    [Fact]
    public void EvaluateAnalysis_BullishConfirmedJustBelowThreshold_ReturnsNonActionableHold()
    {
        var result = _service.EvaluateAnalysis("bullish_breakout_confirmed", 0.59m, targetPrice: null, invalidationPrice: null);

        Assert.Equal(RecommendationActionEnum.Hold, result.Action);
        Assert.False(result.IsActionable);
        Assert.Equal(RiskLevelEnum.Information, result.RiskLevel);
        Assert.Equal(10, result.HorizonDays);
        Assert.Contains("bullish_breakout_confirmed", result.Reason);
    }

    [Theory]
    [InlineData("bearish_breakout_confirmed", 0.60)]
    [InlineData("double_top_breakout_confirmed", 0.61)]
    [InlineData("hs_breakdown_confirmed", 0.75)]
    public void EvaluateAnalysis_BearishConfirmedAboveThreshold_ReturnsActionableSell(string phase, double confidence)
    {
        var result = _service.EvaluateAnalysis(phase, (decimal)confidence, targetPrice: 90m, invalidationPrice: 130m);

        Assert.Equal(RecommendationActionEnum.Sell, result.Action);
        Assert.True(result.IsActionable);
        Assert.Equal(20, result.HorizonDays);
        Assert.Contains("baissier", result.Reason);
    }

    [Fact]
    public void EvaluateAnalysis_BearishConfirmedJustBelowThreshold_ReturnsNonActionableHold()
    {
        var result = _service.EvaluateAnalysis("bearish_breakout_confirmed", 0.59m, targetPrice: null, invalidationPrice: null);

        Assert.Equal(RecommendationActionEnum.Hold, result.Action);
        Assert.False(result.IsActionable);
        Assert.Equal(RiskLevelEnum.Information, result.RiskLevel);
    }

    [Theory]
    [InlineData("invalidated")]
    [InlineData("opposite_breakout_invalidated")]
    [InlineData("flag_support_broken")]
    [InlineData("flag_resistance_broken")]
    [InlineData("legacy_pattern_not_enabled")]
    public void EvaluateAnalysis_InvalidatedPhase_ReturnsNonActionableHoldRegardlessOfConfidence(string phase)
    {
        var result = _service.EvaluateAnalysis(phase, 0.90m, targetPrice: 150m, invalidationPrice: 100m);

        Assert.Equal(RecommendationActionEnum.Hold, result.Action);
        Assert.False(result.IsActionable);
        Assert.Equal(RiskLevelEnum.Information, result.RiskLevel);
        Assert.Equal(10, result.HorizonDays);
        Assert.Equal(
            "Le scenario de continuation n'est pas exploitable dans sa forme actuelle. Aucune posture directionnelle n'est retenue.",
            result.Reason);
    }

    [Theory]
    [InlineData("", 0.30)]
    [InlineData("bullish_monitoring", 0.50)]
    [InlineData("some_unrecognized_phase", 0.10)]
    public void EvaluateAnalysis_UnrecognizedOrFormingPhase_ReturnsDefaultHold(string phase, double confidence)
    {
        var result = _service.EvaluateAnalysis(phase, (decimal)confidence, targetPrice: null, invalidationPrice: null);

        Assert.Equal(RecommendationActionEnum.Hold, result.Action);
        Assert.False(result.IsActionable);
        Assert.Equal(RiskLevelEnum.Information, result.RiskLevel);
        Assert.Equal(10, result.HorizonDays);
    }

    [Fact]
    public void EvaluateAnalysis_BlankPhase_ReasonUsesFallbackLabel()
    {
        var result = _service.EvaluateAnalysis(" ", 0.30m, targetPrice: null, invalidationPrice: null);

        Assert.Contains("phase_inconnue", result.Reason);
    }

    [Theory]
    [InlineData(0.60, RiskLevelEnum.Moderate)]
    [InlineData(0.75, RiskLevelEnum.Low)]
    public void EvaluateAnalysis_ActionableConfidence_MapsToExpectedRiskLevel(double confidence, RiskLevelEnum expectedRiskLevel)
    {
        var result = _service.EvaluateAnalysis("bullish_breakout_confirmed", (decimal)confidence, targetPrice: null, invalidationPrice: null);

        Assert.Equal(expectedRiskLevel, result.RiskLevel);
    }

    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(1.5, 1.0)]
    [InlineData(0.5, 0.5)]
    public void EvaluateAnalysis_OutOfRangeConfidence_IsClampedToZeroOneRange(double rawConfidence, double expectedConfidence)
    {
        var result = _service.EvaluateAnalysis("invalidated", (decimal)rawConfidence, targetPrice: null, invalidationPrice: null);

        Assert.Equal((decimal)expectedConfidence, result.Confidence);
    }
}
