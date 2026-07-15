using AutoMapper;
using BackPredictFinance.Common.Localization;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services;
using BackPredictFinance.Services.ClientFinanceServices.Screener;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Screener;
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

public sealed class ScreenerServiceTests
{
    private static async Task<(FinanceDbContext Db, ScreenerService Service, IAsyncDisposable Scope)> BuildContextAsync()
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

        var service = new ScreenerService(scope.ServiceProvider);

        return (db, service, scope);
    }

    private static Asset BuildAsset(
        string id,
        string symbol,
        string? name,
        string exchange,
        string? country,
        string? sector,
        AssetTypeEnum assetType) => new()
        {
            Id = id,
            Symbol = symbol,
            ProviderSymbol = symbol,
            Name = name,
            Exchange = exchange,
            Currency = "EUR",
            Country = country,
            Sector = sector,
            AssetType = assetType
        };

    private static AssetQuoteSnapshot BuildSnapshot(string assetId, decimal lastPrice, decimal dayVariationPct, DateTime asOfUtc) => new()
    {
        Id = Guid.NewGuid().ToString(),
        AssetId = assetId,
        AsOfUtc = asOfUtc,
        LastPrice = lastPrice,
        DayVariationPct = dayVariationPct,
        Source = "test"
    };

    private static AssetPeaEligibility BuildEligibility(string assetId, PeaEligibilityStatusEnum status) => new()
    {
        Id = Guid.NewGuid().ToString(),
        AssetId = assetId,
        EligibilityStatus = status,
        SourceType = PeaEligibilitySourceTypeEnum.ManualRegistry,
        UniverseId = "PEA_FR_EQUITIES"
    };

    private static AssetFundamentalsSnapshot BuildFundamentals(
        string assetId, decimal? marketCap, decimal? trailingPe, decimal? dividendYield, DateTime asOfUtc) => new()
    {
        Id = Guid.NewGuid().ToString(),
        AssetId = assetId,
        AsOfUtc = asOfUtc,
        MarketCap = marketCap,
        TrailingPE = trailingPe,
        DividendYield = dividendYield,
        Source = "test"
    };

    private static User BuildUser(string userId) => new()
    {
        Id = userId,
        UserName = $"{userId}@test.local",
        Email = $"{userId}@test.local",
        NormalizedEmail = $"{userId}@test.local".ToUpperInvariant(),
        NormalizedUserName = $"{userId}@test.local".ToUpperInvariant()
    };

    private static async Task<(FinanceDbContext Db, ScreenerService Service, IAsyncDisposable Scope)> BuildContextForUserAsync(string userId)
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var httpContext = new DefaultHttpContext();
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
        [
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId)
        ], "test"));

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
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

        var service = new ScreenerService(scope.ServiceProvider);

        return (db, service, scope);
    }

    [Fact]
    public async Task GetPagedAsync_CumulatesAllFilters()
    {
        var (db, service, scope) = await BuildContextAsync();
        await using var _ = scope;

        var match = BuildAsset("a1", "AIR.PA", "Airbus", "XPAR", "France", "Aerospace", AssetTypeEnum.Stock);
        var wrongSector = BuildAsset("a2", "MC.PA", "LVMH", "XPAR", "France", "Luxury", AssetTypeEnum.Stock);
        var wrongCountry = BuildAsset("a3", "AAPL", "Apple", "NASDAQ", "United States", "Aerospace", AssetTypeEnum.Stock);
        var wrongType = BuildAsset("a4", "BTC-USD", "Bitcoin", "CRYPTO", "France", "Aerospace", AssetTypeEnum.Crypto);
        var wrongSearch = BuildAsset("a5", "SAF.PA", "Safran", "XPAR", "France", "Aerospace", AssetTypeEnum.Stock);
        var notPea = BuildAsset("a6", "AIB.PA", "Airbus Bis", "XPAR", "France", "Aerospace", AssetTypeEnum.Stock);

        db.Assets.AddRange(match, wrongSector, wrongCountry, wrongType, wrongSearch, notPea);
        db.AssetPeaEligibilities.Add(BuildEligibility("a1", PeaEligibilityStatusEnum.ConfirmedEligible));
        db.AssetPeaEligibilities.Add(BuildEligibility("a6", PeaEligibilityStatusEnum.ConfirmedIneligible));
        await db.SaveChangesAsync();

        var result = await service.GetPagedAsync(new ScreenerQueryViewModel
        {
            Sectors = ["Aerospace"],
            Countries = ["France"],
            AssetType = (int)AssetTypeEnum.Stock,
            PeaOnly = true,
            Search = "air"
        });

        Assert.Single(result.Items);
        Assert.Equal("AIR.PA", result.Items[0].Symbol);
        Assert.True(result.Items[0].IsPeaEligible);
    }

    [Fact]
    public async Task GetPagedAsync_InvalidSortBy_FallsBackToSymbol()
    {
        var (db, service, scope) = await BuildContextAsync();
        await using var _ = scope;

        db.Assets.Add(BuildAsset("a1", "MC.PA", "LVMH", "XPAR", "France", "Luxury", AssetTypeEnum.Stock));
        db.Assets.Add(BuildAsset("a2", "AIR.PA", "Airbus", "XPAR", "France", "Aerospace", AssetTypeEnum.Stock));
        await db.SaveChangesAsync();

        var result = await service.GetPagedAsync(new ScreenerQueryViewModel
        {
            SortBy = "NotAWhitelistedColumn"
        });

        Assert.Equal("AIR.PA", result.Items[0].Symbol);
        Assert.Equal("MC.PA", result.Items[1].Symbol);
    }

    [Fact]
    public async Task GetPagedAsync_InvalidSortDirection_FallsBackToAsc()
    {
        var (db, service, scope) = await BuildContextAsync();
        await using var _ = scope;

        db.Assets.Add(BuildAsset("a1", "MC.PA", "LVMH", "XPAR", "France", "Luxury", AssetTypeEnum.Stock));
        db.Assets.Add(BuildAsset("a2", "AIR.PA", "Airbus", "XPAR", "France", "Aerospace", AssetTypeEnum.Stock));
        await db.SaveChangesAsync();

        var result = await service.GetPagedAsync(new ScreenerQueryViewModel
        {
            SortBy = "Symbol",
            SortDirection = "descending"
        });

        Assert.Equal("AIR.PA", result.Items[0].Symbol);
        Assert.Equal("MC.PA", result.Items[1].Symbol);
    }

    [Fact]
    public async Task GetPagedAsync_ProjectsLatestQuoteAndPeaFlag()
    {
        var (db, service, scope) = await BuildContextAsync();
        await using var _ = scope;

        var withQuote = BuildAsset("a1", "AIR.PA", "Airbus", "XPAR", "France", "Aerospace", AssetTypeEnum.Stock);
        var withoutQuote = BuildAsset("a2", "MC.PA", "LVMH", "XPAR", "France", "Luxury", AssetTypeEnum.Stock);

        db.Assets.AddRange(withQuote, withoutQuote);
        db.AssetQuoteSnapshots.Add(BuildSnapshot("a1", 100m, 1.5m, new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc)));
        db.AssetQuoteSnapshots.Add(BuildSnapshot("a1", 150m, 2.5m, new DateTime(2026, 6, 2, 8, 0, 0, DateTimeKind.Utc)));
        db.AssetPeaEligibilities.Add(BuildEligibility("a1", PeaEligibilityStatusEnum.ConfirmedEligible));
        await db.SaveChangesAsync();

        var result = await service.GetPagedAsync(new ScreenerQueryViewModel { SortBy = "Symbol" });

        var air = result.Items.Single(i => i.Symbol == "AIR.PA");
        Assert.Equal(150m, air.LastPrice);
        Assert.Equal(2.5m, air.DayVariationPct);
        Assert.Equal(new DateTime(2026, 6, 2, 8, 0, 0, DateTimeKind.Utc), air.QuoteAsOfUtc);
        Assert.True(air.IsPeaEligible);

        var mc = result.Items.Single(i => i.Symbol == "MC.PA");
        Assert.Null(mc.LastPrice);
        Assert.Null(mc.DayVariationPct);
        Assert.Null(mc.QuoteAsOfUtc);
        Assert.False(mc.IsPeaEligible);
    }

    [Fact]
    public async Task GetPagedAsync_ClampsPageAndPageSizeBounds()
    {
        var (db, service, scope) = await BuildContextAsync();
        await using var _ = scope;

        for (var i = 0; i < 5; i++)
        {
            db.Assets.Add(BuildAsset($"a{i}", $"SYM{i}.PA", $"Name {i}", "XPAR", "France", "Sector", AssetTypeEnum.Stock));
        }
        await db.SaveChangesAsync();

        var negativePage = await service.GetPagedAsync(new ScreenerQueryViewModel { Page = -5, PageSize = 2 });
        Assert.Equal(1, negativePage.Page);

        var oversizedPage = await service.GetPagedAsync(new ScreenerQueryViewModel { Page = 1, PageSize = 5000 });
        Assert.Equal(100, oversizedPage.PageSize);

        var undersizedPage = await service.GetPagedAsync(new ScreenerQueryViewModel { Page = 1, PageSize = 0 });
        Assert.Equal(1, undersizedPage.PageSize);
    }

    [Fact]
    public async Task ExportCsvAsync_CapsAtFiveThousandRows()
    {
        var (db, service, scope) = await BuildContextAsync();
        await using var _ = scope;

        for (var i = 0; i < 20; i++)
        {
            db.Assets.Add(BuildAsset($"a{i}", $"SYM{i}.PA", $"Name {i}", "XPAR", "France", "Sector", AssetTypeEnum.Stock));
        }
        await db.SaveChangesAsync();

        var csvBytes = await service.ExportCsvAsync(new ScreenerQueryViewModel());
        var csvText = System.Text.Encoding.UTF8.GetString(csvBytes);
        var lineCount = csvText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;

        Assert.Equal(21, lineCount);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByMinAndMaxPE()
    {
        var (db, service, scope) = await BuildContextAsync();
        await using var _ = scope;

        var cheap = BuildAsset("a1", "CHEAP.PA", "Cheap", "XPAR", "France", "Sector", AssetTypeEnum.Stock);
        var mid = BuildAsset("a2", "MID.PA", "Mid", "XPAR", "France", "Sector", AssetTypeEnum.Stock);
        var expensive = BuildAsset("a3", "EXP.PA", "Expensive", "XPAR", "France", "Sector", AssetTypeEnum.Stock);
        var noFundamentals = BuildAsset("a4", "NOFUND.PA", "NoFundamentals", "XPAR", "France", "Sector", AssetTypeEnum.Stock);

        db.Assets.AddRange(cheap, mid, expensive, noFundamentals);
        db.AssetFundamentalsSnapshots.Add(BuildFundamentals("a1", 100_000m, 5m, 1m, DateTime.UtcNow));
        db.AssetFundamentalsSnapshots.Add(BuildFundamentals("a2", 100_000m, 15m, 1m, DateTime.UtcNow));
        db.AssetFundamentalsSnapshots.Add(BuildFundamentals("a3", 100_000m, 40m, 1m, DateTime.UtcNow));
        await db.SaveChangesAsync();

        var result = await service.GetPagedAsync(new ScreenerQueryViewModel { MinPE = 10m, MaxPE = 20m });

        Assert.Single(result.Items);
        Assert.Equal("MID.PA", result.Items[0].Symbol);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByMinDividendYieldAndMinMarketCap()
    {
        var (db, service, scope) = await BuildContextAsync();
        await using var _ = scope;

        var match = BuildAsset("a1", "MATCH.PA", "Match", "XPAR", "France", "Sector", AssetTypeEnum.Stock);
        var lowYield = BuildAsset("a2", "LOWY.PA", "LowYield", "XPAR", "France", "Sector", AssetTypeEnum.Stock);
        var lowCap = BuildAsset("a3", "LOWC.PA", "LowCap", "XPAR", "France", "Sector", AssetTypeEnum.Stock);

        db.Assets.AddRange(match, lowYield, lowCap);
        db.AssetFundamentalsSnapshots.Add(BuildFundamentals("a1", 1_000_000m, 12m, 3m, DateTime.UtcNow));
        db.AssetFundamentalsSnapshots.Add(BuildFundamentals("a2", 1_000_000m, 12m, 0.5m, DateTime.UtcNow));
        db.AssetFundamentalsSnapshots.Add(BuildFundamentals("a3", 10_000m, 12m, 3m, DateTime.UtcNow));
        await db.SaveChangesAsync();

        var result = await service.GetPagedAsync(new ScreenerQueryViewModel { MinDividendYield = 2m, MinMarketCap = 500_000m });

        Assert.Single(result.Items);
        Assert.Equal("MATCH.PA", result.Items[0].Symbol);
    }

    [Fact]
    public async Task GetPagedAsync_ProjectsLatestFundamentalsSnapshot()
    {
        var (db, service, scope) = await BuildContextAsync();
        await using var _ = scope;

        var asset = BuildAsset("a1", "AIR.PA", "Airbus", "XPAR", "France", "Aerospace", AssetTypeEnum.Stock);
        db.Assets.Add(asset);
        db.AssetFundamentalsSnapshots.Add(BuildFundamentals("a1", 100_000m, 10m, 1m, new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc)));
        db.AssetFundamentalsSnapshots.Add(BuildFundamentals("a1", 150_000m, 12m, 1.5m, new DateTime(2026, 6, 2, 8, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var result = await service.GetPagedAsync(new ScreenerQueryViewModel());

        var item = Assert.Single(result.Items);
        Assert.Equal(150_000m, item.MarketCap);
        Assert.Equal(12m, item.TrailingPE);
        Assert.Equal(1.5m, item.DividendYield);
    }

    [Fact]
    public async Task SavePresetAsync_ThenGetPresetsAsync_ReturnsPresetForOwningUser()
    {
        var userId = $"user-{Guid.NewGuid():N}";
        var (db, service, scope) = await BuildContextForUserAsync(userId);
        await using var _ = scope;

        db.Users.Add(BuildUser(userId));
        await db.SaveChangesAsync();

        var created = await service.SavePresetAsync(new ScreenerPresetCreateViewModel
        {
            Name = "Mon preset",
            Query = new ScreenerQueryViewModel { MinPE = 5m, Search = "air" }
        });

        Assert.False(string.IsNullOrEmpty(created.Id));
        Assert.Equal("Mon preset", created.Name);
        Assert.Equal(5m, created.Query.MinPE);

        var presets = await service.GetPresetsAsync();
        var preset = Assert.Single(presets);
        Assert.Equal("Mon preset", preset.Name);
        Assert.Equal("air", preset.Query.Search);
    }

    [Fact]
    public async Task DeletePresetAsync_SoftDeletes_PresetNoLongerListed()
    {
        var userId = $"user-{Guid.NewGuid():N}";
        var (db, service, scope) = await BuildContextForUserAsync(userId);
        await using var _ = scope;

        db.Users.Add(BuildUser(userId));
        await db.SaveChangesAsync();

        var created = await service.SavePresetAsync(new ScreenerPresetCreateViewModel
        {
            Name = "À supprimer",
            Query = new ScreenerQueryViewModel()
        });

        await service.DeletePresetAsync(created.Id);

        var presets = await service.GetPresetsAsync();
        Assert.Empty(presets);

        var stored = await db.UserScreenerPresets.FirstAsync(p => p.Id == created.Id);
        Assert.True(stored.IsDeleted);
    }

    [Fact]
    public async Task DeletePresetAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        var userId = $"user-{Guid.NewGuid():N}";
        var (db, service, scope) = await BuildContextForUserAsync(userId);
        await using var _ = scope;

        db.Users.Add(BuildUser(userId));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeletePresetAsync("unknown-id"));
    }
}
