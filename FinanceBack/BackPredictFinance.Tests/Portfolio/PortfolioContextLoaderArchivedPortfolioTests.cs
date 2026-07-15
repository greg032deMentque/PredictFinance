using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BackPredictFinance.Tests.Portfolio;

public sealed class PortfolioContextLoaderArchivedPortfolioTests
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
    public async Task TryLoadAsync_HoldingOnlyInArchivedPortfolio_DoesNotThrow_AndReconstructsContext()
    {
        var db = BuildInMemoryDb($"portfolio-context-archived-{Guid.NewGuid():N}");

        const string userId = "user-archived-1";
        const string assetId = "asset-archived-1";
        const string userAssetId = "user-asset-archived-1";
        const string portfolioId = "portfolio-archived-1";

        db.Assets.Add(new Asset
        {
            Id = assetId,
            Symbol = "AIR.PA",
            ProviderSymbol = "AIR.PA",
            Exchange = "XPAR",
            Currency = "EUR",
            AssetType = AssetTypeEnum.Stock
        });

        db.UserAssets.Add(new UserAsset
        {
            Id = userAssetId,
            UserId = userId,
            AssetId = assetId,
            Quantity = 10m
        });

        db.Portfolios.Add(new Datas.Entities.Portfolio
        {
            Id = portfolioId,
            UserId = userId,
            Name = "PEA archivé",
            PortfolioType = PortfolioTypeEnum.Pea,
            Status = PortfolioStatusEnum.Archived
        });

        db.AssetTransactions.Add(new AssetTransaction
        {
            Id = "tx-archived-1",
            UserAssetId = userAssetId,
            PortfolioId = portfolioId,
            TimestampUtc = new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc),
            TransactionType = TransactionTypeEnum.Buy,
            Quantity = 10m,
            UnitPrice = 120m,
            Fees = 5m
        });

        await db.SaveChangesAsync();

        var loader = new PortfolioContextLoader(db);

        var context = await loader.TryLoadAsync(userId, assetId);

        Assert.NotNull(context);
        Assert.True(context!.HoldsInstrument);
        Assert.Equal(10m, context.TotalQuantityHeld);
        Assert.Single(context.OpenLines);
    }

    [Fact]
    public async Task TryLoadAsync_HoldingSplitAcrossActiveAndArchivedPortfolios_ReconstructsCombinedContext()
    {
        var db = BuildInMemoryDb($"portfolio-context-mixed-{Guid.NewGuid():N}");

        const string userId = "user-mixed-1";
        const string assetId = "asset-mixed-1";
        const string userAssetId = "user-asset-mixed-1";
        const string activePortfolioId = "portfolio-active-1";
        const string archivedPortfolioId = "portfolio-archived-2";

        db.Assets.Add(new Asset
        {
            Id = assetId,
            Symbol = "OR.PA",
            ProviderSymbol = "OR.PA",
            Exchange = "XPAR",
            Currency = "EUR",
            AssetType = AssetTypeEnum.Stock
        });

        db.UserAssets.Add(new UserAsset
        {
            Id = userAssetId,
            UserId = userId,
            AssetId = assetId,
            Quantity = 15m
        });

        db.Portfolios.AddRange(
            new Datas.Entities.Portfolio { Id = activePortfolioId, UserId = userId, Name = "CTO actif", PortfolioType = PortfolioTypeEnum.CompteTitres, Status = PortfolioStatusEnum.Active },
            new Datas.Entities.Portfolio { Id = archivedPortfolioId, UserId = userId, Name = "PEA archivé", PortfolioType = PortfolioTypeEnum.Pea, Status = PortfolioStatusEnum.Archived });

        db.AssetTransactions.AddRange(
            new AssetTransaction
            {
                Id = "tx-mixed-1",
                UserAssetId = userAssetId,
                PortfolioId = archivedPortfolioId,
                TimestampUtc = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc),
                TransactionType = TransactionTypeEnum.Buy,
                Quantity = 10m,
                UnitPrice = 100m,
                Fees = 2m
            },
            new AssetTransaction
            {
                Id = "tx-mixed-2",
                UserAssetId = userAssetId,
                PortfolioId = activePortfolioId,
                TimestampUtc = new DateTime(2026, 2, 5, 9, 0, 0, DateTimeKind.Utc),
                TransactionType = TransactionTypeEnum.Buy,
                Quantity = 5m,
                UnitPrice = 110m,
                Fees = 1m
            });

        await db.SaveChangesAsync();

        var loader = new PortfolioContextLoader(db);

        var context = await loader.TryLoadAsync(userId, assetId);

        Assert.NotNull(context);
        Assert.True(context!.HoldsInstrument);
        Assert.Equal(15m, context.TotalQuantityHeld);
        Assert.Equal(2, context.OpenLines.Count);
    }
}
