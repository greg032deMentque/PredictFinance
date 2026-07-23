using System.Net;
using System.Text;
using BackPredictFinance.Common;
using BackPredictFinance.Services.TwelveDataServices;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BackPredictFinance.Tests.MarketData;

public sealed class YahooQuoteSummaryToleranceTests
{
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    private static YahooFinanceMarketDataProvider BuildProvider(string quoteSummaryJson, string chartJson)
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            var uri = request.RequestUri?.AbsoluteUri ?? string.Empty;
            var body = uri.Contains("quoteSummary") ? quoteSummaryJson : chartJson;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new MarketDataOptions());

        var crumbServiceMock = new Mock<IYahooCrumbService>();
        crumbServiceMock
            .Setup(x => x.GetCredentialsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YahooCredentials("test-crumb", "test-cookie"));

        return new YahooFinanceMarketDataProvider(
            httpClient,
            cache,
            options,
            crumbServiceMock.Object,
            NullLogger<YahooFinanceMarketDataProvider>.Instance);
    }

    private static string BuildChartJson(string symbol) => $$"""
        {
          "chart": {
            "result": [{
              "meta": { "currency": "EUR", "symbol": "{{symbol}}", "instrumentType": "EQUITY" },
              "timestamp": [1748000000, 1748086400],
              "indicators": {
                "quote": [{
                  "open": [100.0, 101.0],
                  "high": [102.0, 103.0],
                  "low": [99.0, 100.0],
                  "close": [101.0, 102.0],
                  "volume": [10000, 12000]
                }]
              }
            }],
            "error": null
          }
        }
        """;

    [Fact]
    public async Task GetFundamentalsAsync_WhenFinancialDataModuleAbsent_ReturnsPartialDataWithoutThrowing()
    {
        const string symbol = "AIR.PA";

        var quoteSummaryJson = """
            {
              "quoteSummary": {
                "result": [{
                  "price": {
                    "symbol": "AIR.PA",
                    "longName": "Airbus SE",
                    "shortName": "AIRBUS",
                    "quoteType": "EQUITY",
                    "exchangeName": "XPAR",
                    "currency": "EUR"
                  },
                  "summaryProfile": {
                    "country": "France",
                    "sector": "Industrials",
                    "longBusinessSummary": "Airbus est un constructeur aéronautique."
                  },
                  "summaryDetail": {
                    "trailingPE": { "raw": 22.5, "fmt": "22.5" },
                    "dividendYield": { "raw": 0.015, "fmt": "1.5%" }
                  }
                }],
                "error": null
              }
            }
            """;

        var provider = BuildProvider(quoteSummaryJson, BuildChartJson(symbol));

        var result = await provider.GetFundamentalsAsync(symbol);

        Assert.NotNull(result);
        Assert.Equal("AIR.PA", result.Symbol);
        Assert.Equal("Airbus SE", result.CompanyName);
        Assert.Equal("FR", result.Country);
        Assert.Null(result.ReturnOnEquity);
        Assert.Null(result.OperatingMargin);
        Assert.Null(result.CurrentRatio);
        Assert.Null(result.DebtToEquity);
        Assert.NotNull(result.TrailingPe);
        Assert.Equal(22.5m, decimal.Round(result.TrailingPe!.Value, 1));
    }

    [Fact]
    public async Task GetFundamentalsAsync_WhenAllOptionalModulesAbsent_ReturnsBaseFieldsWithoutThrowing()
    {
        const string symbol = "MC.PA";

        var quoteSummaryJson = """
            {
              "quoteSummary": {
                "result": [{
                  "price": {
                    "symbol": "MC.PA",
                    "longName": "LVMH",
                    "quoteType": "EQUITY",
                    "exchangeName": "XPAR",
                    "currency": "EUR"
                  }
                }],
                "error": null
              }
            }
            """;

        var provider = BuildProvider(quoteSummaryJson, BuildChartJson(symbol));

        var result = await provider.GetFundamentalsAsync(symbol);

        Assert.NotNull(result);
        Assert.Equal("MC.PA", result.Symbol);
        Assert.Equal("LVMH", result.CompanyName);
        Assert.Empty(result.Country);
        Assert.Empty(result.Sector);
        Assert.Null(result.ReturnOnEquity);
        Assert.Null(result.TrailingPe);
        Assert.Null(result.DividendYield);
    }

    [Fact]
    public async Task GetFundamentalsAsync_CalendarEventsEarningsDateIsScalarObject_ParsesRawTimestamp()
    {
        const string symbol = "AIR.PA";
        var rawTimestamp = new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();

        var quoteSummaryJson = $$"""
            {
              "quoteSummary": {
                "result": [{
                  "price": { "symbol": "AIR.PA", "longName": "Airbus SE", "quoteType": "EQUITY", "exchangeName": "XPAR", "currency": "EUR" },
                  "calendarEvents": {
                    "earningsDate": { "raw": {{rawTimestamp}}, "fmt": "2026-08-15" }
                  }
                }],
                "error": null
              }
            }
            """;

        var provider = BuildProvider(quoteSummaryJson, BuildChartJson(symbol));

        var result = await provider.GetFundamentalsAsync(symbol);

        Assert.NotNull(result.EarningsDate);
        Assert.Equal(new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc), result.EarningsDate!.Value);
    }

    [Fact]
    public async Task GetFundamentalsAsync_CalendarEventsEarningsDateIsArrayWithFutureAndPastDates_PicksNearestFuture()
    {
        const string symbol = "AIR.PA";
        var pastTimestamp = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var nearFutureTimestamp = new DateTimeOffset(2027, 3, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var farFutureTimestamp = new DateTimeOffset(2027, 6, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();

        var quoteSummaryJson = $$"""
            {
              "quoteSummary": {
                "result": [{
                  "price": { "symbol": "AIR.PA", "longName": "Airbus SE", "quoteType": "EQUITY", "exchangeName": "XPAR", "currency": "EUR" },
                  "calendarEvents": {
                    "earningsDate": [
                      { "raw": {{farFutureTimestamp}}, "fmt": "2027-06-01" },
                      { "raw": {{pastTimestamp}}, "fmt": "2020-01-01" },
                      { "raw": {{nearFutureTimestamp}}, "fmt": "2027-03-01" }
                    ]
                  }
                }],
                "error": null
              }
            }
            """;

        var provider = BuildProvider(quoteSummaryJson, BuildChartJson(symbol));

        var result = await provider.GetFundamentalsAsync(symbol);

        Assert.NotNull(result.EarningsDate);
        Assert.Equal(new DateTime(2027, 3, 1, 0, 0, 0, DateTimeKind.Utc), result.EarningsDate!.Value);
    }

    [Fact]
    public async Task GetFundamentalsAsync_CalendarEventsEarningsDateIsArrayWithOnlyPastDates_PicksMostRecentPast()
    {
        const string symbol = "AIR.PA";
        var olderTimestamp = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var recentPastTimestamp = new DateTimeOffset(2021, 6, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();

        var quoteSummaryJson = $$"""
            {
              "quoteSummary": {
                "result": [{
                  "price": { "symbol": "AIR.PA", "longName": "Airbus SE", "quoteType": "EQUITY", "exchangeName": "XPAR", "currency": "EUR" },
                  "calendarEvents": {
                    "earningsDate": [
                      { "raw": {{olderTimestamp}}, "fmt": "2020-01-01" },
                      { "raw": {{recentPastTimestamp}}, "fmt": "2021-06-01" }
                    ]
                  }
                }],
                "error": null
              }
            }
            """;

        var provider = BuildProvider(quoteSummaryJson, BuildChartJson(symbol));

        var result = await provider.GetFundamentalsAsync(symbol);

        Assert.NotNull(result.EarningsDate);
        Assert.Equal(new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc), result.EarningsDate!.Value);
    }

    [Fact]
    public async Task GetFundamentalsAsync_CalendarEventsModuleAbsent_ReturnsNullEarningsDateWithoutThrowing()
    {
        const string symbol = "MC.PA";

        var quoteSummaryJson = """
            {
              "quoteSummary": {
                "result": [{
                  "price": { "symbol": "MC.PA", "longName": "LVMH", "quoteType": "EQUITY", "exchangeName": "XPAR", "currency": "EUR" }
                }],
                "error": null
              }
            }
            """;

        var provider = BuildProvider(quoteSummaryJson, BuildChartJson(symbol));

        var result = await provider.GetFundamentalsAsync(symbol);

        Assert.Null(result.EarningsDate);
    }
}
