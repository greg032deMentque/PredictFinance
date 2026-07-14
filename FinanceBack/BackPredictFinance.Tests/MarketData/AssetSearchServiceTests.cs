using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.Patterns;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Assets;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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

    private static ClientFinanceService BuildService(FinanceDbContext db)
    {
        var assetSupportMock = new Mock<IClientFinanceAssetSupportService>(MockBehavior.Strict);
        assetSupportMock
            .Setup(x => x.MapAssetType(It.IsAny<AssetTypeEnum>()))
            .Returns((AssetTypeEnum t) => t.ToString());

        return new ClientFinanceService(
            db,
            new Mock<IAnalysisRequestCompatibilityResolver>(MockBehavior.Strict).Object,
            new Mock<IAnalysisOrchestrator>(MockBehavior.Strict).Object,
            new Mock<IAnalysisPatternRegistry>(MockBehavior.Strict).Object,
            assetSupportMock.Object,
            new Mock<IAnalysisResultProjectionService>(MockBehavior.Strict).Object);
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
}
