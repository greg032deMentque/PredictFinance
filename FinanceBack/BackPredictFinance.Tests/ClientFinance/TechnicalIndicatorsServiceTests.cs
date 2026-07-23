using AutoMapper;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.Localization;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services;
using BackPredictFinance.Services.ClientFinanceServices.Indicators;
using BackPredictFinance.ViewModels.UserViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BackPredictFinance.Tests.ClientFinance;

public sealed class TechnicalIndicatorsServiceTests
{
    private static async Task<(FinanceDbContext Db, TechnicalIndicatorsService Service, IAsyncDisposable Scope)> BuildContextAsync()
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

        var service = new TechnicalIndicatorsService(scope.ServiceProvider);

        return (db, service, scope);
    }

    private static Asset BuildAsset(string id, string symbol) => new()
    {
        Id = id,
        Symbol = symbol,
        ProviderSymbol = symbol,
        Exchange = "XPAR",
        Currency = "EUR",
        AssetType = AssetTypeEnum.Stock
    };

    private static void SeedHistories(FinanceDbContext db, string assetId, int count)
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < count; i++)
        {
            var price = 100m + (decimal)Math.Round(Math.Sin(i) * 5, 4);
            db.PriceHistories.Add(new PriceHistory
            {
                AssetId = assetId,
                RetrievedAtUtc = start.AddDays(i),
                Price = price,
                Volume = 1000m + i
            });
        }
    }

    [Fact]
    public async Task GetIndicatorsAsync_SinglePricePoint_ReturnsNullIndicatorsWithoutComputing()
    {
        var (db, service, scope) = await BuildContextAsync();
        await using var _ = scope;

        var asset = BuildAsset("a1", "AIR.PA");
        db.Assets.Add(asset);
        SeedHistories(db, asset.Id, 1);
        await db.SaveChangesAsync();

        var result = await service.GetIndicatorsAsync("AIR.PA", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result!.DataPointsUsed);
        Assert.Null(result.Rsi);
        Assert.Null(result.Macd);
    }

    [Theory]
    [InlineData(8, false, false)]
    [InlineData(14, false, false)]
    [InlineData(34, true, false)]
    [InlineData(35, true, true)]
    public async Task GetIndicatorsAsync_RespectsRsiAndMacdSufficiencyThresholds(
        int dataPoints, bool expectRsi, bool expectMacd)
    {
        var (db, service, scope) = await BuildContextAsync();
        await using var _ = scope;

        var asset = BuildAsset($"a-{dataPoints}", $"SYM{dataPoints}.PA");
        db.Assets.Add(asset);
        SeedHistories(db, asset.Id, dataPoints);
        await db.SaveChangesAsync();

        var result = await service.GetIndicatorsAsync(asset.Symbol, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(dataPoints, result!.DataPointsUsed);
        Assert.Equal(expectRsi, result.Rsi != null);
        Assert.Equal(expectMacd, result.Macd != null);
    }
}
