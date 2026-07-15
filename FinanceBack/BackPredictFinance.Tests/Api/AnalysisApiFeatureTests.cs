using BackPredictFinance.Common.enums;
using BackPredictFinance.Patterns;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Patterns;
using Microsoft.EntityFrameworkCore;
using Moq;

using BackPredictFinance.Tests.Infrastructure;

namespace BackPredictFinance.Tests.Api;

public sealed class AnalysisApiFeatureTests
{
    [Fact]
    public async Task RunAnalysis_ReturnsMultiPatternAnalysisResult()
    {
        var service = TestInfrastructure.CreateClientFinanceServiceMock();
        var request = new AnalysisRunRequestViewModel
        {
            Symbol = "AIR.PA",
            RequestedPatternIds =
            [
                PatternIds.RectangleContinuation,
                PatternIds.BullFlagContinuation
            ]
        };
        var expected = TestInfrastructure.CreateAnalysisDossier(
            PatternIds.RectangleContinuation,
            "bullish_breakout_confirmed",
            RecommendationActionEnum.Buy);

        service.Setup(x => x.RunAnalysisAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = TestInfrastructure.CreateClientFinanceAnalysisController(service);

        var result = await controller.RunAnalysis(request, CancellationToken.None);

        var payload = TestInfrastructure.AssertOkObject<AnalysisDossierViewModel>(result.Result!);
        Assert.Equal(PatternIds.RectangleContinuation, payload.MainPattern?.PatternId);
        Assert.Equal("Buy", payload.MainPattern?.RecommendationAction);
        service.Verify(x => x.RunAnalysisAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRecentAnalyses_ReturnsRecentAnalysisHistory()
    {
        var dashboardHistoryService = TestInfrastructure.CreateDashboardHistoryServiceMock();
        var expected = new List<AnalysisResultViewModel>
        {
            TestInfrastructure.CreateAnalysisResult(PatternIds.RectangleContinuation, "bullish_breakout_confirmed", RecommendationActionEnum.Buy),
            TestInfrastructure.CreateAnalysisResult(PatternIds.BearFlagContinuation, "bearish_breakout_confirmed", RecommendationActionEnum.Sell)
        };
        dashboardHistoryService.Setup(x => x.GetRecentAnalysesAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = TestInfrastructure.CreateClientFinanceAnalysisController(dashboardHistoryServiceMock: dashboardHistoryService);

        var result = await controller.GetRecentAnalyses(10, CancellationToken.None);

        var payload = TestInfrastructure.AssertOkObject<List<AnalysisResultViewModel>>(result);
        Assert.Equal(2, payload.Count);
        Assert.Contains(payload, x => x.Pattern == PatternIds.RectangleContinuation);
        Assert.Contains(payload, x => x.Pattern == PatternIds.BearFlagContinuation);
        dashboardHistoryService.Verify(x => x.GetRecentAnalysesAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task GetAnalysisDetail_ReturnsPersistedAnalysisDetail()
    {
        var dashboardHistoryService = TestInfrastructure.CreateDashboardHistoryServiceMock();
        var expected = TestInfrastructure.CreateAnalysisDetail();
        dashboardHistoryService.Setup(x => x.GetAnalysisDetailAsync("analysis-1", It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = TestInfrastructure.CreateClientFinanceAnalysisController(dashboardHistoryServiceMock: dashboardHistoryService);

        var result = await controller.GetAnalysisDetail("analysis-1", CancellationToken.None);

        var payload = TestInfrastructure.AssertOkObject<AnalysisDetailViewModel>(result);
        Assert.Equal(expected.AnalysisId, payload.AnalysisId);
        Assert.Equal(expected.Instrument.Symbol, payload.Instrument.Symbol);
        Assert.Equal(expected.ConfidenceBreakdown.Level, payload.ConfidenceBreakdown.Level);
        Assert.NotEmpty(payload.ConfidenceBreakdown.Criteria);
        Assert.NotEmpty(payload.ActionPlan.Steps);
        dashboardHistoryService.Verify(x => x.GetAnalysisDetailAsync("analysis-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void PatternCatalog_ExposesOnlyActiveV1MultiPatternTargets()
    {
        var patterns = PatternCatalog.GetTargetPatterns();

        Assert.Collection(
            patterns.OrderBy(x => x.PatternId, StringComparer.Ordinal),
            x => Assert.Equal(PatternIds.BearFlagContinuation, x.PatternId),
            x => Assert.Equal(PatternIds.BullFlagContinuation, x.PatternId),
            x => Assert.Equal(PatternIds.DoubleBottom, x.PatternId),
            x => Assert.Equal(PatternIds.DoubleTop, x.PatternId),
            x => Assert.Equal(PatternIds.HeadAndShoulders, x.PatternId),
            x => Assert.Equal(PatternIds.InverseHeadAndShoulders, x.PatternId),
            x => Assert.Equal(PatternIds.RectangleContinuation, x.PatternId),
            x => Assert.Equal(PatternIds.SymmetricalTriangleContinuation, x.PatternId));
    }

    [Fact]
    public async Task GetPatternCatalog_SeedStaysInSyncWithEnginePatternSet()
    {
        // Régression silencieuse couverte : si un pattern est ajouté/retiré du moteur
        // (PatternCatalog) sans mettre à jour le seed PatternDefinitions, le catalogue
        // exposé au front devient incohérent. Ce test verrouille cette synchronisation.
        using var dbContext = TestInfrastructure.CreateInMemoryFinanceDbContext();

        var payload = await dbContext.PatternDefinitions
            .AsNoTracking()
            .Select(pattern => new PatternCatalogViewModel
            {
                Id = pattern.PatternId,
                Label = pattern.DisplayName,
                Family = pattern.Family,
                Description = pattern.Description,
                Direction = pattern.Direction,
                FamilyLabel = pattern.FamilyLabel,
                DirectionLabel = pattern.DirectionLabel,
                AnalysisNarrative = pattern.AnalysisNarrative,
                Reliability = pattern.Reliability,
                ReliabilityLabel = pattern.ReliabilityLabel
            })
            .ToListAsync(CancellationToken.None);

        var expectedIds = PatternCatalog.GetTargetPatterns()
            .Select(x => x.PatternId)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
        var actualIds = payload
            .Select(x => x.Id)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expectedIds, actualIds);
        Assert.All(payload, pattern =>
        {
            Assert.NotEmpty(pattern.Label);
            Assert.NotEmpty(pattern.Family);
            Assert.NotEmpty(pattern.Description);
            Assert.NotEmpty(pattern.Direction);
            Assert.NotEmpty(pattern.FamilyLabel);
            Assert.NotEmpty(pattern.DirectionLabel);
            Assert.NotEmpty(pattern.AnalysisNarrative);
            Assert.NotEmpty(pattern.ReliabilityLabel);
            Assert.True(pattern.Reliability > 0m, $"Fiabilité manquante pour {pattern.Id}");
        });
    }

    [Fact]
    public void AnalysisPatternRegistry_DefaultResolution_ReturnsOnlyActiveTargetDefinitions()
    {
        var registry = new AnalysisPatternRegistry(
        [
            new FakePatternDefinition(PatternIds.RectangleContinuation),
            new FakePatternDefinition(PatternIds.SymmetricalTriangleContinuation),
            new FakePatternDefinition(PatternIds.BullFlagContinuation),
            new FakePatternDefinition(PatternIds.BearFlagContinuation)
        ]);

        var resolved = registry.ResolveDefinitions([]);

        Assert.Equal(4, resolved.Count);
    }

    [Theory]
    [InlineData(PatternIds.RectangleContinuation)]
    [InlineData(PatternIds.SymmetricalTriangleContinuation)]
    [InlineData(PatternIds.BullFlagContinuation)]
    [InlineData(PatternIds.BearFlagContinuation)]
    [InlineData(PatternIds.DoubleBottom)]
    [InlineData(PatternIds.DoubleTop)]
    [InlineData(PatternIds.InverseHeadAndShoulders)]
    [InlineData(PatternIds.HeadAndShoulders)]
    public void PatternIds_RequireActivePatternId_AcceptsActiveV1PatternIds(string patternId)
    {
        var actual = PatternIds.RequireActivePatternId(patternId);

        Assert.Equal(patternId, actual);
    }

    [Fact]
    public void AnalysisPatternRegistry_ResolveDefinition_ThrowsForUnsupportedPattern()
    {
        var registry = new AnalysisPatternRegistry(
        [
            new FakePatternDefinition(PatternIds.RectangleContinuation),
            new FakePatternDefinition(PatternIds.SymmetricalTriangleContinuation),
            new FakePatternDefinition(PatternIds.BullFlagContinuation),
            new FakePatternDefinition(PatternIds.BearFlagContinuation)
        ]);

        var exception = Assert.Throws<InvalidOperationException>(() => registry.ResolveDefinition("UNSUPPORTED_PATTERN"));

        Assert.Contains("ne prend pas en charge", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
