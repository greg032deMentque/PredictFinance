using AutoMapper;
using BackPredictFinance.Common.Fundamentals;
using BackPredictFinance.Common.Localization;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services;
using BackPredictFinance.Services.Fundamentals;
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

namespace BackPredictFinance.Tests.Fundamentals;

public sealed class FundamentalScoringServiceTests
{
    private const string UniverseId = "PEA_FR_EQUITIES";

    private static async Task<(FinanceDbContext Db, FundamentalScoringService Service, Mock<IFundamentalsProvider> ProviderMock, IAsyncDisposable Scope)> BuildContextAsync()
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
        var service = new FundamentalScoringService(scope.ServiceProvider, providerMock.Object, NullLogger<FundamentalScoringService>.Instance);

        return (db, service, providerMock, scope);
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

    private static AssetPeaEligibility BuildEligibility(string assetId) => new()
    {
        Id = Guid.NewGuid().ToString(),
        AssetId = assetId,
        EligibilityStatus = PeaEligibilityStatusEnum.ConfirmedEligible,
        SourceType = PeaEligibilitySourceTypeEnum.ManualRegistry,
        UniverseId = UniverseId
    };

    private static MarketFundamentalData BuildFundamentals(
        string symbol,
        string sector,
        decimal? returnOnEquity = null,
        decimal? operatingMargin = null,
        decimal? currentRatio = null,
        decimal? debtToEquity = null,
        decimal? revenueGrowth = null,
        decimal? earningsGrowth = null) => new()
    {
        Symbol = symbol,
        CompanyName = symbol,
        Sector = sector,
        ReturnOnEquity = returnOnEquity,
        OperatingMargin = operatingMargin,
        CurrentRatio = currentRatio,
        DebtToEquity = debtToEquity,
        RevenueGrowth = revenueGrowth,
        EarningsGrowth = earningsGrowth
    };

    private static void SetupProvider(Mock<IFundamentalsProvider> providerMock, Dictionary<string, MarketFundamentalData> fundamentalsBySymbol)
    {
        providerMock
            .Setup(x => x.GetFundamentalsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string symbol, CancellationToken _) => fundamentalsBySymbol[symbol]);
    }

    [Fact]
    public async Task ScoreAsync_SectorWithSingleMemberForMetric_FallsBackToGlobalUniverse()
    {
        var (db, service, providerMock, scope) = await BuildContextAsync();
        await using var _ = scope;

        var techAsset = BuildAsset("a1", "TECH1", "Tech One");
        var peers = Enumerable.Range(1, 5).Select(i => BuildAsset($"p{i}", $"PEER{i}", $"Peer {i}")).ToList();

        db.Assets.Add(techAsset);
        db.Assets.AddRange(peers);
        db.AssetPeaEligibilities.Add(BuildEligibility("a1"));
        foreach (var peer in peers)
        {
            db.AssetPeaEligibilities.Add(BuildEligibility(peer.Id));
        }
        await db.SaveChangesAsync();

        var fundamentalsBySymbol = new Dictionary<string, MarketFundamentalData>
        {
            ["TECH1"] = BuildFundamentals("TECH1", "Tech", returnOnEquity: 0.20m)
        };
        for (var i = 1; i <= 5; i++)
        {
            fundamentalsBySymbol[$"PEER{i}"] = BuildFundamentals($"PEER{i}", "Other", returnOnEquity: 0.05m * i);
        }
        SetupProvider(providerMock, fundamentalsBySymbol);

        var response = await service.ScoreAsync(new FundamentalScoreRequest
        {
            UniverseId = UniverseId,
            Symbols = ["TECH1"],
            MinCategoriesRequired = 1,
            CoveragePenaltyEnabled = true,
            IncludeRankPosition = false
        });

        var result = Assert.Single(response.Results);
        Assert.True(result.UsedGlobalUniverseFallback);
        Assert.DoesNotContain("returnOnEquity", result.MissingMetrics);
        Assert.True(result.ProfitabilityScore.HasValue);
        Assert.Contains(result.Notes, note => note.Contains("insufficient sector sample", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ScoreAsync_UnknownSector_FallsBackSystematicallyWithEmptyPercentileGroupLabel()
    {
        var (db, service, providerMock, scope) = await BuildContextAsync();
        await using var _ = scope;

        var noSectorAsset = BuildAsset("a1", "NOSEC1", "No Sector One");
        var peers = Enumerable.Range(1, 5).Select(i => BuildAsset($"p{i}", $"PEER{i}", $"Peer {i}")).ToList();

        db.Assets.Add(noSectorAsset);
        db.Assets.AddRange(peers);
        db.AssetPeaEligibilities.Add(BuildEligibility("a1"));
        foreach (var peer in peers)
        {
            db.AssetPeaEligibilities.Add(BuildEligibility(peer.Id));
        }
        await db.SaveChangesAsync();

        var fundamentalsBySymbol = new Dictionary<string, MarketFundamentalData>
        {
            ["NOSEC1"] = BuildFundamentals("NOSEC1", string.Empty, returnOnEquity: 0.20m)
        };
        for (var i = 1; i <= 5; i++)
        {
            fundamentalsBySymbol[$"PEER{i}"] = BuildFundamentals($"PEER{i}", "Other", returnOnEquity: 0.05m * i);
        }
        SetupProvider(providerMock, fundamentalsBySymbol);

        var response = await service.ScoreAsync(new FundamentalScoreRequest
        {
            UniverseId = UniverseId,
            Symbols = ["NOSEC1"],
            MinCategoriesRequired = 1,
            CoveragePenaltyEnabled = true,
            IncludeRankPosition = false
        });

        var result = Assert.Single(response.Results);
        Assert.Equal(string.Empty, result.PercentileGroupLabel);
        Assert.True(result.UsedGlobalUniverseFallback);
        Assert.Contains(result.Notes, note => note.Contains("unknown sector", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ScoreAsync_SectorWithAtLeastFiveMembersForMetric_UsesSectorPercentile()
    {
        var (db, service, providerMock, scope) = await BuildContextAsync();
        await using var _ = scope;

        var sectorAssets = Enumerable.Range(1, 5).Select(i => BuildAsset($"a{i}", $"INDUS{i}", $"Industrial {i}")).ToList();
        db.Assets.AddRange(sectorAssets);
        foreach (var asset in sectorAssets)
        {
            db.AssetPeaEligibilities.Add(BuildEligibility(asset.Id));
        }
        await db.SaveChangesAsync();

        var fundamentalsBySymbol = new Dictionary<string, MarketFundamentalData>();
        for (var i = 1; i <= 5; i++)
        {
            fundamentalsBySymbol[$"INDUS{i}"] = BuildFundamentals($"INDUS{i}", "Industrials", returnOnEquity: 0.02m * i);
        }
        SetupProvider(providerMock, fundamentalsBySymbol);

        var response = await service.ScoreAsync(new FundamentalScoreRequest
        {
            UniverseId = UniverseId,
            Symbols = ["INDUS1"],
            MinCategoriesRequired = 1,
            CoveragePenaltyEnabled = true,
            IncludeRankPosition = false
        });

        var result = Assert.Single(response.Results);
        Assert.False(result.UsedGlobalUniverseFallback);
        Assert.Equal("Industrials", result.PercentileGroupLabel);
        Assert.True(result.ProfitabilityScore.HasValue);
    }

    [Fact]
    public async Task ScoreAsync_GrowthCategoryWithOnlyRevenueGrowth_ComputesScoreFromAvailableMetric()
    {
        var (db, service, providerMock, scope) = await BuildContextAsync();
        await using var _ = scope;

        var growthAsset = BuildAsset("a1", "GROWTH1", "Growth One");
        var peers = Enumerable.Range(1, 2).Select(i => BuildAsset($"p{i}", $"GPEER{i}", $"Growth Peer {i}")).ToList();

        db.Assets.Add(growthAsset);
        db.Assets.AddRange(peers);
        db.AssetPeaEligibilities.Add(BuildEligibility("a1"));
        foreach (var peer in peers)
        {
            db.AssetPeaEligibilities.Add(BuildEligibility(peer.Id));
        }
        await db.SaveChangesAsync();

        var fundamentalsBySymbol = new Dictionary<string, MarketFundamentalData>
        {
            ["GROWTH1"] = BuildFundamentals("GROWTH1", string.Empty, revenueGrowth: 0.08m),
            ["GPEER1"] = BuildFundamentals("GPEER1", string.Empty, revenueGrowth: 0.03m),
            ["GPEER2"] = BuildFundamentals("GPEER2", string.Empty, revenueGrowth: 0.12m)
        };
        SetupProvider(providerMock, fundamentalsBySymbol);

        var response = await service.ScoreAsync(new FundamentalScoreRequest
        {
            UniverseId = UniverseId,
            Symbols = ["GROWTH1"],
            MinCategoriesRequired = 1,
            CoveragePenaltyEnabled = true,
            IncludeRankPosition = false
        });

        var result = Assert.Single(response.Results);
        Assert.True(result.GrowthScore.HasValue);
        Assert.Contains("earningsGrowth", result.MissingMetrics);
    }

    [Fact]
    public async Task ScoreAsync_GrowthCategoryFullyAbsent_GrowthScoreNullAndCoverageOverSix()
    {
        var (db, service, providerMock, scope) = await BuildContextAsync();
        await using var _ = scope;

        var asset = BuildAsset("a1", "NOGROWTH1", "No Growth One");
        var peers = Enumerable.Range(1, 2).Select(i => BuildAsset($"p{i}", $"NGPEER{i}", $"No Growth Peer {i}")).ToList();

        db.Assets.Add(asset);
        db.Assets.AddRange(peers);
        db.AssetPeaEligibilities.Add(BuildEligibility("a1"));
        foreach (var peer in peers)
        {
            db.AssetPeaEligibilities.Add(BuildEligibility(peer.Id));
        }
        await db.SaveChangesAsync();

        var fundamentalsBySymbol = new Dictionary<string, MarketFundamentalData>
        {
            ["NOGROWTH1"] = BuildFundamentals(
                "NOGROWTH1",
                string.Empty,
                returnOnEquity: 0.15m,
                operatingMargin: 0.10m,
                currentRatio: 1.5m,
                debtToEquity: 0.4m),
            ["NGPEER1"] = BuildFundamentals(
                "NGPEER1",
                string.Empty,
                returnOnEquity: 0.10m,
                operatingMargin: 0.08m,
                currentRatio: 1.2m,
                debtToEquity: 0.6m),
            ["NGPEER2"] = BuildFundamentals(
                "NGPEER2",
                string.Empty,
                returnOnEquity: 0.20m,
                operatingMargin: 0.12m,
                currentRatio: 1.8m,
                debtToEquity: 0.3m)
        };
        SetupProvider(providerMock, fundamentalsBySymbol);

        var response = await service.ScoreAsync(new FundamentalScoreRequest
        {
            UniverseId = UniverseId,
            Symbols = ["NOGROWTH1"],
            MinCategoriesRequired = 1,
            CoveragePenaltyEnabled = true,
            IncludeRankPosition = false
        });

        var result = Assert.Single(response.Results);
        Assert.Null(result.GrowthScore);
        Assert.Equal(3, result.CategoriesPresent);
        Assert.Equal(0.5m, result.CategoryCoverage);
    }
}
