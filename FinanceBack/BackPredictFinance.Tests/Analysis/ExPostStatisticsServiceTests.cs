using AutoMapper;
using BackPredictFinance.Common.Localization;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services;
using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
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

public sealed class ExPostStatisticsServiceTests
{
    private const string CurrentUserId = "ex-post-user-1";
    private const string OtherUserId = "ex-post-user-2";

    private static async Task<(FinanceDbContext Db, ExPostStatisticsService Service, IAsyncDisposable Scope)> BuildContextAsync(string currentUserId)
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

        var assetSupportServiceMock = new Mock<IClientFinanceAssetSupportService>();
        assetSupportServiceMock.Setup(x => x.GetRequiredCurrentUserId()).Returns(currentUserId);
        services.AddSingleton(assetSupportServiceMock.Object);

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateAsyncScope();

        var service = new ExPostStatisticsService(scope.ServiceProvider, assetSupportServiceMock.Object);

        return (db, service, scope);
    }

    private static User BuildUser(string userId) => new()
    {
        Id = userId,
        UserName = $"{userId}@test.local",
        Email = $"{userId}@test.local",
        NormalizedEmail = $"{userId}@test.local".ToUpperInvariant(),
        NormalizedUserName = $"{userId}@test.local".ToUpperInvariant(),
        IsActive = true
    };

    private static Asset BuildAsset(string assetId) => new()
    {
        Id = assetId,
        Symbol = assetId,
        ProviderSymbol = assetId,
        Exchange = "XPAR",
        Currency = "EUR",
        AssetType = AssetTypeEnum.Stock
    };

    private static async Task SeedTerminalOutcomeAsync(
        FinanceDbContext db,
        string userId,
        string assetId,
        string patternId,
        bool hasEarningsInWindow,
        SignalOutcomeEnum outcome,
        string suffix)
    {
        var emissionDateUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        db.AnalysisRuns.Add(new AnalysisRun
        {
            Id = $"run-{suffix}",
            UserId = userId,
            AssetId = assetId,
            Status = AnalysisRunStatusEnum.Completed,
            StartedAtUtc = emissionDateUtc,
            CompletedAtUtc = emissionDateUtc,
            RawPayload = "{}"
        });

        db.PatternAssessments.Add(new PatternAssessment
        {
            Id = $"pa-{suffix}",
            AnalysisRunId = $"run-{suffix}",
            PatternId = patternId,
            Probability = 0.5m,
            Confidence = 0.5m,
            CurrentPrice = 100m
        });

        db.DecisionSignals.Add(new DecisionSignal
        {
            Id = $"ds-{suffix}",
            AnalysisRunId = $"run-{suffix}",
            HorizonDays = 20,
            EarningsDateUtc = hasEarningsInWindow ? emissionDateUtc.AddDays(5) : null
        });

        db.SignalOutcomes.Add(new SignalOutcome
        {
            Id = $"so-{suffix}",
            AnalysisRunId = $"run-{suffix}",
            PatternAssessmentId = $"pa-{suffix}",
            DecisionSignalId = $"ds-{suffix}",
            Outcome = outcome,
            EvaluationWindowDays = 20,
            EvaluatedAtUtc = emissionDateUtc.AddDays(20),
            PolicyVersion = "v1"
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedSegmentAsync(
        FinanceDbContext db,
        string userId,
        string assetId,
        string patternId,
        bool hasEarningsInWindow,
        string segmentTag,
        int winCount,
        int lossCount)
    {
        for (var i = 0; i < winCount; i++)
        {
            await SeedTerminalOutcomeAsync(db, userId, assetId, patternId, hasEarningsInWindow, SignalOutcomeEnum.TargetHit, $"{segmentTag}-win-{i}");
        }

        for (var i = 0; i < lossCount; i++)
        {
            await SeedTerminalOutcomeAsync(db, userId, assetId, patternId, hasEarningsInWindow, SignalOutcomeEnum.InvalidationHit, $"{segmentTag}-loss-{i}");
        }
    }

    [Fact]
    public async Task GetPatternStatisticsAsync_SegmentsByEarningsWindow_ReturnsDistinctGroupsForSamePattern()
    {
        var (db, service, scope) = await BuildContextAsync(CurrentUserId);
        await using (scope)
        {
            const string assetId = "asset-segments";
            const string patternId = "DoubleTop";
            db.Users.Add(BuildUser(CurrentUserId));
            db.Assets.Add(BuildAsset(assetId));
            await db.SaveChangesAsync();

            await SeedSegmentAsync(db, CurrentUserId, assetId, patternId, hasEarningsInWindow: true, "earn", winCount: 12, lossCount: 8);
            await SeedSegmentAsync(db, CurrentUserId, assetId, patternId, hasEarningsInWindow: false, "noearn", winCount: 12, lossCount: 8);

            var result = await service.GetPatternStatisticsAsync();

            var patternGroups = result.PatternStats.Where(x => x.PatternId == patternId).ToList();
            Assert.Equal(2, patternGroups.Count);
            Assert.Contains(patternGroups, x => x.HasEarningsInWindow);
            Assert.Contains(patternGroups, x => !x.HasEarningsInWindow);
        }
    }

    [Fact]
    public async Task GetPatternStatisticsAsync_SampleSizeBelowThreshold_ReturnsInsufficientDataWithNullRates()
    {
        var (db, service, scope) = await BuildContextAsync(CurrentUserId);
        await using (scope)
        {
            const string assetId = "asset-small";
            const string patternId = "HeadAndShoulders";
            db.Users.Add(BuildUser(CurrentUserId));
            db.Assets.Add(BuildAsset(assetId));
            await db.SaveChangesAsync();

            await SeedSegmentAsync(db, CurrentUserId, assetId, patternId, hasEarningsInWindow: false, "small", winCount: 10, lossCount: 9);

            var result = await service.GetPatternStatisticsAsync();

            var stat = Assert.Single(result.PatternStats, x => x.PatternId == patternId);
            Assert.True(stat.InsufficientData);
            Assert.Equal(19, stat.SampleSize);
            Assert.Null(stat.WinRate);
            Assert.Null(stat.WinRateLow);
            Assert.Null(stat.WinRateHigh);
        }
    }

    [Fact]
    public async Task GetPatternStatisticsAsync_SampleSizeAtThreshold_ReturnsWilsonBoundsWithinValidRange()
    {
        var (db, service, scope) = await BuildContextAsync(CurrentUserId);
        await using (scope)
        {
            const string assetId = "asset-sufficient";
            const string patternId = "InverseHeadAndShoulders";
            db.Users.Add(BuildUser(CurrentUserId));
            db.Assets.Add(BuildAsset(assetId));
            await db.SaveChangesAsync();

            await SeedSegmentAsync(db, CurrentUserId, assetId, patternId, hasEarningsInWindow: false, "sufficient", winCount: 14, lossCount: 6);

            var result = await service.GetPatternStatisticsAsync();

            var stat = Assert.Single(result.PatternStats, x => x.PatternId == patternId);
            Assert.False(stat.InsufficientData);
            Assert.Equal(20, stat.SampleSize);
            Assert.NotNull(stat.WinRate);
            Assert.NotNull(stat.WinRateLow);
            Assert.NotNull(stat.WinRateHigh);
            Assert.True(stat.WinRateLow >= 0m);
            Assert.True(stat.WinRateLow <= stat.WinRate);
            Assert.True(stat.WinRate <= stat.WinRateHigh);
            Assert.True(stat.WinRateHigh <= 1m);
        }
    }

    [Fact]
    public async Task GetPatternStatisticsAsync_AlwaysReturnsSelectionBiasDisclaimerTrue()
    {
        var (db, service, scope) = await BuildContextAsync(CurrentUserId);
        await using (scope)
        {
            const string assetId = "asset-disclaimer";
            const string patternId = "DoubleBottom";
            db.Users.Add(BuildUser(CurrentUserId));
            db.Assets.Add(BuildAsset(assetId));
            await db.SaveChangesAsync();

            await SeedSegmentAsync(db, CurrentUserId, assetId, patternId, hasEarningsInWindow: false, "disclaimer", winCount: 3, lossCount: 2);

            var result = await service.GetPatternStatisticsAsync();

            Assert.True(result.SelectionBiasDisclaimer);
            Assert.All(result.PatternStats, x => Assert.True(x.SelectionBiasDisclaimer));
        }
    }

    [Fact]
    public async Task GetPatternStatisticsAsync_FiltersOutcomesFromOtherUsers()
    {
        var (db, service, scope) = await BuildContextAsync(CurrentUserId);
        await using (scope)
        {
            const string assetId = "asset-scope";
            const string patternId = "CupAndHandle";
            db.Users.Add(BuildUser(CurrentUserId));
            db.Users.Add(BuildUser(OtherUserId));
            db.Assets.Add(BuildAsset(assetId));
            await db.SaveChangesAsync();

            await SeedSegmentAsync(db, CurrentUserId, assetId, patternId, hasEarningsInWindow: false, "mine", winCount: 3, lossCount: 2);
            await SeedSegmentAsync(db, OtherUserId, assetId, patternId, hasEarningsInWindow: false, "other", winCount: 30, lossCount: 30);

            var result = await service.GetPatternStatisticsAsync();

            var stat = Assert.Single(result.PatternStats, x => x.PatternId == patternId);
            Assert.Equal(5, stat.SampleSize);
        }
    }
}
