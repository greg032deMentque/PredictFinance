using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Portfolio;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using BackPredictFinance.Tests.Infrastructure;

namespace BackPredictFinance.Tests.Portfolio;

public sealed class PortfolioAllocationServiceIntegrationTests : IClassFixture<ApiIntegrationTestFactory>
{
    private static readonly DateTime CandleTimestampUtc = new(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TransactionTimestampUtc = new(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc);

    private readonly ApiIntegrationTestFactory _factory;

    public PortfolioAllocationServiceIntegrationTests(ApiIntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPortfolio_ForeignCurrencyAsset_ConvertsValueToEur()
    {
        await using var factory = BuildFactoryWithFixedPrices();

        var userId = $"it-alloc-fx-{Guid.NewGuid():N}";
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var assetId = $"asset-{suffix}";
        var symbol = $"US{suffix}.US";

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
            db.Assets.Add(BuildAsset(assetId, symbol, "USD"));
            db.UserAssets.Add(new UserAsset
            {
                Id = $"user-asset-{suffix}",
                UserId = userId,
                AssetId = assetId,
                Quantity = 10m
            });
            db.AssetCandleSnapshots.Add(new AssetCandleSnapshot
            {
                Id = $"candle-{suffix}",
                AssetId = assetId,
                TimestampUtc = CandleTimestampUtc,
                Interval = "1d",
                Close = 50m
            });
            await db.SaveChangesAsync();
        }

        var payload = await GetPortfolioAsync(factory, userId, portfolioId: null);

        Assert.NotNull(payload?.Allocation);
        var usdSlice = Assert.Single(payload!.Allocation!.CurrencyAllocation, x => x.Label == "USD");
        Assert.Equal(400.00m, usdSlice.ValueEur);
    }

    [Fact]
    public async Task GetPortfolio_ScopedByPortfolioId_ReturnsDifferentAllocationPerPortfolio()
    {
        await using var factory = BuildFactoryWithFixedPrices();

        var userId = $"it-alloc-scope-{Guid.NewGuid():N}";
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var assetTechId = $"asset-tech-{suffix}";
        var assetEnergyId = $"asset-energy-{suffix}";
        var userAssetTechId = $"user-asset-tech-{suffix}";
        var userAssetEnergyId = $"user-asset-energy-{suffix}";
        var portfolioTechId = $"portfolio-tech-{suffix}";
        var portfolioEnergyId = $"portfolio-energy-{suffix}";

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

            db.Assets.AddRange(
                BuildAsset(assetTechId, $"TECH{suffix}.PA", "EUR", "Technology"),
                BuildAsset(assetEnergyId, $"NRG{suffix}.PA", "EUR", "Energy"));

            db.UserAssets.AddRange(
                new UserAsset { Id = userAssetTechId, UserId = userId, AssetId = assetTechId, Quantity = 5m },
                new UserAsset { Id = userAssetEnergyId, UserId = userId, AssetId = assetEnergyId, Quantity = 3m });

            db.Portfolios.AddRange(
                new Datas.Entities.Portfolio { Id = portfolioTechId, UserId = userId, Name = "Tech", PortfolioType = PortfolioTypeEnum.CompteTitres, Status = PortfolioStatusEnum.Active },
                new Datas.Entities.Portfolio { Id = portfolioEnergyId, UserId = userId, Name = "Energy", PortfolioType = PortfolioTypeEnum.CompteTitres, Status = PortfolioStatusEnum.Active });

            db.AssetTransactions.AddRange(
                new AssetTransaction
                {
                    Id = $"tx-tech-{suffix}",
                    UserAssetId = userAssetTechId,
                    PortfolioId = portfolioTechId,
                    TimestampUtc = TransactionTimestampUtc,
                    TransactionType = TransactionTypeEnum.Buy,
                    Quantity = 5m,
                    UnitPrice = 100m,
                    Fees = 0m
                },
                new AssetTransaction
                {
                    Id = $"tx-energy-{suffix}",
                    UserAssetId = userAssetEnergyId,
                    PortfolioId = portfolioEnergyId,
                    TimestampUtc = TransactionTimestampUtc,
                    TransactionType = TransactionTypeEnum.Buy,
                    Quantity = 3m,
                    UnitPrice = 200m,
                    Fees = 0m
                });

            db.AssetCandleSnapshots.AddRange(
                new AssetCandleSnapshot { Id = $"candle-tech-{suffix}", AssetId = assetTechId, TimestampUtc = CandleTimestampUtc, Interval = "1d", Close = 100m },
                new AssetCandleSnapshot { Id = $"candle-energy-{suffix}", AssetId = assetEnergyId, TimestampUtc = CandleTimestampUtc, Interval = "1d", Close = 200m });

            await db.SaveChangesAsync();
        }

        var techPayload = await GetPortfolioAsync(factory, userId, portfolioTechId);
        var energyPayload = await GetPortfolioAsync(factory, userId, portfolioEnergyId);

        Assert.NotNull(techPayload?.Allocation);
        Assert.Contains(techPayload!.Allocation!.SectorAllocation, x => x.Label == "Technology");
        Assert.DoesNotContain(techPayload.Allocation.SectorAllocation, x => x.Label == "Energy");

        Assert.NotNull(energyPayload?.Allocation);
        Assert.Contains(energyPayload!.Allocation!.SectorAllocation, x => x.Label == "Energy");
        Assert.DoesNotContain(energyPayload.Allocation.SectorAllocation, x => x.Label == "Technology");
    }

    [Fact]
    public async Task GetPortfolio_WithAcquisitionCost_ReturnsNonNullReturn365d()
    {
        await using var factory = BuildFactoryWithFixedPrices();

        var userId = $"it-alloc-ret-{Guid.NewGuid():N}";
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var assetId = $"asset-ret-{suffix}";
        var userAssetId = $"user-asset-ret-{suffix}";

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

            db.Assets.Add(BuildAsset(assetId, $"RET{suffix}.PA", "EUR"));
            db.UserAssets.Add(new UserAsset { Id = userAssetId, UserId = userId, AssetId = assetId, Quantity = 10m });
            db.AssetTransactions.Add(new AssetTransaction
            {
                Id = $"tx-ret-{suffix}",
                UserAssetId = userAssetId,
                PortfolioId = string.Empty,
                TimestampUtc = TransactionTimestampUtc,
                TransactionType = TransactionTypeEnum.Buy,
                Quantity = 10m,
                UnitPrice = 80m,
                Fees = 0m
            });
            db.AssetCandleSnapshots.Add(new AssetCandleSnapshot
            {
                Id = $"candle-ret-{suffix}",
                AssetId = assetId,
                TimestampUtc = CandleTimestampUtc,
                Interval = "1d",
                Close = 100m
            });

            await db.SaveChangesAsync();
        }

        var payload = await GetPortfolioAsync(factory, userId, portfolioId: null);

        Assert.NotNull(payload?.Allocation);
        Assert.NotNull(payload!.Allocation!.PortfolioReturn365d);
        Assert.Equal(0.25m, payload.Allocation.PortfolioReturn365d);
    }

    [Fact]
    public async Task GetPortfolio_SoftDeletedSellTransaction_IsExcludedFromAllocation()
    {
        await using var factory = BuildFactoryWithFixedPrices();

        var userId = $"it-alloc-softdel-{Guid.NewGuid():N}";
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var assetId = $"asset-softdel-{suffix}";
        var userAssetId = $"user-asset-softdel-{suffix}";

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

            db.Assets.Add(BuildAsset(assetId, $"SOFTDEL{suffix}.PA", "EUR"));
            db.UserAssets.Add(new UserAsset { Id = userAssetId, UserId = userId, AssetId = assetId, Quantity = 10m });
            db.AssetTransactions.Add(new AssetTransaction
            {
                Id = $"tx-buy-{suffix}",
                UserAssetId = userAssetId,
                PortfolioId = string.Empty,
                TimestampUtc = TransactionTimestampUtc,
                TransactionType = TransactionTypeEnum.Buy,
                Quantity = 10m,
                UnitPrice = 80m,
                Fees = 0m
            });
            // Vente totale marquee supprimee : si le filtre !IsDeleted manque dans
            // PortfolioAllocationService, le FIFO la consomme quand meme et l'actif
            // disparait de l'allocation (quantite retombant a 0).
            db.AssetTransactions.Add(new AssetTransaction
            {
                Id = $"tx-sell-{suffix}",
                UserAssetId = userAssetId,
                PortfolioId = string.Empty,
                TimestampUtc = TransactionTimestampUtc.AddDays(30),
                TransactionType = TransactionTypeEnum.Sell,
                Quantity = 10m,
                UnitPrice = 150m,
                Fees = 0m,
                IsDeleted = true
            });
            db.AssetCandleSnapshots.Add(new AssetCandleSnapshot
            {
                Id = $"candle-softdel-{suffix}",
                AssetId = assetId,
                TimestampUtc = CandleTimestampUtc,
                Interval = "1d",
                Close = 100m
            });

            await db.SaveChangesAsync();
        }

        var payload = await GetPortfolioAsync(factory, userId, portfolioId: null);

        Assert.NotNull(payload?.Allocation);
        var eurSlice = Assert.Single(payload!.Allocation!.CurrencyAllocation, x => x.Label == "EUR");
        Assert.Equal(1000.00m, eurSlice.ValueEur);
        Assert.NotNull(payload.Allocation.PortfolioReturn365d);
        Assert.Equal(0.25m, payload.Allocation.PortfolioReturn365d);
    }

    private WebApplicationFactory<Program> BuildFactoryWithFixedPrices()
        => _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IMarketPriceProvider>();
                services.AddSingleton<IMarketPriceProvider, FixedMarketPriceProvider>();
            });
        });

    private static async Task<PortfolioViewModel?> GetPortfolioAsync(
        WebApplicationFactory<Program> factory,
        string userId,
        string? portfolioId)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "integration-test");
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, userId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeaderName, UserRoleEnum.User.ToString());

        var url = string.IsNullOrWhiteSpace(portfolioId)
            ? "/api/ClientFinance/portfolio"
            : $"/api/ClientFinance/portfolio?portfolioId={portfolioId}";

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var deserializationOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };
        return await response.Content.ReadFromJsonAsync<PortfolioViewModel>(deserializationOptions);
    }

    private static Asset BuildAsset(string id, string symbol, string currency, string? sector = null) => new()
    {
        Id = id,
        Symbol = symbol,
        ProviderSymbol = symbol,
        Name = symbol,
        Exchange = "XPAR",
        Currency = currency,
        Country = "FR",
        Sector = sector,
        AssetType = AssetTypeEnum.Stock,
        LastProfileSyncUtc = CandleTimestampUtc
    };

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
            LastPrice = symbol.Equals("EURUSD=X", StringComparison.OrdinalIgnoreCase) ? 1.25m : 100m,
            DayVariationPct = 0m,
            AsOfUtc = CandleTimestampUtc
        };
    }
}
