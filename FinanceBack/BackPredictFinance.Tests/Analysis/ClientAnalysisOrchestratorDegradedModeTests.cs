using System.Net;
using BackPredictFinance.Common;
using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Patterns.Contracts;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BackPredictFinance.Tests.Analysis;

public sealed class ClientAnalysisOrchestratorDegradedModeTests
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
        Mock<IAnalysisExecutionService> executionServiceMock,
        IDegradedModeState degradedModeState)
    {
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
                Kind = RecommendationKind.Hold,
                Rationale = "Aucune figure exploitable."
            });

        var pedagogicalExplanationServiceMock = new Mock<IPedagogicalExplanationService>(MockBehavior.Loose);
        pedagogicalExplanationServiceMock
            .Setup(x => x.BuildAnalysisSummary(It.IsAny<AnalysisOutcome>(), It.IsAny<IReadOnlyList<PatternAssessmentContract>>(), It.IsAny<AnalysisRecommendation>(), It.IsAny<PortfolioContext>()))
            .Returns("Résumé pédagogique de test.");

        var snapshotPersistenceServiceMock = new Mock<IAnalysisSnapshotPersistenceService>(MockBehavior.Loose);

        snapshotPersistenceServiceMock
            .Setup(x => x.PersistFailedAnalysisAsync(
                It.IsAny<AnalysisRequest>(),
                It.IsAny<ResolvedAnalysisPattern>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<Exception>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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
                IsActive = true
            });

        return new ClientAnalysisOrchestrator(
            executionServiceMock.Object,
            riskEvaluationServiceMock.Object,
            recommendationPolicyServiceMock.Object,
            pedagogicalExplanationServiceMock.Object,
            snapshotPersistenceServiceMock.Object,
            degradedModeState,
            NullLogger<ClientAnalysisOrchestrator>.Instance);
    }

    [Fact]
    public async Task RunAnalysisAsync_ExecutionFailsWithProviderException_NoFallbackAvailable_ThrowsClearFrenchMessage()
    {
        var executionServiceMock = new Mock<IAnalysisExecutionService>(MockBehavior.Strict);
        executionServiceMock
            .Setup(x => x.ExecuteAsync(It.IsAny<AnalysisRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Yahoo indisponible"));

        var degradedModeState = new DegradedModeState();
        var orchestrator = BuildOrchestrator(executionServiceMock, degradedModeState);

        var ex = await Assert.ThrowsAsync<CustomException>(
            () => orchestrator.RunAnalysisAsync(BuildRequest()));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Equal("Données de marché momentanément indisponibles, réessayez plus tard.", ex.FrontMessage);
        Assert.DoesNotContain("HttpRequestException", ex.FrontMessage);
    }

    [Fact]
    public async Task RunAnalysisAsync_ExecutionSucceeds_DegradedModeFresh_DoesNotAlterResponseFreshness()
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

        var degradedModeState = new DegradedModeState();
        var orchestrator = BuildOrchestrator(executionServiceMock, degradedModeState);

        var response = await orchestrator.RunAnalysisAsync(BuildRequest());

        Assert.Equal(FreshnessStatusEnum.Fresh, response.DataFreshnessStatus);
        Assert.Null(response.DataFreshnessCheckedAtUtc);
    }
}
