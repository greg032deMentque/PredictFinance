using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.Patterns;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Assets;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BackPredictFinance.Tests.MarketData;

public sealed class AssetSearchServiceTests
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

    private static ClientFinanceService BuildService(FinanceDbContext db, Mock<IMarketCatalogProvider>? catalogMock = null)
    {
        var assetSupportMock = new Mock<IClientFinanceAssetSupportService>(MockBehavior.Strict);
        assetSupportMock
            .Setup(x => x.MapAssetType(It.IsAny<AssetTypeEnum>()))
            .Returns((AssetTypeEnum t) => t.ToString());

        var catalog = catalogMock ?? new Mock<IMarketCatalogProvider>(MockBehavior.Strict);
        if (catalogMock is null)
        {
            catalog.Setup(x => x.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync([]);
        }

        return new ClientFinanceService(
            db,
            new Mock<IAnalysisRequestCompatibilityResolver>(MockBehavior.Strict).Object,
            new Mock<IAnalysisOrchestrator>(MockBehavior.Strict).Object,
            new Mock<IAnalysisPatternRegistry>(MockBehavior.Strict).Object,
            assetSupportMock.Object,
            new Mock<IAnalysisResultProjectionService>(MockBehavior.Strict).Object,
            catalog.Object,
            NullLogger<ClientFinanceService>.Instance);
    }

    private static Asset BuildAsset(string id, string symbol, string name) => new()
    {
        Id = id,
        Symbol = symbol,
        ProviderSymbol = symbol,
        Name = name,
        Exchange = "XPAR",
        Currency = "EUR",
        AssetType = AssetTypeEnum.Stock
    };

    private static AssetPeaEligibility BuildEligibility(string assetId, PeaEligibilityStatusEnum status) => new()
    {
        Id = Guid.NewGuid().ToString(),
        AssetId = assetId,
        EligibilityStatus = status,
        SourceType = PeaEligibilitySourceTypeEnum.ManualRegistry,
        UniverseId = "PEA_FR_EQUITIES"
    };

    [Fact]
    public async Task SearchAssetsAsync_PeaEligibleOnly_ReturnsOnlyConfirmedEligibleAssets()
    {
        var dbName = $"search-pea-{Guid.NewGuid():N}";
        await using var db = BuildInMemoryDb(dbName);

        var eligibleAsset = BuildAsset("asset-eligible-1", "AIR.PA", "Airbus");
        var ineligibleAsset = BuildAsset("asset-ineligible-1", "BTC-USD", "Bitcoin");
        var unknownAsset = BuildAsset("asset-unknown-1", "AAPL", "Apple");

        db.Assets.AddRange(eligibleAsset, ineligibleAsset, unknownAsset);
        db.AssetPeaEligibilities.Add(BuildEligibility("asset-eligible-1", PeaEligibilityStatusEnum.ConfirmedEligible));
        db.AssetPeaEligibilities.Add(BuildEligibility("asset-ineligible-1", PeaEligibilityStatusEnum.ConfirmedIneligible));
        db.AssetPeaEligibilities.Add(BuildEligibility("asset-unknown-1", PeaEligibilityStatusEnum.Unknown));
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var results = await service.SearchAssetsAsync("A", peaEligibleOnly: true);

        Assert.Single(results);
        Assert.Equal("AIR.PA", results[0].Symbol);
        Assert.True(results[0].IsPeaEligible);
    }

    [Fact]
    public async Task SearchAssetsAsync_MatchesBySymbolCaseInsensitive()
    {
        var dbName = $"search-symbol-{Guid.NewGuid():N}";
        await using var db = BuildInMemoryDb(dbName);

        db.Assets.Add(BuildAsset("asset-1", "AIR.PA", "Airbus"));
        db.Assets.Add(BuildAsset("asset-2", "MC.PA", "LVMH"));
        await db.SaveChangesAsync();

        var service = BuildService(db);

        var upperResult = await service.SearchAssetsAsync("air");
        var lowerResult = await service.SearchAssetsAsync("AIR");

        Assert.Single(upperResult);
        Assert.Equal("AIR.PA", upperResult[0].Symbol);
        Assert.Single(lowerResult);
        Assert.Equal("AIR.PA", lowerResult[0].Symbol);
    }

    [Fact]
    public async Task SearchAssetsAsync_MatchesByNameCaseInsensitive()
    {
        var dbName = $"search-name-{Guid.NewGuid():N}";
        await using var db = BuildInMemoryDb(dbName);

        db.Assets.Add(BuildAsset("asset-1", "AIR.PA", "Airbus SE"));
        db.Assets.Add(BuildAsset("asset-2", "MC.PA", "LVMH Moet Hennessy"));
        await db.SaveChangesAsync();

        var service = BuildService(db);

        var upperResult = await service.SearchAssetsAsync("LVMH");
        var lowerResult = await service.SearchAssetsAsync("lvmh");

        Assert.Single(upperResult);
        Assert.Equal("MC.PA", upperResult[0].Symbol);
        Assert.Single(lowerResult);
        Assert.Equal("MC.PA", lowerResult[0].Symbol);
    }

    [Fact]
    public async Task SearchAssetsAsync_MergesYahooResults_DeduplicatesBySymbol_LocalVersionWins()
    {
        var dbName = $"search-merge-{Guid.NewGuid():N}";
        await using var db = BuildInMemoryDb(dbName);

        var localAsset = BuildAsset("asset-local-1", "AIR.PA", "Airbus SE");
        localAsset.Sector = "Aerospace";
        localAsset.Isin = "FR0000120073";
        db.Assets.Add(localAsset);
        await db.SaveChangesAsync();

        var catalogMock = new Mock<IMarketCatalogProvider>(MockBehavior.Strict);
        catalogMock
            .Setup(x => x.SearchAssetsAsync("airbus", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new MarketAssetDescriptor
                {
                    Symbol = "AIR.PA",
                    CompanyName = "Airbus (Yahoo)",
                    Exchange = "EPA",
                    Currency = "EUR",
                    AssetType = AssetTypeEnum.Stock
                },
                new MarketAssetDescriptor
                {
                    Symbol = "EADSY",
                    CompanyName = "Airbus SE ADR",
                    Exchange = "OTC",
                    Currency = "USD",
                    AssetType = AssetTypeEnum.Stock
                }
            ]);

        var service = BuildService(db, catalogMock);
        var results = await service.SearchAssetsAsync("airbus");

        Assert.Equal(2, results.Count);

        var airPa = results.First(r => r.Symbol == "AIR.PA");
        Assert.Equal("Airbus SE", airPa.CompanyName);
        Assert.Equal("Aerospace", airPa.Sector);
        Assert.Equal("FR0000120073", airPa.Isin);

        var adsr = results.First(r => r.Symbol == "EADSY");
        Assert.Equal("Airbus SE ADR", adsr.CompanyName);
        Assert.Null(adsr.Sector);
        Assert.Null(adsr.Isin);
    }
}
