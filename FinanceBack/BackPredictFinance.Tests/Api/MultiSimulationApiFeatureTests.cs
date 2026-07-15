using BackPredictFinance.Common.enums;
using BackPredictFinance.Patterns;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Simulation;
using Moq;

using BackPredictFinance.Tests.Infrastructure;

namespace BackPredictFinance.Tests.Api;

public sealed class MultiSimulationApiFeatureTests
{
    [Fact]
    public async Task RunMultiSimulation_MixedPatterns_ReturnsOneResultPerPattern()
    {
        var service = TestInfrastructure.CreateClientFinanceServiceMock();
        var request = new SimulationRequestViewModel
        {
            Symbol = "AIR.PA",
            Patterns = [PatternIds.RectangleContinuation, PatternIds.BullFlagContinuation],
            InvestmentAmount = 1000m,
            HorizonDays = 20
        };

        var expected = new MultiSimulationResultViewModel
        {
            Symbol = "AIR.PA",
            InvestmentAmount = 1000m,
            HorizonDays = 20,
            CurrentPrice = 145m,
            GlobalMessage = string.Empty,
            PatternResults =
            [
                TestInfrastructure.CreateSimulationResult(PatternIds.RectangleContinuation, RecommendationActionEnum.Buy),
                new SimulationResultViewModel
                {
                    Symbol = "AIR.PA",
                    Pattern = PatternIds.BullFlagContinuation,
                    Phase = string.Empty,
                    InvestmentAmount = 1000m,
                    HorizonDays = 0,
                    EstimatedReturnAmount = 0m,
                    EstimatedReturnPct = 0m,
                    EstimatedFinalAmount = 1000m,
                    Assumption = "Pattern non identifie sur cette valeur.",
                    CurrentPrice = 0m,
                    Probability = 0m,
                    RecommendationAction = RecommendationActionEnum.Hold,
                    RecommendationReason = $"Le pattern {PatternIds.BullFlagContinuation} n'a pas ete identifie sur AIR.PA.",
                    RiskLevel = RiskLevelEnum.Information,
                    IsActionable = false
                }
            ]
        };

        service.Setup(x => x.RunMultiSimulationAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = TestInfrastructure.CreateClientFinanceAnalysisController(service);

        var result = await controller.RunMultiSimulation(request, CancellationToken.None);

        var payload = TestInfrastructure.AssertOkObject<MultiSimulationResultViewModel>(result);
        Assert.Equal(2, payload.PatternResults.Count);
        Assert.True(payload.PatternResults[0].IsActionable);
        Assert.False(payload.PatternResults[1].IsActionable);
        Assert.Equal(PatternIds.BullFlagContinuation, payload.PatternResults[1].Pattern);
        service.Verify(x => x.RunMultiSimulationAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
