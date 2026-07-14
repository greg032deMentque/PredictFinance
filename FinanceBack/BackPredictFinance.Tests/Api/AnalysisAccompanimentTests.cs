using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Patterns;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;

namespace BackPredictFinance.Tests.Api;

public sealed class AnalysisAccompanimentTests
{
    private static ConfidenceBreakdownAssembler CreateAssembler()
        => new(new AnalysisAccompanimentWordingProvider());

    private static ActionPlanGenerationService CreateActionPlanService()
        => new(new AnalysisAccompanimentWordingProvider());

    private static PatternAssessmentContract CreatePrimaryPattern(
        string confidenceLabel = "HIGH",
        bool isCompatible = true,
        string validationState = "VALIDATED",
        string invalidationState = "ACTIVE",
        decimal? invalidationLevel = 136m,
        decimal? takeProfit = 160m)
    {
        return new PatternAssessmentContract
        {
            PatternId = PatternIds.RectangleContinuation,
            DisplayName = "Rectangle continuation",
            Detection = new PatternDetection { IsCompatible = isCompatible, Status = PatternStatus.Confirmed },
            Validation = new PatternValidation { State = validationState },
            Invalidation = new PatternInvalidation { State = invalidationState, InvalidationLevel = invalidationLevel },
            Scoring = new PatternScoring { ConfidenceLabel = confidenceLabel, ConfidenceScore = 0.82m, IsCredible = true },
            RiskHints = new PatternRiskHints { SuggestedTakeProfit = takeProfit }
        };
    }

    [Fact]
    public void ConfidenceBreakdown_KeepsPersistedLabelVerbatim()
    {
        // RM-27 : on explique le niveau, on ne le recalcule pas (même un label hors taxonomie passe tel quel).
        var breakdown = CreateAssembler().Build(CreatePrimaryPattern(confidenceLabel: "VERY_LOW"));

        Assert.Equal("VERY_LOW", breakdown.Label);
    }

    [Fact]
    public void ConfidenceBreakdown_DerivesCriteriaWithGovernedLabelsAndSources()
    {
        var breakdown = CreateAssembler().Build(CreatePrimaryPattern(isCompatible: true, validationState: "VALIDATED", invalidationState: "ACTIVE"));

        Assert.Collection(
            breakdown.Criteria,
            criterion =>
            {
                Assert.Equal(ConfidenceCriterionCodes.StructureCompatible, criterion.Code);
                Assert.Equal(CriterionSource.Detection, criterion.Source);
                Assert.Equal(CriterionState.Met, criterion.State);
            },
            criterion =>
            {
                Assert.Equal(ConfidenceCriterionCodes.PatternValidated, criterion.Code);
                Assert.Equal(CriterionSource.Validation, criterion.Source);
                Assert.Equal(CriterionState.Met, criterion.State);
            },
            criterion =>
            {
                Assert.Equal(ConfidenceCriterionCodes.InvalidationNotTriggered, criterion.Code);
                Assert.Equal(CriterionSource.Invalidation, criterion.Source);
                Assert.Equal(CriterionState.Met, criterion.State);
            });
        Assert.All(breakdown.Criteria, criterion => Assert.False(string.IsNullOrWhiteSpace(criterion.Label)));
    }

    [Theory]
    [InlineData("VALIDATED", CriterionState.Met)]
    [InlineData("NOT_VALIDATED", CriterionState.Absent)]
    [InlineData("", CriterionState.Partial)]
    public void ConfidenceBreakdown_MapsValidationStateDeterministically(string state, CriterionState expected)
    {
        var breakdown = CreateAssembler().Build(CreatePrimaryPattern(validationState: state));

        var validation = breakdown.Criteria.Single(criterion => criterion.Source == CriterionSource.Validation);
        Assert.Equal(expected, validation.State);
    }

    [Theory]
    [InlineData("ACTIVE", CriterionState.Met)]
    [InlineData("INVALIDATED", CriterionState.Absent)]
    public void ConfidenceBreakdown_MapsInvalidationStateDeterministically(string state, CriterionState expected)
    {
        var breakdown = CreateAssembler().Build(CreatePrimaryPattern(invalidationState: state));

        var invalidation = breakdown.Criteria.Single(criterion => criterion.Source == CriterionSource.Invalidation);
        Assert.Equal(expected, invalidation.State);
    }

    [Fact]
    public void ConfidenceBreakdown_NullPrimaryPattern_ReturnsEmpty()
    {
        var breakdown = CreateAssembler().Build(null);

        Assert.Empty(breakdown.Criteria);
        Assert.Equal(string.Empty, breakdown.Label);
    }

    [Fact]
    public void ActionPlan_ExecutableNotHeld_ReformulatesRiskAndHorizonFromSourceFields()
    {
        var recommendation = new AnalysisRecommendation
        {
            Kind = RecommendationKind.Buy,
            HoldingContext = HoldingStatusEnum.NotHeld,
            ReviewHorizonDays = 20
        };

        var plan = CreateActionPlanService().Generate(
            AnalysisOutcome.CrediblePatternFound,
            CreatePrimaryPattern(),
            recommendation,
            holdsInstrument: false,
            currencyCode: "EUR");

        Assert.NotEmpty(plan.Steps);
        Assert.True(plan.Steps.Count <= 3);
        Assert.Contains(plan.Steps, step => step.Kind == ActionStepKind.NoteLevel && !string.IsNullOrWhiteSpace(step.Value));
        Assert.Contains(plan.Steps, step => step.Kind == ActionStepKind.SetAlert && step.AlertTrigger == AlertTrigger.LevelCrossed);
        Assert.Contains(plan.Steps, step => step.Kind == ActionStepKind.ReviewAt && step.Value == "20 j");
        Assert.DoesNotContain(plan.Steps, step => step.Kind == ActionStepKind.HoldingReminder);
        // RM-26 : toute valeur affichée provient d'un champ source tracé.
        Assert.All(
            plan.Steps.Where(step => step.Value is not null),
            step => Assert.False(string.IsNullOrWhiteSpace(step.SourceField)));
        Assert.False(string.IsNullOrWhiteSpace(plan.PolicyVersion));
    }

    [Fact]
    public void ActionPlan_ExecutableHeld_IncludesHoldingReminderWithinCap()
    {
        var recommendation = new AnalysisRecommendation
        {
            Kind = RecommendationKind.Hold,
            HoldingContext = HoldingStatusEnum.Held
        };

        var plan = CreateActionPlanService().Generate(
            AnalysisOutcome.CrediblePatternFound,
            CreatePrimaryPattern(),
            recommendation,
            holdsInstrument: true,
            currencyCode: "EUR");

        Assert.Contains(plan.Steps, step => step.Kind == ActionStepKind.HoldingReminder);
        Assert.True(plan.Steps.Count <= 3);
    }

    [Theory]
    [InlineData(AnalysisOutcome.NoCrediblePattern)]
    [InlineData(AnalysisOutcome.InsufficientData)]
    [InlineData(AnalysisOutcome.UnsupportedInstrument)]
    [InlineData(AnalysisOutcome.UnsupportedContext)]
    public void ActionPlan_NonExecutable_OnlyAllowsWaitOrHoldingReminder(AnalysisOutcome outcome)
    {
        var plan = CreateActionPlanService().Generate(
            outcome,
            CreatePrimaryPattern(),
            recommendation: null,
            holdsInstrument: true,
            currencyCode: "EUR");

        Assert.All(plan.Steps, step => Assert.True(step.Kind is ActionStepKind.WaitForData or ActionStepKind.HoldingReminder));
        Assert.Contains(plan.Steps, step => step.Kind == ActionStepKind.WaitForData);
    }

    [Fact]
    public void ActionPlan_NonExecutableNotHeld_HasNoHoldingReminder()
    {
        var plan = CreateActionPlanService().Generate(
            AnalysisOutcome.InsufficientData,
            primaryPattern: null,
            recommendation: null,
            holdsInstrument: false,
            currencyCode: "EUR");

        Assert.Contains(plan.Steps, step => step.Kind == ActionStepKind.WaitForData);
        Assert.DoesNotContain(plan.Steps, step => step.Kind == ActionStepKind.HoldingReminder);
    }
}
