using BackPredictFinance.Common;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BackPredictFinance.Tests.Analysis;

public sealed class FallbackPatternMarketDataProviderTests
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

    private static FallbackPatternMarketDataProvider BuildProvider(
        IPatternMarketDataProvider innerProvider,
        FinanceDbContext db,
        IDegradedModeState degradedModeState,
        int degradedModeMaxSnapshotAgeHours = 48)
    {
        var options = Options.Create(new MarketDataOptions
        {
            DegradedModeMaxSnapshotAgeHours = degradedModeMaxSnapshotAgeHours
        });

        return new FallbackPatternMarketDataProvider(
            innerProvider,
            db,
            options,
            degradedModeState,
            NullLogger<FallbackPatternMarketDataProvider>.Instance);
    }

    private static async Task<Asset> SeedAssetWithSnapshotAsync(FinanceDbContext db, string symbol, DateTime candleTimestampUtc)
    {
        var asset = new Asset
        {
            Symbol = symbol,
            ProviderSymbol = symbol,
            Exchange = "XPAR",
            Currency = "EUR",
            AssetType = AssetTypeEnum.Stock
        };
        db.Assets.Add(asset);

        db.AssetCandleSnapshots.Add(new AssetCandleSnapshot
        {
            AssetId = asset.Id,
            Interval = "1d",
            TimestampUtc = candleTimestampUtc,
            Open = 100m,
            High = 105m,
            Low = 99m,
            Close = 103m,
            Volume = 1_000m,
            Source = "TEST"
        });

        await db.SaveChangesAsync();
        return asset;
    }

    [Fact]
    public async Task GetTimeSeriesAsync_ProviderFails_RecentSnapshotAvailable_FallsBackAndMarksDegraded()
    {
        using var db = BuildInMemoryDb($"fallback-fresh-{Guid.NewGuid():N}");
        await SeedAssetWithSnapshotAsync(db, "AIR.PA", DateTime.UtcNow.AddHours(-2));

        var innerProviderMock = new Mock<IPatternMarketDataProvider>(MockBehavior.Strict);
        innerProviderMock
            .Setup(x => x.GetTimeSeriesAsync("AIR.PA", "1d", 30, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Yahoo indisponible"));

        var degradedModeState = new DegradedModeState();
        var provider = BuildProvider(innerProviderMock.Object, db, degradedModeState);

        var result = await provider.GetTimeSeriesAsync("AIR.PA", "1d", 30);

        Assert.NotEmpty(result.Candles);
        Assert.Equal(FreshnessStatusEnum.Fresh, degradedModeState.Status);
        Assert.NotNull(degradedModeState.CheckedAtUtc);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_ProviderFails_NoSnapshotAvailable_RethrowsOriginalException()
    {
        using var db = BuildInMemoryDb($"fallback-nosnapshot-{Guid.NewGuid():N}");
        db.Assets.Add(new Asset
        {
            Symbol = "NAKED.PA",
            ProviderSymbol = "NAKED.PA",
            Exchange = "XPAR",
            Currency = "EUR",
            AssetType = AssetTypeEnum.Stock
        });
        await db.SaveChangesAsync();

        var innerProviderMock = new Mock<IPatternMarketDataProvider>(MockBehavior.Strict);
        innerProviderMock
            .Setup(x => x.GetTimeSeriesAsync("NAKED.PA", "1d", 30, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Yahoo indisponible"));

        var degradedModeState = new DegradedModeState();
        var provider = BuildProvider(innerProviderMock.Object, db, degradedModeState);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => provider.GetTimeSeriesAsync("NAKED.PA", "1d", 30));

        Assert.Equal(FreshnessStatusEnum.Fresh, degradedModeState.Status);
        Assert.Null(degradedModeState.CheckedAtUtc);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_ProviderFails_SnapshotTooOld_RethrowsOriginalException()
    {
        using var db = BuildInMemoryDb($"fallback-stale-{Guid.NewGuid():N}");
        await SeedAssetWithSnapshotAsync(db, "OLD.PA", DateTime.UtcNow.AddHours(-72));

        var innerProviderMock = new Mock<IPatternMarketDataProvider>(MockBehavior.Strict);
        innerProviderMock
            .Setup(x => x.GetTimeSeriesAsync("OLD.PA", "1d", 30, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Yahoo indisponible"));

        var degradedModeState = new DegradedModeState();
        var provider = BuildProvider(innerProviderMock.Object, db, degradedModeState, degradedModeMaxSnapshotAgeHours: 48);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => provider.GetTimeSeriesAsync("OLD.PA", "1d", 30));

        Assert.Equal(FreshnessStatusEnum.Fresh, degradedModeState.Status);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_ProviderSucceeds_DoesNotTouchDegradedModeState()
    {
        using var db = BuildInMemoryDb($"fallback-healthy-{Guid.NewGuid():N}");

        var candles = new List<TickerCandle>
        {
            new() { Date = DateTime.UtcNow, Open = 1m, High = 2m, Low = 0.5m, Close = 1.5m, Volume = 100m }
        };

        var innerProviderMock = new Mock<IPatternMarketDataProvider>(MockBehavior.Strict);
        innerProviderMock
            .Setup(x => x.GetTimeSeriesAsync("OK.PA", "1d", 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TickerTimeSeriesResponse { Symbol = "OK.PA", Interval = "1d", OutputSize = 1, Candles = candles });

        var degradedModeState = new DegradedModeState();
        var provider = BuildProvider(innerProviderMock.Object, db, degradedModeState);

        var result = await provider.GetTimeSeriesAsync("OK.PA", "1d", 30);

        Assert.Single(result.Candles);
        Assert.Equal(FreshnessStatusEnum.Fresh, degradedModeState.Status);
        Assert.Null(degradedModeState.CheckedAtUtc);
    }
}
