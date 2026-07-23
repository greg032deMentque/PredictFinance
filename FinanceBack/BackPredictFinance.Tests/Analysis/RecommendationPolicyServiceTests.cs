using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.Trading;
using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using Moq;

namespace BackPredictFinance.Tests.Analysis;

public sealed class RecommendationPolicyServiceTests
{
    private static AnalysisRequest BuildRequest(bool holdsInstrument)
    {
        return new AnalysisRequest
        {
            InstrumentId = "instrument-1",
            UserId = "user-1",
            PortfolioContext = new PortfolioContext { HoldsInstrument = holdsInstrument }
        };
    }

    private static PatternAssessmentContract BuildPattern(
        bool isCompatible,
        PatternStatus status,
        string patternId = "PATTERN_1",
        bool isPrimary = true,
        decimal confidenceScore = 0.60m)
    {
        return new PatternAssessmentContract
        {
            PatternId = patternId,
            DisplayName = "Pattern de test",
            Detection = new PatternDetection
            {
                IsCompatible = isCompatible,
                Status = status,
                CurrentPhaseCode = "test_phase",
                CurrentPhaseLabel = "en test"
            },
            Scoring = new PatternScoring { ConfidenceScore = confidenceScore, ConfidenceLabel = "MEDIUM" },
            Trace = new PatternTrace { IsPrimaryDisplayCandidate = isPrimary }
        };
    }

    private static Mock<ITradingRecommendationService> BuildTradingServiceMock(RecommendationActionEnum action, int horizonDays = 20)
    {
        var mock = new Mock<ITradingRecommendationService>(MockBehavior.Strict);
        mock.Setup(x => x.EvaluateAnalysis(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<decimal?>(), It.IsAny<decimal?>()))
            .Returns(new TradingRecommendationResult
            {
                Action = action,
                IsActionable = action is RecommendationActionEnum.Buy or RecommendationActionEnum.Sell,
                HorizonDays = horizonDays
            });
        return mock;
    }

    [Fact]
    public void EvaluateAnalysis_EmptyCompatiblePatterns_ReturnsWaitWithoutCallingTradingService()
    {
        var tradingServiceMock = new Mock<ITradingRecommendationService>(MockBehavior.Strict);
        var service = new RecommendationPolicyService(tradingServiceMock.Object);

        var recommendation = service.EvaluateAnalysis(BuildRequest(holdsInstrument: false), [], AnalysisOutcome.CrediblePatternFound);

        Assert.Equal(RecommendationKind.Wait, recommendation.Kind);
        Assert.Empty(recommendation.BasedOnPatternIds);
    }

    [Fact]
    public void EvaluateAnalysis_OutcomeNoCrediblePattern_WithNonEmptyPatterns_ReturnsWaitWithoutCallingTradingService()
    {
        var tradingServiceMock = new Mock<ITradingRecommendationService>(MockBehavior.Strict);
        var service = new RecommendationPolicyService(tradingServiceMock.Object);
        var patterns = new List<PatternAssessmentContract> { BuildPattern(isCompatible: true, PatternStatus.Confirmed) };

        var recommendation = service.EvaluateAnalysis(BuildRequest(holdsInstrument: false), patterns, AnalysisOutcome.NoCrediblePattern);

        Assert.Equal(RecommendationKind.Wait, recommendation.Kind);
    }

    [Theory]
    [InlineData(false, true, PatternStatus.Confirmed, RecommendationActionEnum.Buy, RecommendationKind.Wait)]
    [InlineData(false, false, PatternStatus.Confirmed, RecommendationActionEnum.Buy, RecommendationKind.Wait)]
    [InlineData(true, true, PatternStatus.Confirmed, RecommendationActionEnum.Buy, RecommendationKind.Reinforce)]
    [InlineData(true, true, PatternStatus.Confirmed, RecommendationActionEnum.Sell, RecommendationKind.Sell)]
    [InlineData(true, true, PatternStatus.Invalidated, RecommendationActionEnum.Hold, RecommendationKind.Wait)]
    [InlineData(true, true, PatternStatus.Completed, RecommendationActionEnum.Hold, RecommendationKind.Wait)]
    [InlineData(true, true, PatternStatus.Forming, RecommendationActionEnum.Hold, RecommendationKind.Hold)]
    [InlineData(true, true, PatternStatus.Monitoring, RecommendationActionEnum.Hold, RecommendationKind.Hold)]
    [InlineData(true, true, PatternStatus.Confirmed, RecommendationActionEnum.Hold, RecommendationKind.Hold)]
    [InlineData(true, false, PatternStatus.Confirmed, RecommendationActionEnum.Buy, RecommendationKind.Buy)]
    [InlineData(true, false, PatternStatus.Confirmed, RecommendationActionEnum.Sell, RecommendationKind.Monitor)]
    [InlineData(true, false, PatternStatus.Forming, RecommendationActionEnum.Hold, RecommendationKind.Monitor)]
    [InlineData(true, false, PatternStatus.Monitoring, RecommendationActionEnum.Hold, RecommendationKind.Monitor)]
    [InlineData(true, false, PatternStatus.Confirmed, RecommendationActionEnum.Hold, RecommendationKind.Monitor)]
    [InlineData(true, false, PatternStatus.Invalidated, RecommendationActionEnum.Hold, RecommendationKind.Wait)]
    [InlineData(true, false, PatternStatus.Completed, RecommendationActionEnum.Hold, RecommendationKind.Wait)]
    [InlineData(true, false, PatternStatus.Monitoring, RecommendationActionEnum.NonActionable, RecommendationKind.Monitor)]
    public void EvaluateAnalysis_ResolvesExpectedRecommendationKind(
        bool isCompatible,
        bool holdsInstrument,
        PatternStatus status,
        RecommendationActionEnum tradingAction,
        RecommendationKind expectedKind)
    {
        var tradingServiceMock = BuildTradingServiceMock(tradingAction);
        var service = new RecommendationPolicyService(tradingServiceMock.Object);
        var patterns = new List<PatternAssessmentContract> { BuildPattern(isCompatible, status) };

        var recommendation = service.EvaluateAnalysis(BuildRequest(holdsInstrument), patterns, AnalysisOutcome.CrediblePatternFound);

        Assert.Equal(expectedKind, recommendation.Kind);
    }

    [Theory]
    [InlineData(RecommendationActionEnum.Buy, RecommendationKind.Buy, "BUY")]
    [InlineData(RecommendationActionEnum.Sell, RecommendationKind.Sell, "SELL")]
    public void EvaluateAnalysis_Rationale_ContainsExpectedBusinessKeyword(
        RecommendationActionEnum tradingAction,
        RecommendationKind expectedKind,
        string expectedKeyword)
    {
        var holdsInstrument = expectedKind == RecommendationKind.Sell;
        var tradingServiceMock = BuildTradingServiceMock(tradingAction);
        var service = new RecommendationPolicyService(tradingServiceMock.Object);
        var patterns = new List<PatternAssessmentContract> { BuildPattern(isCompatible: true, PatternStatus.Confirmed) };

        var recommendation = service.EvaluateAnalysis(BuildRequest(holdsInstrument), patterns, AnalysisOutcome.CrediblePatternFound);

        Assert.Equal(expectedKind, recommendation.Kind);
        Assert.Contains(expectedKeyword, recommendation.Rationale);
    }

    [Fact]
    public void EvaluateAnalysis_MultipleCompatiblePatterns_SetsWarningText()
    {
        var tradingServiceMock = BuildTradingServiceMock(RecommendationActionEnum.Hold);
        var service = new RecommendationPolicyService(tradingServiceMock.Object);
        var patterns = new List<PatternAssessmentContract>
        {
            BuildPattern(isCompatible: true, PatternStatus.Confirmed, patternId: "PATTERN_1", isPrimary: true, confidenceScore: 0.80m),
            BuildPattern(isCompatible: true, PatternStatus.Monitoring, patternId: "PATTERN_2", isPrimary: false, confidenceScore: 0.60m)
        };

        var recommendation = service.EvaluateAnalysis(BuildRequest(holdsInstrument: false), patterns, AnalysisOutcome.MultipleCompatiblePatterns);

        Assert.NotNull(recommendation.WarningText);
    }

    [Fact]
    public void EvaluateAnalysis_SingleCompatiblePattern_LeavesWarningTextNull()
    {
        var tradingServiceMock = BuildTradingServiceMock(RecommendationActionEnum.Hold);
        var service = new RecommendationPolicyService(tradingServiceMock.Object);
        var patterns = new List<PatternAssessmentContract> { BuildPattern(isCompatible: true, PatternStatus.Confirmed) };

        var recommendation = service.EvaluateAnalysis(BuildRequest(holdsInstrument: false), patterns, AnalysisOutcome.CrediblePatternFound);

        Assert.Null(recommendation.WarningText);
    }

    [Fact]
    public void EvaluateAnalysis_ZeroHorizonDaysFromTradingService_LeavesReviewHorizonDaysNull()
    {
        var tradingServiceMock = BuildTradingServiceMock(RecommendationActionEnum.Hold, horizonDays: 0);
        var service = new RecommendationPolicyService(tradingServiceMock.Object);
        var patterns = new List<PatternAssessmentContract> { BuildPattern(isCompatible: true, PatternStatus.Confirmed) };

        var recommendation = service.EvaluateAnalysis(BuildRequest(holdsInstrument: false), patterns, AnalysisOutcome.CrediblePatternFound);

        Assert.Null(recommendation.ReviewHorizonDays);
    }

    [Fact]
    public void EvaluateAnalysis_PositiveHorizonDaysFromTradingService_SetsReviewHorizonDays()
    {
        var tradingServiceMock = BuildTradingServiceMock(RecommendationActionEnum.Hold, horizonDays: 20);
        var service = new RecommendationPolicyService(tradingServiceMock.Object);
        var patterns = new List<PatternAssessmentContract> { BuildPattern(isCompatible: true, PatternStatus.Confirmed) };

        var recommendation = service.EvaluateAnalysis(BuildRequest(holdsInstrument: false), patterns, AnalysisOutcome.CrediblePatternFound);

        Assert.Equal(20, recommendation.ReviewHorizonDays);
    }

    [Fact]
    public void EvaluateAnalysis_BasedOnPatternIds_FiltersWhitespaceAndDeduplicatesIgnoringCase()
    {
        var tradingServiceMock = BuildTradingServiceMock(RecommendationActionEnum.Hold);
        var service = new RecommendationPolicyService(tradingServiceMock.Object);
        var patterns = new List<PatternAssessmentContract>
        {
            BuildPattern(isCompatible: true, PatternStatus.Confirmed, patternId: "ABC", isPrimary: true),
            BuildPattern(isCompatible: true, PatternStatus.Monitoring, patternId: "abc", isPrimary: false),
            BuildPattern(isCompatible: true, PatternStatus.Monitoring, patternId: " ", isPrimary: false),
            BuildPattern(isCompatible: true, PatternStatus.Monitoring, patternId: "", isPrimary: false)
        };

        var recommendation = service.EvaluateAnalysis(BuildRequest(holdsInstrument: false), patterns, AnalysisOutcome.MultipleCompatiblePatterns);

        Assert.Equal(["ABC"], recommendation.BasedOnPatternIds);
    }

    [Theory]
    [InlineData(true, HoldingStatusEnum.Held)]
    [InlineData(false, HoldingStatusEnum.NotHeld)]
    public void EvaluateAnalysis_HoldingContext_ReflectsPortfolioHoldsInstrument(bool holdsInstrument, HoldingStatusEnum expectedHoldingContext)
    {
        var tradingServiceMock = BuildTradingServiceMock(RecommendationActionEnum.Hold);
        var service = new RecommendationPolicyService(tradingServiceMock.Object);
        var patterns = new List<PatternAssessmentContract> { BuildPattern(isCompatible: true, PatternStatus.Confirmed) };

        var recommendation = service.EvaluateAnalysis(BuildRequest(holdsInstrument), patterns, AnalysisOutcome.CrediblePatternFound);

        Assert.Equal(expectedHoldingContext, recommendation.HoldingContext);
    }
}
