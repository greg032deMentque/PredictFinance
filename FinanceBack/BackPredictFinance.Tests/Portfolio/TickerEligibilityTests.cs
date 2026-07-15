using BackPredictFinance.Common;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.TwelveDataServices;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BackPredictFinance.Tests.Portfolio;

public sealed class TickerEligibilityTests
{
    private static FinanceDbContext BuildInMemoryDb(string name)
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        return new FinanceDbContext(options, httpContextAccessorMock.Object);
    }

    private static TickerService BuildService(
        IMarketCatalogProvider catalogProvider,
        FinanceDbContext? db = null)
    {
        db ??= BuildInMemoryDb($"ticker-elig-{Guid.NewGuid():N}");

        var priceProviderMock = new Mock<IMarketPriceProvider>(MockBehavior.Strict);

        return new TickerService(
            catalogProvider,
            priceProviderMock.Object,
            db,
            NullLogger<TickerService>.Instance);
    }

    private static MarketAssetProfileData BuildProfile(
        string symbol,
        AssetTypeEnum assetType,
        string exchange = "XPAR",
        string currency = "EUR",
        string country = "US",
        decimal lastPrice = 100m) => new()
    {
        Symbol = symbol,
        ProviderSymbol = symbol,
        CompanyName = symbol,
        AssetType = assetType,
        Exchange = exchange,
        Currency = currency,
        Country = country,
        LastPrice = lastPrice
    };

    [Fact]
    public async Task GetAssetProfile_EtfWithForeignCountry_Accepted()
    {
        var catalogMock = new Mock<IMarketCatalogProvider>(MockBehavior.Strict);
        catalogMock
            .Setup(x => x.GetAssetProfileAsync("CW8.PA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildProfile("CW8.PA", AssetTypeEnum.Etf, exchange: "XPAR", currency: "EUR", country: "LU"));

        var service = BuildService(catalogMock.Object);
        var result = await service.GetAssetProfileAsync("CW8.PA");

        Assert.Equal("CW8.PA", result.Symbol);
        Assert.Equal(AssetTypeEnum.Etf, result.AssetType);
        Assert.Equal("LU", result.Country);
    }

    [Fact]
    public async Task GetAssetProfile_StockForeignCountry_Accepted()
    {
        var catalogMock = new Mock<IMarketCatalogProvider>(MockBehavior.Strict);
        catalogMock
            .Setup(x => x.GetAssetProfileAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildProfile("AAPL", AssetTypeEnum.Stock, exchange: "NMS", currency: "USD", country: "US"));

        var service = BuildService(catalogMock.Object);
        var result = await service.GetAssetProfileAsync("AAPL");

        Assert.Equal("AAPL", result.Symbol);
        Assert.Equal(AssetTypeEnum.Stock, result.AssetType);
        Assert.Equal("US", result.Country);
    }

    [Fact]
    public async Task GetAssetProfile_CryptoAsset_Rejected()
    {
        var catalogMock = new Mock<IMarketCatalogProvider>(MockBehavior.Strict);
        catalogMock
            .Setup(x => x.GetAssetProfileAsync("BTC-USD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildProfile("BTC-USD", AssetTypeEnum.Crypto, exchange: "CCC", currency: "USD", country: ""));

        var service = BuildService(catalogMock.Object);

        var ex = await Assert.ThrowsAsync<CustomException>(
            () => service.GetAssetProfileAsync("BTC-USD"));

        Assert.Contains("crypto", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAssetProfile_MissingPrice_Rejected()
    {
        var catalogMock = new Mock<IMarketCatalogProvider>(MockBehavior.Strict);
        catalogMock
            .Setup(x => x.GetAssetProfileAsync("ETF.PA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildProfile("ETF.PA", AssetTypeEnum.Etf, exchange: "XPAR", currency: "EUR", country: "FR", lastPrice: 0m));

        var service = BuildService(catalogMock.Object);

        await Assert.ThrowsAsync<CustomException>(
            () => service.GetAssetProfileAsync("ETF.PA"));
    }

    [Fact]
    public async Task GetAllSymbols_IncludesEtfAndStock_ExcludesCrypto()
    {
        var dbName = $"ticker-symbols-{Guid.NewGuid():N}";
        var db = BuildInMemoryDb(dbName);

        db.Assets.AddRange(
            new Asset { Id = "a1", Symbol = "AIR.PA", ProviderSymbol = "AIR.PA", Exchange = "XPAR", Currency = "EUR", AssetType = AssetTypeEnum.Stock, Country = "FR" },
            new Asset { Id = "a2", Symbol = "CW8.PA", ProviderSymbol = "CW8.PA", Exchange = "XPAR", Currency = "EUR", AssetType = AssetTypeEnum.Etf, Country = "LU" },
            new Asset { Id = "a3", Symbol = "BTC-USD", ProviderSymbol = "BTC-USD", Exchange = "CCC", Currency = "USD", AssetType = AssetTypeEnum.Crypto, Country = "" });
        await db.SaveChangesAsync();

        var catalogMock = new Mock<IMarketCatalogProvider>(MockBehavior.Strict);
        var service = BuildService(catalogMock.Object, db);

        var symbols = await service.GetAllSymbolsAsync();

        Assert.Contains("AIR.PA", symbols);
        Assert.Contains("CW8.PA", symbols);
        Assert.DoesNotContain("BTC-USD", symbols);
    }
}
