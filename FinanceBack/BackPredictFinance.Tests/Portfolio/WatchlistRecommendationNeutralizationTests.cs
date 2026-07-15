using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Watchlist;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using BackPredictFinance.Tests.Infrastructure;

namespace BackPredictFinance.Tests.Portfolio;

public sealed class WatchlistRecommendationNeutralizationTests : IClassFixture<ApiIntegrationTestFactory>
{
    private static readonly JsonSerializerOptions SnapshotOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiIntegrationTestFactory _factory;

    public WatchlistRecommendationNeutralizationTests(ApiIntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetWatchlist_WhenSnapshotWasComputedAsHeld_ButAssetIsNowNotHeld_ReturnsWaitVerb()
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
        var symbol = $"NZ{suffix}.PA";
        var assetId = $"asset-{symbol}";
        var userAssetId = $"ua-{symbol}";
        var analysisRunId = $"run-{symbol}";

        var snapshotPayload = new PersistedAnalysisSnapshotPayloadReadModel
        {
            SchemaVersion = "1.0",
            SnapshotId = Guid.NewGuid().ToString("N"),
            Outcome = AnalysisOutcome.CrediblePatternFound,
            CompletedAtUtc = DateTime.UtcNow.AddDays(-1),
            PortfolioContextSnapshot = new SnapshotPortfolioContextSummary
            {
                HoldsInstrument = true
            },
            Recommendation = new AnalysisSnapshotRecommendation
            {
                RecommendationPayload = new AnalysisRecommendation
                {
                    Kind = RecommendationKind.Hold,
                    HoldingContext = HoldingStatusEnum.Held,
                    Rationale = "Pattern confirme en contexte detenu."
                }
            },
            PatternRows = []
        };

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
                LastProfileSyncUtc = DateTime.UtcNow
            });
            db.UserAssets.Add(new UserAsset
            {
                Id = userAssetId,
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
                StartedAtUtc = DateTime.UtcNow.AddDays(-1),
                CompletedAtUtc = DateTime.UtcNow.AddDays(-1),
                RawPayload = JsonSerializer.Serialize(snapshotPayload, SnapshotOptions)
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
        var payload = await response.Content.ReadFromJsonAsync<List<WatchlistItemViewModel>>(deserializationOptions);
        Assert.NotNull(payload);
        var item = Assert.Single(payload!, x => x.Symbol == symbol);
        Assert.Equal(HoldingStatusEnum.NotHeld, item.HoldingStatus);
        Assert.Equal(RecommendationKind.Wait, item.Recommendation.Kind);
        Assert.Equal("Attendre", item.Recommendation.DisplayLabel);
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
            AsOfUtc = DateTime.UtcNow
        };
    }
}
