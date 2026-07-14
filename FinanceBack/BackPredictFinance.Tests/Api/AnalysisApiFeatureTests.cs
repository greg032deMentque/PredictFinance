using BackPredictFinance.Common.enums;
using BackPredictFinance.Patterns;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Patterns;
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
        var expected = TestInfrastructure.CreateAnalysisResult(
            PatternIds.RectangleContinuation,
            "bullish_breakout_confirmed",
            RecommendationActionEnum.Buy);

        service.Setup(x => x.RunAnalysisAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = TestInfrastructure.CreateClientFinanceAnalysisController(service);

        var result = await controller.RunAnalysis(request, CancellationToken.None);

        var payload = TestInfrastructure.AssertOkObject<AnalysisResultViewModel>(result);
        Assert.Equal(PatternIds.RectangleContinuation, payload.Pattern);
        Assert.Equal(RecommendationActionEnum.Buy, payload.RecommendationAction);
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
            x => Assert.Equal(PatternIds.RectangleContinuation, x.PatternId),
            x => Assert.Equal(PatternIds.SymmetricalTriangleContinuation, x.PatternId));
    }

    [Fact]
    public void GetPatternCatalog_ReturnsFrontendProjectionForActivePatterns()
    {
        var controller = TestInfrastructure.CreateClientFinanceAnalysisController();

        var result = controller.GetPatternCatalog();

        var payload = TestInfrastructure.AssertOkObject<List<PatternCatalogViewModel>>(result);
        Assert.Collection(
            payload,
            pattern =>
            {
                Assert.Equal(PatternIds.RectangleContinuation, pattern.Id);
                Assert.Equal("Rectangle continuation", pattern.Label);
                Assert.Equal("continuation", pattern.Family);
                Assert.Equal("TrendFollowing", pattern.Direction);
                Assert.NotEmpty(pattern.Description);
            },
            pattern =>
            {
                Assert.Equal(PatternIds.SymmetricalTriangleContinuation, pattern.Id);
                Assert.Equal("Symmetrical triangle continuation", pattern.Label);
                Assert.Equal("continuation", pattern.Family);
                Assert.Equal("TrendFollowing", pattern.Direction);
                Assert.NotEmpty(pattern.Description);
            },
            pattern =>
            {
                Assert.Equal(PatternIds.BullFlagContinuation, pattern.Id);
                Assert.Equal("Bull flag continuation", pattern.Label);
                Assert.Equal("continuation", pattern.Family);
                Assert.Equal("Bullish", pattern.Direction);
                Assert.NotEmpty(pattern.Description);
            },
            pattern =>
            {
                Assert.Equal(PatternIds.BearFlagContinuation, pattern.Id);
                Assert.Equal("Bear flag continuation", pattern.Label);
                Assert.Equal("continuation", pattern.Family);
                Assert.Equal("Bearish", pattern.Direction);
                Assert.NotEmpty(pattern.Description);
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
    public void PatternIds_RequireActivePatternId_AcceptsActiveV1PatternIds(string patternId)
    {
        var actual = PatternIds.RequireActivePatternId(patternId);

        Assert.Equal(patternId, actual);
    }

    [Fact]
    public void PatternIds_RequireActivePatternId_RejectsDoubleTopLegacy()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => PatternIds.RequireActivePatternId("DOUBLE_TOP"));

        Assert.Contains("n'est pas pris en charge", exception.Message, StringComparison.OrdinalIgnoreCase);
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
