using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.ClientFinanceServices.Patterns;

namespace BackPredictFinance.Tests.Analysis;

public sealed class PatternScenarioBranchGeneratorTests
{
    private static PatternAssessmentContract BuildBullishAssessment(decimal? invalidationLevel = 136m)
    {
        return new PatternAssessmentContract
        {
            PatternId = "RECTANGLE_CONTINUATION",
            DisplayName = "Rectangle continuation",
            Detection = new PatternDetection
            {
                IsCompatible = true,
                Status = PatternStatus.Monitoring,
                CurrentPhaseCode = "bullish_monitoring",
                CurrentPhaseLabel = "Sous surveillance haussier",
                CurrentPrice = 145m,
                StructuralPoints =
                [
                    new PatternStructuralPoint { PointType = "breakout", Price = 150m },
                    new PatternStructuralPoint { PointType = "target", Price = 165m }
                ]
            },
            Validation = new PatternValidation { State = "NOT_VALIDATED" },
            Invalidation = new PatternInvalidation { State = "NOT_INVALIDATED", InvalidationLevel = invalidationLevel },
            Scoring = new PatternScoring { ConfidenceScore = 0.75m, ConfidenceLabel = "HIGH", IsCredible = true }
        };
    }

    private static PatternAssessmentContract BuildBearishAssessment(decimal? invalidationLevel = 155m)
    {
        return new PatternAssessmentContract
        {
            PatternId = "BEAR_FLAG",
            DisplayName = "Bear flag",
            Detection = new PatternDetection
            {
                IsCompatible = true,
                Status = PatternStatus.Monitoring,
                CurrentPhaseCode = "bearish_monitoring",
                CurrentPhaseLabel = "Sous surveillance baissier",
                CurrentPrice = 145m,
                StructuralPoints =
                [
                    new PatternStructuralPoint { PointType = "neckline", Price = 140m }
                ]
            },
            Validation = new PatternValidation { State = "NOT_VALIDATED" },
            Invalidation = new PatternInvalidation { State = "NOT_INVALIDATED", InvalidationLevel = invalidationLevel },
            Scoring = new PatternScoring { ConfidenceScore = 0.65m, ConfidenceLabel = "MEDIUM", IsCredible = true }
        };
    }

    private static IPatternScenarioBranchGenerator BuildGenerator() => new PatternScenarioBranchGenerator();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Generate_BullishPattern_ReturnsTwoBranchesWithCorrectStates(bool holdsInstrument)
    {
        var assessment = BuildBullishAssessment();
        var generator = BuildGenerator();

        var branches = generator.Generate(assessment, holdsInstrument);

        Assert.Equal(2, branches.Count);

        var confirmBranch = branches[0];
        Assert.Equal("Confirmed", confirmBranch.ResultingState);
        Assert.Equal("Up", confirmBranch.Direction);
        Assert.Equal(150m, confirmBranch.TriggerLevel);
        Assert.False(string.IsNullOrWhiteSpace(confirmBranch.Posture));
        Assert.False(string.IsNullOrWhiteSpace(confirmBranch.Rationale));

        var invalidBranch = branches[1];
        Assert.Equal("Invalidated", invalidBranch.ResultingState);
        Assert.Equal("Down", invalidBranch.Direction);
        Assert.Equal(136m, invalidBranch.TriggerLevel);
        Assert.False(string.IsNullOrWhiteSpace(invalidBranch.Posture));
        Assert.False(string.IsNullOrWhiteSpace(invalidBranch.Rationale));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Generate_BearishPattern_ReturnsTwoBranchesWithCorrectDirections(bool holdsInstrument)
    {
        var assessment = BuildBearishAssessment();
        var generator = BuildGenerator();

        var branches = generator.Generate(assessment, holdsInstrument);

        Assert.Equal(2, branches.Count);

        var confirmBranch = branches[0];
        Assert.Equal("Confirmed", confirmBranch.ResultingState);
        Assert.Equal("Down", confirmBranch.Direction);

        var invalidBranch = branches[1];
        Assert.Equal("Invalidated", invalidBranch.ResultingState);
        Assert.Equal("Up", invalidBranch.Direction);
        Assert.Equal(155m, invalidBranch.TriggerLevel);
    }

    [Fact]
    public void Generate_BearishPatternWithDirectionlessPhaseCode_StillReturnsBearishDirections()
    {
        // Certains codes de phase ne portent pas la direction (ex. flag_structure_not_confirmed) :
        // la direction doit se lire sur l'identité de la figure.
        var assessment = BuildBearishAssessment();
        assessment.Detection.CurrentPhaseCode = "flag_structure_not_confirmed";
        var generator = BuildGenerator();

        var branches = generator.Generate(assessment, holdsInstrument: false);

        Assert.Equal("Down", branches[0].Direction);
        Assert.Equal("Up", branches[1].Direction);
    }

    [Fact]
    public void Generate_HoldsInstrument_ConfirmationPostureContainsConserver()
    {
        var assessment = BuildBullishAssessment();
        var generator = BuildGenerator();

        var branches = generator.Generate(assessment, holdsInstrument: true);

        Assert.Contains("Conserver", branches[0].Posture, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_DoesNotHoldInstrument_ConfirmationPostureContainsEntrer()
    {
        var assessment = BuildBullishAssessment();
        var generator = BuildGenerator();

        var branches = generator.Generate(assessment, holdsInstrument: false);

        Assert.Contains("Entrer", branches[0].Posture, StringComparison.OrdinalIgnoreCase);
    }
}
