using BackPredictFinance.Tests.Infrastructure;

namespace BackPredictFinance.Tests.Api;

public sealed class TradingRetiredApiFeatureTests
{
    [Fact]
    public void Predict_ReturnsGoneProblemDetails()
    {
        var controller = TestInfrastructure.CreateTradingController();

        var result = controller.Predict();

        var payload = TestInfrastructure.AssertGoneProblem(result);
        Assert.Equal("Endpoint retired", payload.Title);
    }

    [Fact]
    public void PredictBySymbol_ReturnsGoneProblemDetails()
    {
        var controller = TestInfrastructure.CreateTradingController();

        var result = controller.PredictBySymbol("AIR.PA");

        var payload = TestInfrastructure.AssertGoneProblem(result);
        Assert.Contains("V1", payload.Detail, StringComparison.OrdinalIgnoreCase);
    }
}
