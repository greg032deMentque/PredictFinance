using AutoMapper;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.Localization;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.ClientFinanceServices.PortfolioMetrics;
using BackPredictFinance.ViewModels.UserViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;

namespace BackPredictFinance.Tests.Portfolio;

public sealed class PortfolioRiskMetricsServiceTests
{
    private const string HeldAssetId = "asset-risk";
    private const string HeldAssetSymbol = "RISK.PA";
    private const string RiskFreeAssetId = "asset-xeon";
    private const string RiskFreeAssetSymbol = "XEON.PA";

    private static async Task<(FinanceDbContext Db, PortfolioRiskMetricsService Service, string UserId, string PortfolioId, IAsyncDisposable Scope)> BuildContextAsync()
    {
        var userId = $"user-{Guid.NewGuid():N}";

        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId)
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
        services.AddScoped<BackPredictFinance.Services.ILogService>(_ => Mock.Of<BackPredictFinance.Services.ILogService>());
        services.AddMemoryCache();

        services
            .AddIdentity<User, IdentityRole>()
            .AddEntityFrameworkStores<FinanceDbContext>()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateAsyncScope();

        var service = new PortfolioRiskMetricsService(scope.ServiceProvider);

        db.Users.Add(new User
        {
            Id = userId,
            UserName = $"{userId}@test.local",
            Email = $"{userId}@test.local",
            NormalizedEmail = $"{userId}@test.local".ToUpperInvariant(),
            NormalizedUserName = $"{userId}@test.local".ToUpperInvariant()
        });

        var portfolioId = Guid.NewGuid().ToString();
        db.Portfolios.Add(new BackPredictFinance.Datas.Entities.Portfolio
        {
            Id = portfolioId,
            UserId = userId,
            Name = "Compte-titres test",
            PortfolioType = PortfolioTypeEnum.CompteTitres,
            Status = PortfolioStatusEnum.Active
        });

        db.Assets.Add(new Asset
        {
            Id = HeldAssetId,
            Symbol = HeldAssetSymbol,
            ProviderSymbol = HeldAssetSymbol,
            Exchange = "XPAR",
            Currency = "EUR",
            AssetType = AssetTypeEnum.Stock
        });

        var userAssetId = Guid.NewGuid().ToString();
        db.UserAssets.Add(new UserAsset
        {
            Id = userAssetId,
            UserId = userId,
            AssetId = HeldAssetId,
            Quantity = 10m
        });

        var startDate = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        db.AssetTransactions.Add(new AssetTransaction
        {
            Id = Guid.NewGuid().ToString(),
            UserAssetId = userAssetId,
            PortfolioId = portfolioId,
            TimestampUtc = startDate,
            TransactionType = TransactionTypeEnum.Buy,
            Quantity = 10m,
            UnitPrice = 100m,
            Fees = 0m
        });

        var heldPrices = BuildAlternatingPriceSeries(100m, 0.02m, -0.01m, 21);
        for (var i = 0; i < heldPrices.Count; i++)
        {
            db.PriceHistories.Add(new PriceHistory
            {
                Id = Guid.NewGuid().ToString(),
                AssetId = HeldAssetId,
                RetrievedAtUtc = startDate.AddDays(i),
                Price = heldPrices[i]
            });
        }

        await db.SaveChangesAsync();

        return (db, service, userId, portfolioId, scope);
    }

    private static List<decimal> BuildAlternatingPriceSeries(decimal basePrice, decimal returnA, decimal returnB, int count)
    {
        var prices = new List<decimal> { basePrice };
        for (var i = 1; i < count; i++)
        {
            var r = i % 2 == 1 ? returnA : returnB;
            prices.Add(decimal.Round(prices[^1] * (1m + r), 8));
        }
        return prices;
    }

    private static decimal ComputeExpectedAnnualizedReturn(List<decimal> dailyReturns)
    {
        var product = dailyReturns.Aggregate(1m, (acc, r) => acc * (1m + r));
        var exponent = 252.0 / dailyReturns.Count;
        return (decimal)Math.Pow((double)product, exponent) - 1m;
    }

    private static decimal ComputeExpectedAnnualizedVolatility(List<decimal> dailyReturns)
    {
        var mean = dailyReturns.Average();
        var variance = dailyReturns.Sum(r => (r - mean) * (r - mean)) / (dailyReturns.Count - 1);
        var dailyVol = (decimal)Math.Sqrt((double)variance);
        return dailyVol * (decimal)Math.Sqrt(252.0);
    }

    private static List<decimal> ComputeReturnsFromPrices(List<decimal> prices)
    {
        var returns = new List<decimal>(prices.Count - 1);
        for (var i = 1; i < prices.Count; i++)
        {
            returns.Add((prices[i] - prices[i - 1]) / prices[i - 1]);
        }
        return returns;
    }

    [Fact]
    public async Task GetMetricsAsync_WithXeonHistory_SubtractsRiskFreeAnnualizedReturn()
    {
        var (db, service, _, portfolioId, scope) = await BuildContextAsync();
        await using var _1 = scope;

        db.Assets.Add(new Asset
        {
            Id = RiskFreeAssetId,
            Symbol = RiskFreeAssetSymbol,
            ProviderSymbol = RiskFreeAssetSymbol,
            Exchange = "XPAR",
            Currency = "EUR",
            AssetType = AssetTypeEnum.Etf
        });

        var startDate = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        var riskFreePrices = BuildAlternatingPriceSeries(100m, 0.001m, -0.0003m, 21);
        for (var i = 0; i < riskFreePrices.Count; i++)
        {
            db.AssetCandleSnapshots.Add(new AssetCandleSnapshot
            {
                Id = Guid.NewGuid().ToString(),
                AssetId = RiskFreeAssetId,
                TimestampUtc = startDate.AddDays(i),
                Interval = "1d",
                Open = riskFreePrices[i],
                High = riskFreePrices[i],
                Low = riskFreePrices[i],
                Close = riskFreePrices[i],
                Volume = 0m,
                Source = "test"
            });
        }
        await db.SaveChangesAsync();

        var result = await service.GetMetricsAsync(portfolioId);

        Assert.NotNull(result);
        Assert.NotNull(result!.SharpeRatio);

        var heldPrices = BuildAlternatingPriceSeries(100m, 0.02m, -0.01m, 21);
        var portfolioReturns = ComputeReturnsFromPrices(heldPrices);
        var portfolioAnnualizedReturn = ComputeExpectedAnnualizedReturn(portfolioReturns);
        var portfolioVol = ComputeExpectedAnnualizedVolatility(portfolioReturns);

        var riskFreeReturns = ComputeReturnsFromPrices(riskFreePrices);
        var riskFreeAnnualizedReturn = ComputeExpectedAnnualizedReturn(riskFreeReturns);

        var expectedSharpe = Math.Round((portfolioAnnualizedReturn - riskFreeAnnualizedReturn) / portfolioVol, 2);

        Assert.Equal(expectedSharpe, result.SharpeRatio!.Value);

        var legacySharpe = Math.Round(portfolioAnnualizedReturn / portfolioVol, 2);
        Assert.NotEqual(legacySharpe, result.SharpeRatio!.Value);
    }

    [Fact]
    public async Task GetMetricsAsync_WithoutXeonHistory_FallsBackToLegacyFormula()
    {
        var (_, service, _, portfolioId, scope) = await BuildContextAsync();
        await using var _1 = scope;

        var result = await service.GetMetricsAsync(portfolioId);

        Assert.NotNull(result);
        Assert.NotNull(result!.SharpeRatio);

        var heldPrices = BuildAlternatingPriceSeries(100m, 0.02m, -0.01m, 21);
        var portfolioReturns = ComputeReturnsFromPrices(heldPrices);
        var portfolioAnnualizedReturn = ComputeExpectedAnnualizedReturn(portfolioReturns);
        var portfolioVol = ComputeExpectedAnnualizedVolatility(portfolioReturns);

        var expectedSharpe = Math.Round(portfolioAnnualizedReturn / portfolioVol, 2);

        Assert.Equal(expectedSharpe, result.SharpeRatio!.Value);
    }

    [Fact]
    public async Task GetMetricsAsync_XeonCandlesTimestampedLateInDay_StillIncludesLastDayCandle()
    {
        var (db, service, _, portfolioId, scope) = await BuildContextAsync();
        await using var _1 = scope;

        db.Assets.Add(new Asset
        {
            Id = RiskFreeAssetId,
            Symbol = RiskFreeAssetSymbol,
            ProviderSymbol = RiskFreeAssetSymbol,
            Exchange = "XPAR",
            Currency = "EUR",
            AssetType = AssetTypeEnum.Etf
        });

        var startDate = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        var riskFreePrices = BuildAlternatingPriceSeries(100m, 0.001m, -0.0003m, 21);
        for (var i = 0; i < riskFreePrices.Count; i++)
        {
            // Horodatage volontairement place en fin de journee (20h UTC), comme le ferait
            // un fournisseur qui timestamp une bougie journaliere sur la cloture de marche
            // plutot que sur minuit. NormalizeCandleTimestampUtc (ingestion reelle) ne
            // tronque jamais l'heure : ce cas doit rester couvert par la borne de date.
            db.AssetCandleSnapshots.Add(new AssetCandleSnapshot
            {
                Id = Guid.NewGuid().ToString(),
                AssetId = RiskFreeAssetId,
                TimestampUtc = startDate.AddDays(i).AddHours(20),
                Interval = "1d",
                Open = riskFreePrices[i],
                High = riskFreePrices[i],
                Low = riskFreePrices[i],
                Close = riskFreePrices[i],
                Volume = 0m,
                Source = "test"
            });
        }
        await db.SaveChangesAsync();

        var result = await service.GetMetricsAsync(portfolioId);

        Assert.NotNull(result);
        Assert.NotNull(result!.SharpeRatio);

        var heldPrices = BuildAlternatingPriceSeries(100m, 0.02m, -0.01m, 21);
        var portfolioReturns = ComputeReturnsFromPrices(heldPrices);
        var portfolioAnnualizedReturn = ComputeExpectedAnnualizedReturn(portfolioReturns);
        var portfolioVol = ComputeExpectedAnnualizedVolatility(portfolioReturns);

        // Attendu calcule sur les 21 bougies (20 rendements) : si la derniere bougie
        // (jour 20, horodatee a 20h) etait exclue a tort par la borne de date, ce calcul
        // ne porterait que sur 20 bougies (19 rendements) et l'assertion echouerait.
        var riskFreeReturns = ComputeReturnsFromPrices(riskFreePrices);
        var riskFreeAnnualizedReturn = ComputeExpectedAnnualizedReturn(riskFreeReturns);

        var expectedSharpe = Math.Round((portfolioAnnualizedReturn - riskFreeAnnualizedReturn) / portfolioVol, 2);

        Assert.Equal(expectedSharpe, result.SharpeRatio!.Value);
    }
}
