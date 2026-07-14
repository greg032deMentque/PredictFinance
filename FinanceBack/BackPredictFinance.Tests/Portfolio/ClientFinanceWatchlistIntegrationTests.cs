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

public sealed class ClientFinanceWatchlistIntegrationTests : IClassFixture<ApiIntegrationTestFactory>
{
    private readonly ApiIntegrationTestFactory _factory;

    public ClientFinanceWatchlistIntegrationTests(ApiIntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetWatchlist_ExcludesHeldAssetsFromClientWatchlist()
    {
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITickerService>();
                services.AddSingleton<ITickerService, FixedTickerService>();
            });
        });

        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var nonHeldSymbol = $"NH{suffix}.PA";
        var heldSymbol = $"HD{suffix}.PA";

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
            db.Assets.AddRange(
                BuildAsset($"asset-{nonHeldSymbol}", nonHeldSymbol),
                BuildAsset($"asset-{heldSymbol}", heldSymbol));
            db.UserAssets.AddRange(
                new UserAsset
                {
                    Id = $"user-asset-{nonHeldSymbol}",
                    UserId = ApiIntegrationTestFactory.StandardUserId,
                    AssetId = $"asset-{nonHeldSymbol}",
                    Quantity = 0m
                },
                new UserAsset
                {
                    Id = $"user-asset-{heldSymbol}",
                    UserId = ApiIntegrationTestFactory.StandardUserId,
                    AssetId = $"asset-{heldSymbol}",
                    Quantity = 4m
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
        Assert.Contains(payload!, x => x.Symbol == nonHeldSymbol && x.HoldingStatus == HoldingStatusEnum.NotHeld);
        Assert.DoesNotContain(payload!, x => x.Symbol == heldSymbol);
    }

    private static Asset BuildAsset(string id, string symbol) => new()
    {
        Id = id,
        Symbol = symbol,
        ProviderSymbol = symbol,
        Name = symbol,
        Exchange = "XPAR",
        Currency = "EUR",
        Country = "FR",
        AssetType = AssetTypeEnum.Stock,
        LastProfileSyncUtc = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc)
    };

    private sealed class FixedTickerService : ITickerService
    {
        public Task<IReadOnlyList<string>> GetExchangesAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>(["XPAR"]);

        public Task<IReadOnlyList<string>> GetSymbolsByExchangeAsync(string exchange, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyList<string>> GetAllSymbolsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<TickerTimeSeriesResponse> GetTimeSeriesAsync(string symbol, string interval, int outputSize, CancellationToken ct = default)
            => Task.FromResult(new TickerTimeSeriesResponse());

        public Task<IReadOnlyList<MarketAssetDescriptor>> SearchAssetsAsync(string query, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MarketAssetDescriptor>>([]);

        public Task<MarketQuoteData> GetQuoteAsync(string symbol, CancellationToken ct = default)
            => Task.FromResult(new MarketQuoteData
            {
                Symbol = symbol,
                AssetType = AssetTypeEnum.Stock,
                LastPrice = 100m,
                DayVariationPct = 0.5m,
                AsOfUtc = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc)
            });

        public Task<MarketAssetProfileData> GetAssetProfileAsync(string symbol, CancellationToken ct = default)
            => Task.FromResult(new MarketAssetProfileData
            {
                Symbol = symbol,
                ProviderSymbol = symbol,
                CompanyName = symbol,
                AssetType = AssetTypeEnum.Stock,
                Exchange = "XPAR",
                Currency = "EUR",
                Country = "FR",
                LastPrice = 100m,
                DayVariationPct = 0.5m,
                AsOfUtc = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc)
            });
    }
}
