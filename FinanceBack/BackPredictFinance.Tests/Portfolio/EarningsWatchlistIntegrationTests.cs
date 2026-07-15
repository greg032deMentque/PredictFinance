using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Watchlist;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using BackPredictFinance.Tests.Infrastructure;

namespace BackPredictFinance.Tests.Portfolio;

public sealed class EarningsWatchlistIntegrationTests : IClassFixture<ApiIntegrationTestFactory>
{
    private readonly ApiIntegrationTestFactory _factory;

    public EarningsWatchlistIntegrationTests(ApiIntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetWatchlist_LatestAnalysisHasEarningsWithinReviewHorizon_ExposesWarningAndDate()
    {
        var earningsDateUtc = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var completedAtUtc = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

        var payload = await RunWatchlistScenarioAsync(earningsDateUtc, completedAtUtc, reviewHorizonDays: 20);

        Assert.True(payload.EarningsWithinHorizonWarning);
        Assert.Equal(earningsDateUtc, payload.NextEarningsDateUtc);
    }

    [Fact]
    public async Task GetWatchlist_LatestAnalysisHasEarningsBeyondReviewHorizon_ExposesDateWithoutWarning()
    {
        var earningsDateUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var completedAtUtc = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

        var payload = await RunWatchlistScenarioAsync(earningsDateUtc, completedAtUtc, reviewHorizonDays: 20);

        Assert.False(payload.EarningsWithinHorizonWarning);
        Assert.Equal(earningsDateUtc, payload.NextEarningsDateUtc);
    }

    [Fact]
    public async Task GetWatchlist_LatestAnalysisHasNoEarningsDate_NeverWarns()
    {
        var completedAtUtc = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

        var payload = await RunWatchlistScenarioAsync(earningsDateUtc: null, completedAtUtc, reviewHorizonDays: 20);

        Assert.False(payload.EarningsWithinHorizonWarning);
        Assert.Null(payload.NextEarningsDateUtc);
    }

    private async Task<WatchlistItemViewModel> RunWatchlistScenarioAsync(DateTime? earningsDateUtc, DateTime completedAtUtc, int reviewHorizonDays)
    {
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IMarketPriceProvider>();
                services.AddSingleton<IMarketPriceProvider, FixedMarketPriceProvider>();
            });
        });

        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var symbol = $"EW{suffix}.PA";
        var assetId = $"asset-{symbol}";
        var analysisRunId = $"run-{symbol}";

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
            db.Assets.Add(new Asset
            {
                Id = assetId,
                Symbol = symbol,
                ProviderSymbol = symbol,
                Name = symbol,
                Exchange = "XPAR",
                Currency = "EUR",
                Country = "FR",
                AssetType = AssetTypeEnum.Stock,
                LastProfileSyncUtc = completedAtUtc
            });
            db.UserAssets.Add(new UserAsset
            {
                Id = $"user-asset-{symbol}",
                UserId = ApiIntegrationTestFactory.StandardUserId,
                AssetId = assetId,
                Quantity = 0m
            });
            db.AnalysisRuns.Add(new AnalysisRun
            {
                Id = analysisRunId,
                UserId = ApiIntegrationTestFactory.StandardUserId,
                AssetId = assetId,
                Status = AnalysisRunStatusEnum.Completed,
                StartedAtUtc = completedAtUtc.AddMinutes(-1),
                CompletedAtUtc = completedAtUtc,
                RawPayload = BuildRawPayloadJson(completedAtUtc, reviewHorizonDays)
            });
            db.DecisionSignals.Add(new DecisionSignal
            {
                Id = $"signal-{symbol}",
                AnalysisRunId = analysisRunId,
                Action = RecommendationActionEnum.Buy,
                IsActionable = true,
                Confidence = 0.75m,
                HorizonDays = reviewHorizonDays,
                Reason = "Scenario de test.",
                EarningsDateUtc = earningsDateUtc
            });
            await db.SaveChangesAsync();
        }

        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "integration-test");
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, ApiIntegrationTestFactory.StandardUserId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeaderName, UserRoleEnum.User.ToString());

        var response = await client.GetAsync("/api/ClientFinance/watchlist");
        response.EnsureSuccessStatusCode();

        var deserializationOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };
        var items = await response.Content.ReadFromJsonAsync<List<WatchlistItemViewModel>>(deserializationOptions);

        Assert.NotNull(items);
        var item = Assert.Single(items!, x => x.Symbol == symbol);
        return item;
    }

    private static string BuildRawPayloadJson(DateTime completedAtUtc, int reviewHorizonDays)
    {
        var completedAtUtcIso = completedAtUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        return $$"""
            {
              "outcome": 0,
              "completedAtUtc": "{{completedAtUtcIso}}",
              "recommendation": {
                "recommendationPayload": {
                  "recommendationId": "reco-1",
                  "kind": 1,
                  "reviewHorizonDays": {{reviewHorizonDays}},
                  "rationale": "Scenario de test."
                }
              }
            }
            """;
    }

    private sealed class FixedMarketPriceProvider : IMarketPriceProvider
    {
        public Task<MarketQuoteData> GetQuoteAsync(string symbol, CancellationToken ct = default)
            => Task.FromResult(BuildQuote(symbol));

        public Task<IReadOnlyDictionary<string, MarketQuoteData>> GetQuotesAsync(IEnumerable<string> symbols, CancellationToken ct = default)
        {
            var result = symbols
                .Select(BuildQuote)
                .ToDictionary(x => x.Symbol, x => x, StringComparer.OrdinalIgnoreCase);
            return Task.FromResult<IReadOnlyDictionary<string, MarketQuoteData>>(result);
        }

        public Task<IReadOnlyList<TickerCandle>> GetChartAsync(string symbol, string interval, string range, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TickerCandle>>([]);

        private static MarketQuoteData BuildQuote(string symbol) => new()
        {
            Symbol = symbol,
            AssetType = AssetTypeEnum.Stock,
            LastPrice = 100m,
            DayVariationPct = 0.5m,
            AsOfUtc = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc)
        };
    }
}
