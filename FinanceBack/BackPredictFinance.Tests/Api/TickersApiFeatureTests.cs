using BackPredictFinance.Common.MarketData;
using Moq;

using BackPredictFinance.Tests.Infrastructure;

namespace BackPredictFinance.Tests.Api;

public sealed class TickersApiFeatureTests
{
    [Fact]
    public async Task GetExchanges_ReturnsProviderExchanges()
    {
        var service = TestInfrastructure.CreateTickerServiceMock();
        var expected = new List<string> { "PAR", "EPA" };
        service.Setup(x => x.GetExchangesAsync()).ReturnsAsync(expected);
        var controller = TestInfrastructure.CreateTickersController(service);

        var result = await controller.GetExchanges();

        var payload = TestInfrastructure.AssertOkObject<List<string>>(result);
        Assert.Equal(expected, payload);
    }

    [Fact]
    public async Task GetSymbols_ReturnsSymbolsForRequestedExchange()
    {
        var service = TestInfrastructure.CreateTickerServiceMock();
        var expected = new List<string> { "AIR.PA", "MC.PA" };
        service.Setup(x => x.GetSymbolsByExchangeAsync("PAR")).ReturnsAsync(expected);
        var controller = TestInfrastructure.CreateTickersController(service);

        var result = await controller.GetSymbols("PAR");

        var payload = TestInfrastructure.AssertOkObject<List<string>>(result);
        Assert.Equal(expected, payload);
    }

    [Fact]
    public async Task GetAllSymbols_ReturnsFullSymbolList()
    {
        var service = TestInfrastructure.CreateTickerServiceMock();
        var expected = new List<string> { "AIR.PA", "MC.PA", "OR.PA" };
        service.Setup(x => x.GetAllSymbolsAsync()).ReturnsAsync(expected);
        var controller = TestInfrastructure.CreateTickersController(service);

        var result = await controller.GetAllSymbols();

        var payload = TestInfrastructure.AssertOkObject<List<string>>(result);
        Assert.Equal(3, payload.Count);
    }

    [Fact]
    public async Task GetTimeSeries_ReturnsRequestedTimeSeries()
    {
        var service = TestInfrastructure.CreateTickerServiceMock();
        var expected = new TickerTimeSeriesResponse
        {
            Symbol = "AIR.PA",
            Interval = "1day",
            Candles =
            [
                new TickerCandle { Date = new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc), Open = 100m, High = 102m, Low = 99m, Close = 101m, Volume = 10_000m }
            ]
        };
        service.Setup(x => x.GetTimeSeriesAsync("AIR.PA", "1day", 100, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = TestInfrastructure.CreateTickersController(service);

        var result = await controller.GetTimeSeries("AIR.PA", "1day", 100);

        var payload = TestInfrastructure.AssertOkObject<TickerTimeSeriesResponse>(result);
        Assert.Equal("AIR.PA", payload.Symbol);
        Assert.Single(payload.Candles);
    }
}
