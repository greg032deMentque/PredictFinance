using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;

namespace BackPredictFinance.Tests.Analysis;

public sealed class VolumeConfidenceAdjustmentTests
{
    private static PatternScoring BuildScoring(decimal confidenceScore)
    {
        return new PatternScoring
        {
            ConfidenceScore = confidenceScore,
            ScoreReasons = []
        };
    }

    private readonly IRiskEvaluationService _service = new RiskEvaluationService();

    [Fact]
    public void ApplyVolumeConfidenceAdjustment_StrongVolume_AddsBonus()
    {
        var scoring = BuildScoring(0.60m);

        _service.ApplyVolumeConfidenceAdjustment(scoring, VolumeConfirmation.Strong);

        Assert.Equal(0.65m, scoring.ConfidenceScore);
        Assert.Contains(scoring.ScoreReasons, r => r.Contains("fort"));
    }

    [Fact]
    public void ApplyVolumeConfidenceAdjustment_WeakVolume_AppliesMalus()
    {
        var scoring = BuildScoring(0.60m);

        _service.ApplyVolumeConfidenceAdjustment(scoring, VolumeConfirmation.Weak);

        Assert.Equal(0.55m, scoring.ConfidenceScore);
        Assert.Contains(scoring.ScoreReasons, r => r.Contains("faible"));
    }

    [Fact]
    public void ApplyVolumeConfidenceAdjustment_NeutralVolume_LeavesScoreUnchanged()
    {
        var scoring = BuildScoring(0.60m);

        _service.ApplyVolumeConfidenceAdjustment(scoring, VolumeConfirmation.Neutral);

        Assert.Equal(0.60m, scoring.ConfidenceScore);
        Assert.Empty(scoring.ScoreReasons);
    }

    [Fact]
    public void ApplyVolumeConfidenceAdjustment_StrongVolume_ClampsAtOne()
    {
        var scoring = BuildScoring(0.98m);

        _service.ApplyVolumeConfidenceAdjustment(scoring, VolumeConfirmation.Strong);

        Assert.Equal(1.00m, scoring.ConfidenceScore);
    }

    [Fact]
    public void ApplyVolumeConfidenceAdjustment_WeakVolume_ClampsAtZero()
    {
        var scoring = BuildScoring(0.02m);

        _service.ApplyVolumeConfidenceAdjustment(scoring, VolumeConfirmation.Weak);

        Assert.Equal(0.00m, scoring.ConfidenceScore);
    }
}
