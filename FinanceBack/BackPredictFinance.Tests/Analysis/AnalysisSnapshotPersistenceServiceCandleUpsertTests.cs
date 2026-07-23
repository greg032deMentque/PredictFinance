using AutoMapper;
using BackPredictFinance.Common.Localization;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.ViewModels.UserViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BackPredictFinance.Tests.Analysis;

public sealed class AnalysisSnapshotPersistenceServiceCandleUpsertTests
{
    private static async Task<(AnalysisSnapshotPersistenceService Service, FinanceDbContext Db, IAsyncDisposable Scope)> BuildContextAsync()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var db = new FinanceDbContext(options, httpContextAccessorMock.Object);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(httpContextAccessorMock.Object);
        services.AddSingleton(db);
        services.AddSingleton<IMapper>(new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(UserViewModel).Assembly);
        }, NullLoggerFactory.Instance).CreateMapper());
        services.AddSingleton(Mock.Of<IStringLocalizer<SharedResources>>());
        services.AddScoped<ILogService>(_ => Mock.Of<ILogService>());
        services.AddMemoryCache();

        services
            .AddIdentity<User, IdentityRole>()
            .AddEntityFrameworkStores<FinanceDbContext>()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateAsyncScope();

        var providerMock = new Mock<IFundamentalsProvider>();
        var service = new AnalysisSnapshotPersistenceService(scope.ServiceProvider, providerMock.Object);

        return (service, db, scope);
    }

    // Regression DB-03 : l'ancienne implémentation appelait ChangeTracker.Clear() en cas de conflit
    // d'unicité sur une bougie déjà présente en base (course avec le job de rafraîchissement), ce qui
    // détachait toute entité non sauvegardée du même contexte (dont une AnalysisRun en attente). Le
    // nouvel upsert charge les bougies existantes en un seul SELECT et met à jour en mémoire sans
    // jamais réinsérer ni vider le tracker : une bougie déjà présente doit être mise à jour, pas dupliquée,
    // et une bougie absente doit être insérée, le tout en un seul SaveChangesAsync.
    [Fact]
    public async Task UpsertCandlesForRefreshAsync_ExistingAndNewCandle_UpdatesExistingWithoutDuplicationAndInsertsNew()
    {
        var (service, db, scope) = await BuildContextAsync();
        await using var _ = scope;

        var asset = new Asset { Symbol = "AIR.PA", ProviderSymbol = "AIR.PA", Name = "Airbus", Currency = "EUR" };
        db.Assets.Add(asset);

        var conflictDate = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
        db.AssetCandleSnapshots.Add(new AssetCandleSnapshot
        {
            AssetId = asset.Id,
            Interval = "1d",
            TimestampUtc = conflictDate,
            Open = 100m,
            High = 101m,
            Low = 99m,
            Close = 100.5m,
            Volume = 1000m,
            Source = "PIPELINE"
        });
        await db.SaveChangesAsync();

        var newDate = conflictDate.AddDays(1);
        var candles = new List<TickerCandle>
        {
            // Même clé (AssetId, Interval, TimestampUtc) que la ligne déjà en base : doit être mise à jour.
            new() { Date = conflictDate, Open = 200m, High = 210m, Low = 195m, Close = 205m, Volume = 5000m },
            // Nouvelle clé : doit être insérée.
            new() { Date = newDate, Open = 205m, High = 208m, Low = 203m, Close = 206m, Volume = 3000m }
        };

        await service.UpsertCandlesForRefreshAsync(asset.Id, candles, CancellationToken.None);

        var snapshots = await db.AssetCandleSnapshots
            .Where(x => x.AssetId == asset.Id)
            .OrderBy(x => x.TimestampUtc)
            .ToListAsync();

        Assert.Equal(2, snapshots.Count);

        var updated = snapshots[0];
        Assert.Equal(conflictDate, updated.TimestampUtc);
        Assert.Equal(205m, updated.Close);
        Assert.Equal(5000m, updated.Volume);

        var inserted = snapshots[1];
        Assert.Equal(newDate, inserted.TimestampUtc);
        Assert.Equal(206m, inserted.Close);
    }
}
