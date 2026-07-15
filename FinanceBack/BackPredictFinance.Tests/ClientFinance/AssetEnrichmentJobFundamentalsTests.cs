using BackPredictFinance.Common;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.BackgroundJobs;
using BackPredictFinance.Services.TwelveDataServices;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BackPredictFinance.Tests.ClientFinance;

public sealed class AssetEnrichmentJobFundamentalsTests
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

    private static Asset BuildAsset(string assetId) => new()
    {
        Id = assetId,
        Symbol = assetId,
        ProviderSymbol = assetId,
        Exchange = "XPAR",
        Currency = "EUR",
        AssetType = AssetTypeEnum.Stock
    };

    private static AssetEnrichmentJob BuildJob()
    {
        var scopeFactoryMock = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
        var optionsMock = Options.Create(new MarketDataOptions());
        return new AssetEnrichmentJob(scopeFactoryMock.Object, NullLogger<AssetEnrichmentJob>.Instance, optionsMock);
    }

    [Fact]
    public async Task PersistFundamentalsSnapshotAsync_ValidData_PersistsSnapshot()
    {
        var db = BuildInMemoryDb($"fund-valid-{Guid.NewGuid():N}");
        var assetId = "asset-1";
        db.Assets.Add(BuildAsset(assetId));
        await db.SaveChangesAsync();

        var providerMock = new Mock<IFundamentalsProvider>();
        providerMock.Setup(p => p.GetFundamentalsAsync("asset-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MarketFundamentalData
            {
                MarketCap = 1_000_000m,
                TrailingPe = 15.5m,
                DividendYield = 2.1m,
                ProviderId = "YAHOO_FINANCE"
            });

        var job = BuildJob();
        await job.PersistFundamentalsSnapshotAsync(db, providerMock.Object, (assetId, "asset-1"), CancellationToken.None);

        var snapshot = Assert.Single(db.AssetFundamentalsSnapshots);
        Assert.Equal(assetId, snapshot.AssetId);
        Assert.Equal(1_000_000m, snapshot.MarketCap);
        Assert.Equal(15.5m, snapshot.TrailingPE);
        Assert.Equal(2.1m, snapshot.DividendYield);
        Assert.Equal("YAHOO_FINANCE", snapshot.Source);
    }

    [Fact]
    public async Task PersistFundamentalsSnapshotAsync_MarketCapMissing_RejectsSnapshot()
    {
        var db = BuildInMemoryDb($"fund-nomcap-{Guid.NewGuid():N}");
        var assetId = "asset-2";
        db.Assets.Add(BuildAsset(assetId));
        await db.SaveChangesAsync();

        var providerMock = new Mock<IFundamentalsProvider>();
        providerMock.Setup(p => p.GetFundamentalsAsync("asset-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MarketFundamentalData
            {
                MarketCap = null,
                TrailingPe = 15.5m,
                DividendYield = 2.1m,
                ProviderId = "YAHOO_FINANCE"
            });

        var job = BuildJob();
        await job.PersistFundamentalsSnapshotAsync(db, providerMock.Object, (assetId, "asset-2"), CancellationToken.None);

        Assert.Empty(db.AssetFundamentalsSnapshots);
    }

    [Fact]
    public async Task PersistFundamentalsSnapshotAsync_MarketCapNegativeOrZero_RejectsSnapshot()
    {
        var db = BuildInMemoryDb($"fund-negmcap-{Guid.NewGuid():N}");
        var assetId = "asset-3";
        db.Assets.Add(BuildAsset(assetId));
        await db.SaveChangesAsync();

        var providerMock = new Mock<IFundamentalsProvider>();
        providerMock.Setup(p => p.GetFundamentalsAsync("asset-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MarketFundamentalData
            {
                MarketCap = 0m,
                TrailingPe = 15.5m,
                DividendYield = 2.1m,
                ProviderId = "YAHOO_FINANCE"
            });

        var job = BuildJob();
        await job.PersistFundamentalsSnapshotAsync(db, providerMock.Object, (assetId, "asset-3"), CancellationToken.None);

        Assert.Empty(db.AssetFundamentalsSnapshots);
    }

    [Fact]
    public async Task PersistFundamentalsSnapshotAsync_PeNegativeOrZero_PersistsSnapshotWithoutPe()
    {
        var db = BuildInMemoryDb($"fund-negpe-{Guid.NewGuid():N}");
        var assetId = "asset-4";
        db.Assets.Add(BuildAsset(assetId));
        await db.SaveChangesAsync();

        var providerMock = new Mock<IFundamentalsProvider>();
        providerMock.Setup(p => p.GetFundamentalsAsync("asset-4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MarketFundamentalData
            {
                MarketCap = 500_000m,
                TrailingPe = -3m,
                DividendYield = 1.2m,
                ProviderId = "YAHOO_FINANCE"
            });

        var job = BuildJob();
        await job.PersistFundamentalsSnapshotAsync(db, providerMock.Object, (assetId, "asset-4"), CancellationToken.None);

        var snapshot = Assert.Single(db.AssetFundamentalsSnapshots);
        Assert.Equal(500_000m, snapshot.MarketCap);
        Assert.Null(snapshot.TrailingPE);
        Assert.Equal(1.2m, snapshot.DividendYield);
    }

    [Fact]
    public async Task PersistFundamentalsSnapshotAsync_ProviderThrows_DoesNotPersistAndDoesNotThrow()
    {
        var db = BuildInMemoryDb($"fund-throws-{Guid.NewGuid():N}");
        var assetId = "asset-5";
        db.Assets.Add(BuildAsset(assetId));
        await db.SaveChangesAsync();

        var providerMock = new Mock<IFundamentalsProvider>();
        providerMock.Setup(p => p.GetFundamentalsAsync("asset-5", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("provider unavailable"));

        var job = BuildJob();
        await job.PersistFundamentalsSnapshotAsync(db, providerMock.Object, (assetId, "asset-5"), CancellationToken.None);

        Assert.Empty(db.AssetFundamentalsSnapshots);
    }

    [Fact]
    public async Task PersistFundamentalsSnapshotAsync_CalledTwice_InsertsTwoDistinctSnapshots()
    {
        var db = BuildInMemoryDb($"fund-upsert-{Guid.NewGuid():N}");
        var assetId = "asset-6";
        db.Assets.Add(BuildAsset(assetId));
        await db.SaveChangesAsync();

        var providerMock = new Mock<IFundamentalsProvider>();
        providerMock.SetupSequence(p => p.GetFundamentalsAsync("asset-6", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MarketFundamentalData { MarketCap = 100_000m, TrailingPe = 10m, ProviderId = "YAHOO_FINANCE" })
            .ReturnsAsync(new MarketFundamentalData { MarketCap = 110_000m, TrailingPe = 11m, ProviderId = "YAHOO_FINANCE" });

        var job = BuildJob();
        await job.PersistFundamentalsSnapshotAsync(db, providerMock.Object, (assetId, "asset-6"), CancellationToken.None);
        await job.PersistFundamentalsSnapshotAsync(db, providerMock.Object, (assetId, "asset-6"), CancellationToken.None);

        Assert.Equal(2, await db.AssetFundamentalsSnapshots.CountAsync());
    }
}
