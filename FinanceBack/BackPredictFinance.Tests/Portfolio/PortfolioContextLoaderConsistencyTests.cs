using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BackPredictFinance.Tests.Portfolio;

public sealed class PortfolioContextLoaderConsistencyTests
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

    [Fact]
    public async Task TryLoadAsync_SellExceedsTrackedStock_DoesNotThrow_AndFlagsWarning()
    {
        var db = BuildInMemoryDb($"portfolio-context-oversell-{Guid.NewGuid():N}");

        const string userId = "user-oversell-1";
        const string assetId = "asset-oversell-1";
        const string userAssetId = "user-asset-oversell-1";
        const string portfolioId = "portfolio-oversell-1";

        db.Assets.Add(new Asset
        {
            Id = assetId,
            Symbol = "TTE.PA",
            ProviderSymbol = "TTE.PA",
            Exchange = "XPAR",
            Currency = "EUR",
            AssetType = AssetTypeEnum.Stock
        });

        db.UserAssets.Add(new UserAsset { Id = userAssetId, UserId = userId, AssetId = assetId, Quantity = 0m });

        db.Portfolios.Add(new Datas.Entities.Portfolio
        {
            Id = portfolioId,
            UserId = userId,
            Name = "CTO",
            PortfolioType = PortfolioTypeEnum.CompteTitres,
            Status = PortfolioStatusEnum.Active
        });

        db.AssetTransactions.AddRange(
            new AssetTransaction
            {
                Id = "tx-oversell-1",
                UserAssetId = userAssetId,
                PortfolioId = portfolioId,
                TimestampUtc = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc),
                TransactionType = TransactionTypeEnum.Buy,
                Quantity = 5m,
                UnitPrice = 100m
            },
            new AssetTransaction
            {
                Id = "tx-oversell-2",
                UserAssetId = userAssetId,
                PortfolioId = portfolioId,
                TimestampUtc = new DateTime(2026, 1, 6, 9, 0, 0, DateTimeKind.Utc),
                TransactionType = TransactionTypeEnum.Sell,
                Quantity = 8m,
                UnitPrice = 110m
            });

        await db.SaveChangesAsync();

        var loader = new PortfolioContextLoader(db);
        var context = await loader.TryLoadAsync(userId, assetId);

        Assert.NotNull(context);
        Assert.False(context!.HoldsInstrument);
        Assert.True(context.HasDataIntegrityWarning);
    }

    [Fact]
    public async Task TryLoadAsync_ReconstructedQuantityMismatchesUserAsset_DoesNotThrow_AndFlagsWarning()
    {
        var db = BuildInMemoryDb($"portfolio-context-mismatch-{Guid.NewGuid():N}");

        const string userId = "user-mismatch-1";
        const string assetId = "asset-mismatch-1";
        const string userAssetId = "user-asset-mismatch-1";
        const string portfolioId = "portfolio-mismatch-1";

        db.Assets.Add(new Asset
        {
            Id = assetId,
            Symbol = "BNP.PA",
            ProviderSymbol = "BNP.PA",
            Exchange = "XPAR",
            Currency = "EUR",
            AssetType = AssetTypeEnum.Stock
        });

        db.UserAssets.Add(new UserAsset { Id = userAssetId, UserId = userId, AssetId = assetId, Quantity = 999m });

        db.Portfolios.Add(new Datas.Entities.Portfolio
        {
            Id = portfolioId,
            UserId = userId,
            Name = "CTO",
            PortfolioType = PortfolioTypeEnum.CompteTitres,
            Status = PortfolioStatusEnum.Active
        });

        db.AssetTransactions.Add(new AssetTransaction
        {
            Id = "tx-mismatch-1",
            UserAssetId = userAssetId,
            PortfolioId = portfolioId,
            TimestampUtc = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc),
            TransactionType = TransactionTypeEnum.Buy,
            Quantity = 10m,
            UnitPrice = 100m
        });

        await db.SaveChangesAsync();

        var loader = new PortfolioContextLoader(db);
        var context = await loader.TryLoadAsync(userId, assetId);

        Assert.NotNull(context);
        Assert.True(context!.HoldsInstrument);
        Assert.Equal(10m, context.TotalQuantityHeld);
        Assert.True(context.HasDataIntegrityWarning);
    }

    [Fact]
    public async Task TryLoadAsync_QuantityHeldWithoutTransactionHistory_DoesNotThrow_AndFlagsWarning()
    {
        var db = BuildInMemoryDb($"portfolio-context-notx-{Guid.NewGuid():N}");

        const string userId = "user-notx-1";
        const string assetId = "asset-notx-1";
        const string userAssetId = "user-asset-notx-1";

        db.Assets.Add(new Asset
        {
            Id = assetId,
            Symbol = "SAN.PA",
            ProviderSymbol = "SAN.PA",
            Exchange = "XPAR",
            Currency = "EUR",
            AssetType = AssetTypeEnum.Stock
        });

        db.UserAssets.Add(new UserAsset { Id = userAssetId, UserId = userId, AssetId = assetId, Quantity = 3m });

        await db.SaveChangesAsync();

        var loader = new PortfolioContextLoader(db);
        var context = await loader.TryLoadAsync(userId, assetId);

        Assert.NotNull(context);
        Assert.True(context!.HoldsInstrument);
        Assert.Null(context.AverageUnitCost);
        Assert.True(context.HasDataIntegrityWarning);
    }
}
