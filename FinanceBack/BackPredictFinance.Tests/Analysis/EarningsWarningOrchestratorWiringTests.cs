using BackPredictFinance.Common;
using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Patterns.Contracts;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BackPredictFinance.Tests.Analysis;

public sealed class EarningsWarningOrchestratorWiringTests
{
    private static AnalysisRequest BuildRequest()
    {
        return new AnalysisRequest
        {
            Instrument = new Instrument { Symbol = "AIR.PA" },
            ResolvedPatternIds = ["double_bottom"],
            HistoryStartDate = new DateOnly(2024, 1, 1),
            HistoryEndDate = new DateOnly(2024, 12, 31)
        };
    }

    private static ClientAnalysisOrchestrator BuildOrchestrator(
        DateTime? earningsDateUtc,
        int? reviewHorizonDays,
        RecommendationKind recommendationKind = RecommendationKind.Buy)
    {
        var executionArtifact = new AnalysisExecutionArtifact
        {
            Symbol = "AIR.PA",
            GeneratedAtUtc = DateTime.UtcNow,
            Patterns = [],
            ModelStatus = ModelStatusEnum.Go,
            Candles = []
        };

        var executionServiceMock = new Mock<IAnalysisExecutionService>(MockBehavior.Strict);
        executionServiceMock
            .Setup(x => x.ExecuteAsync(It.IsAny<AnalysisRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(executionArtifact);

        var riskEvaluationServiceMock = new Mock<IRiskEvaluationService>(MockBehavior.Loose);
        riskEvaluationServiceMock
            .Setup(x => x.BuildRiskContext(It.IsAny<AnalysisExecutionArtifact>()))
            .Returns(new AnalysisRiskContext());

        var recommendationPolicyServiceMock = new Mock<IRecommendationPolicyService>(MockBehavior.Loose);
        recommendationPolicyServiceMock
            .Setup(x => x.EvaluateAnalysis(It.IsAny<AnalysisRequest>(), It.IsAny<IReadOnlyList<PatternAssessmentContract>>(), It.IsAny<AnalysisOutcome>()))
            .Returns(new AnalysisRecommendation
            {
                RecommendationId = "reco-1",
                Kind = recommendationKind,
                Rationale = "Scenario de test.",
                ReviewHorizonDays = reviewHorizonDays
            });

        var pedagogicalExplanationServiceMock = new Mock<IPedagogicalExplanationService>(MockBehavior.Loose);
        pedagogicalExplanationServiceMock
            .Setup(x => x.BuildAnalysisSummary(It.IsAny<AnalysisOutcome>(), It.IsAny<IReadOnlyList<PatternAssessmentContract>>(), It.IsAny<AnalysisRecommendation>(), It.IsAny<PortfolioContext>()))
            .Returns("Résumé pédagogique de test.");

        var snapshotPersistenceServiceMock = new Mock<IAnalysisSnapshotPersistenceService>(MockBehavior.Loose);
        snapshotPersistenceServiceMock
            .Setup(x => x.PersistSuccessfulAnalysisAsync(
                It.IsAny<AnalysisRequest>(),
                It.IsAny<ResolvedAnalysisPattern>(),
                It.IsAny<AnalysisExecutionArtifact>(),
                It.IsAny<AnalysisRecommendation>(),
                It.IsAny<AnalysisOutcome>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistedAnalysisRecord
            {
                PublicId = "analysis-1",
                InstrumentId = "instrument-1",
                Symbol = "AIR.PA",
                ProviderSymbol = "AIR.PA",
                CompanyName = "Airbus",
                MarketCode = "XPAR",
                CurrencyCode = "EUR",
                AssetType = "EQUITY",
                CountryCode = "FR",
                IsActive = true,
                EarningsDateUtc = earningsDateUtc
            });

        return new ClientAnalysisOrchestrator(
            executionServiceMock.Object,
            riskEvaluationServiceMock.Object,
            recommendationPolicyServiceMock.Object,
            pedagogicalExplanationServiceMock.Object,
            snapshotPersistenceServiceMock.Object,
            new DegradedModeState(),
            NullLogger<ClientAnalysisOrchestrator>.Instance);
    }

    [Fact]
    public async Task RunAnalysisAsync_EarningsWithinReviewHorizon_SetsWarningTrue()
    {
        var earningsDateUtc = DateTime.UtcNow.AddDays(5);
        var orchestrator = BuildOrchestrator(earningsDateUtc, reviewHorizonDays: 20);

        var response = await orchestrator.RunAnalysisAsync(BuildRequest());

        Assert.NotNull(response.RiskContext);
        Assert.True(response.RiskContext!.EarningsWithinHorizonWarning);
        Assert.Equal(earningsDateUtc, response.RiskContext.NextEarningsDateUtc);
    }

    [Fact]
    public async Task RunAnalysisAsync_EarningsBeyondReviewHorizon_SetsWarningFalse()
    {
        var earningsDateUtc = DateTime.UtcNow.AddDays(45);
        var orchestrator = BuildOrchestrator(earningsDateUtc, reviewHorizonDays: 20);

        var response = await orchestrator.RunAnalysisAsync(BuildRequest());

        Assert.NotNull(response.RiskContext);
        Assert.False(response.RiskContext!.EarningsWithinHorizonWarning);
        Assert.Equal(earningsDateUtc, response.RiskContext.NextEarningsDateUtc);
    }

    [Fact]
    public async Task RunAnalysisAsync_WaitRecommendationWithZeroHorizon_NeverWarnsEvenWithImminentEarnings()
    {
        var earningsDateUtc = DateTime.UtcNow.AddHours(6);
        var orchestrator = BuildOrchestrator(earningsDateUtc, reviewHorizonDays: 0, recommendationKind: RecommendationKind.Wait);

        var response = await orchestrator.RunAnalysisAsync(BuildRequest());

        Assert.NotNull(response.RiskContext);
        Assert.False(response.RiskContext!.EarningsWithinHorizonWarning);
    }

    [Fact]
    public async Task RunAnalysisAsync_NoEarningsDateResolved_NeverWarns()
    {
        var orchestrator = BuildOrchestrator(earningsDateUtc: null, reviewHorizonDays: 20);

        var response = await orchestrator.RunAnalysisAsync(BuildRequest());

        Assert.NotNull(response.RiskContext);
        Assert.False(response.RiskContext!.EarningsWithinHorizonWarning);
        Assert.Null(response.RiskContext.NextEarningsDateUtc);
    }
}
