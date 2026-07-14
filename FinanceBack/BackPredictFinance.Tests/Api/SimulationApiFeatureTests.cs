using BackPredictFinance.Common.enums;
using BackPredictFinance.Patterns;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Simulation;
using Moq;

using BackPredictFinance.Tests.Infrastructure;

namespace BackPredictFinance.Tests.Api;

public sealed class SimulationApiFeatureTests
{
    [Theory]
    [InlineData(PatternIds.RectangleContinuation, RecommendationActionEnum.Buy)]
    [InlineData(PatternIds.SymmetricalTriangleContinuation, RecommendationActionEnum.Hold)]
    [InlineData(PatternIds.BullFlagContinuation, RecommendationActionEnum.Buy)]
    [InlineData(PatternIds.BearFlagContinuation, RecommendationActionEnum.Sell)]
    public async Task RunSimulation_ReturnsExpectedActivePatternPayload(string patternId, RecommendationActionEnum expectedAction)
    {
        var service = TestInfrastructure.CreateClientFinanceServiceMock();
        var request = new SimulationRequestViewModel
        {
            Symbol = "AIR.PA",
            Pattern = patternId,
            InvestmentAmount = 1000m,
            HorizonDays = 20
        };
        var expected = TestInfrastructure.CreateSimulationResult(patternId, expectedAction);
        service.Setup(x => x.RunSimulationAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = TestInfrastructure.CreateClientFinanceAnalysisController(service);

        var result = await controller.RunSimulation(request, CancellationToken.None);

        var payload = TestInfrastructure.AssertOkObject<SimulationResultViewModel>(result);
        Assert.Equal(patternId, payload.Pattern);
        Assert.Equal(expectedAction, payload.RecommendationAction);
        service.Verify(x => x.RunSimulationAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
