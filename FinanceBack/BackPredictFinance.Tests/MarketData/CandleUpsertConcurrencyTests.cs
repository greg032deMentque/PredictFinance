using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BackPredictFinance.Tests.MarketData;

public sealed class CandleUpsertConcurrencyTests
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

    [Fact]
    public async Task UpsertCandles_WhenCandleAlreadyExists_UpdatesOhlcv()
    {
        var dbName = $"upsert-existing-{Guid.NewGuid():N}";
        var db = BuildInMemoryDb(dbName);
        var assetId = "asset-upsert-1";

        db.Assets.Add(BuildAsset(assetId));

        var existingTimestamp = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        db.AssetCandleSnapshots.Add(new AssetCandleSnapshot
        {
            AssetId = assetId,
            Interval = "1d",
            TimestampUtc = existingTimestamp,
            Open = 100m, High = 110m, Low = 95m, Close = 105m, Volume = 1000m,
            Source = "PIPELINE"
        });
        await db.SaveChangesAsync();

        var updatedCandle = new TickerCandle
        {
            Date = existingTimestamp,
            Open = 106m, High = 115m, Low = 104m, Close = 112m, Volume = 2000m
        };

        await UpsertCandlesAsync(db, assetId, [updatedCandle]);

        var stored = await db.AssetCandleSnapshots
            .AsNoTracking()
            .Where(c => c.AssetId == assetId && c.Interval == "1d" && c.TimestampUtc == existingTimestamp)
            .ToListAsync();

        Assert.Single(stored);
        Assert.Equal(112m, stored[0].Close);
        Assert.Equal(2000m, stored[0].Volume);
    }

    [Fact]
    public async Task UpsertCandles_WhenCandleIsNew_InsertsRow()
    {
        var dbName = $"upsert-new-{Guid.NewGuid():N}";
        var db = BuildInMemoryDb(dbName);
        var assetId = "asset-upsert-2";

        db.Assets.Add(BuildAsset(assetId));
        await db.SaveChangesAsync();

        var newTimestamp = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);
        var newCandle = new TickerCandle
        {
            Date = newTimestamp,
            Open = 130m, High = 140m, Low = 128m, Close = 138m, Volume = 5000m
        };

        await UpsertCandlesAsync(db, assetId, [newCandle]);

        var stored = await db.AssetCandleSnapshots
            .AsNoTracking()
            .Where(c => c.AssetId == assetId && c.Interval == "1d" && c.TimestampUtc == newTimestamp)
            .ToListAsync();

        Assert.Single(stored);
        Assert.Equal(138m, stored[0].Close);
    }

    [Fact]
    public async Task UpsertCandles_WhenMultipleCandlesSameAsset_NoDuplicate()
    {
        var dbName = $"upsert-multi-{Guid.NewGuid():N}";
        var db = BuildInMemoryDb(dbName);
        var assetId = "asset-upsert-3";

        db.Assets.Add(BuildAsset(assetId));
        await db.SaveChangesAsync();

        var t1 = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc);

        var candles = new List<TickerCandle>
        {
            new() { Date = t1, Open = 100m, High = 110m, Low = 98m, Close = 105m, Volume = 1000m },
            new() { Date = t2, Open = 105m, High = 115m, Low = 103m, Close = 112m, Volume = 1200m }
        };

        await UpsertCandlesAsync(db, assetId, candles);

        var stored = await db.AssetCandleSnapshots
            .AsNoTracking()
            .Where(c => c.AssetId == assetId && c.Interval == "1d")
            .ToListAsync();

        Assert.Equal(2, stored.Count);
    }

    [Fact]
    public async Task UpsertCandles_WhenSameCandleUpsertedTwice_ResultIsIdempotent()
    {
        var dbName = $"upsert-idempotent-{Guid.NewGuid():N}";
        var db = BuildInMemoryDb(dbName);
        var assetId = "asset-upsert-4";

        db.Assets.Add(BuildAsset(assetId));
        await db.SaveChangesAsync();

        var timestamp = new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc);
        var candle = new TickerCandle
        {
            Date = timestamp,
            Open = 120m, High = 125m, Low = 118m, Close = 122m, Volume = 3000m
        };

        await UpsertCandlesAsync(db, assetId, [candle]);
        await UpsertCandlesAsync(db, assetId, [candle]);

        var stored = await db.AssetCandleSnapshots
            .AsNoTracking()
            .Where(c => c.AssetId == assetId && c.Interval == "1d" && c.TimestampUtc == timestamp)
            .ToListAsync();

        Assert.Single(stored);
        Assert.Equal(122m, stored[0].Close);
    }

    private static async Task UpsertCandlesAsync(
        FinanceDbContext db,
        string assetId,
        IReadOnlyList<TickerCandle> candles,
        CancellationToken ct = default)
    {
        foreach (var candle in candles)
        {
            var candleTimestampUtc = candle.Date.Kind switch
            {
                DateTimeKind.Utc => candle.Date,
                DateTimeKind.Unspecified => DateTime.SpecifyKind(candle.Date, DateTimeKind.Utc),
                _ => candle.Date.ToUniversalTime()
            };

            var existing = await db.AssetCandleSnapshots
                .FirstOrDefaultAsync(
                    x => x.AssetId == assetId
                        && x.Interval == "1d"
                        && x.TimestampUtc == candleTimestampUtc,
                    ct);

            if (existing is null)
            {
                db.AssetCandleSnapshots.Add(new AssetCandleSnapshot
                {
                    AssetId = assetId,
                    Interval = "1d",
                    TimestampUtc = candleTimestampUtc,
                    Open = candle.Open,
                    High = candle.High,
                    Low = candle.Low,
                    Close = candle.Close,
                    Volume = candle.Volume,
                    Source = "PIPELINE"
                });

                try
                {
                    await db.SaveChangesAsync(ct);
                }
                catch (DbUpdateException)
                {
                    db.ChangeTracker.Clear();

                    var conflicted = await db.AssetCandleSnapshots
                        .FirstOrDefaultAsync(
                            x => x.AssetId == assetId
                                && x.Interval == "1d"
                                && x.TimestampUtc == candleTimestampUtc,
                            ct);

                    if (conflicted is not null)
                    {
                        conflicted.Open = candle.Open;
                        conflicted.High = candle.High;
                        conflicted.Low = candle.Low;
                        conflicted.Close = candle.Close;
                        conflicted.Volume = candle.Volume;
                        await db.SaveChangesAsync(ct);
                    }
                }
            }
            else
            {
                existing.Open = candle.Open;
                existing.High = candle.High;
                existing.Low = candle.Low;
                existing.Close = candle.Close;
                existing.Volume = candle.Volume;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
