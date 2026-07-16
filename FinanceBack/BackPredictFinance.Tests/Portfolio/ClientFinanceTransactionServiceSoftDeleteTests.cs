using AutoMapper;
using BackPredictFinance.Common.Localization;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services;
using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.Services.ClientFinanceServices.Tax;
using BackPredictFinance.ViewModels.UserViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BackPredictFinance.Tests.Portfolio;

public sealed class ClientFinanceTransactionServiceSoftDeleteTests
{
    private static User BuildUser(string userId) => new()
    {
        Id = userId,
        UserName = $"{userId}@test.local",
        Email = $"{userId}@test.local",
        NormalizedEmail = $"{userId}@test.local".ToUpperInvariant(),
        NormalizedUserName = $"{userId}@test.local".ToUpperInvariant()
    };

    private static async Task<(FinanceDbContext Db, IServiceProvider Services, IAsyncDisposable Scope)> BuildContextForUserAsync(string userId)
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

        return (db, scope.ServiceProvider, scope);
    }

    [Fact]
    public async Task DeleteTransactionAsync_SoftDeletesSell_NoLongerCountedInPositionOrTax()
    {
        var userId = $"user-{Guid.NewGuid():N}";
        var (db, serviceProvider, scope) = await BuildContextForUserAsync(userId);
        await using var _ = scope;

        const string assetId = "asset-1";
        const string userAssetId = "user-asset-1";
        const string portfolioId = "portfolio-1";

        db.Users.Add(BuildUser(userId));

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
            Quantity = 0m
        });

        db.Portfolios.Add(new Datas.Entities.Portfolio
        {
            Id = portfolioId,
            UserId = userId,
            Name = "CTO test",
            PortfolioType = PortfolioTypeEnum.CompteTitres,
            Status = PortfolioStatusEnum.Active
        });

        db.AssetTransactions.Add(new AssetTransaction
        {
            Id = "tx-buy-1",
            UserAssetId = userAssetId,
            PortfolioId = portfolioId,
            TimestampUtc = new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc),
            TransactionType = TransactionTypeEnum.Buy,
            Quantity = 10m,
            UnitPrice = 100m,
            Fees = 2m
        });

        var sellTransaction = new AssetTransaction
        {
            Id = "tx-sell-1",
            UserAssetId = userAssetId,
            PortfolioId = portfolioId,
            TimestampUtc = new DateTime(2026, 2, 10, 9, 0, 0, DateTimeKind.Utc),
            TransactionType = TransactionTypeEnum.Sell,
            Quantity = 10m,
            UnitPrice = 150m,
            Fees = 1m
        };
        db.AssetTransactions.Add(sellTransaction);

        await db.SaveChangesAsync();

        var userAsset = await db.UserAssets.SingleAsync(x => x.Id == userAssetId);
        userAsset.Quantity = 0m;
        await db.SaveChangesAsync();

        var taxServiceBaseline = new TaxService(serviceProvider);
        var baselineSummary = await taxServiceBaseline.GetAllPortfoliosTaxSummaryAsync(2026);
        Assert.Single(baselineSummary);
        Assert.NotEmpty(baselineSummary[0].Positions);
        Assert.True(baselineSummary[0].TotalRealizedPnl > 0m);

        var positionLoaderBaseline = new PortfolioContextLoader(db);
        var baselineContext = await positionLoaderBaseline.TryLoadAsync(userId, assetId);
        Assert.NotNull(baselineContext);
        Assert.False(baselineContext!.HoldsInstrument);

        var transactionService = new ClientFinanceTransactionService(
            serviceProvider,
            Mock.Of<IClientFinanceAssetSupportService>(),
            Mock.Of<IPortfolioService>());

        await transactionService.DeleteTransactionAsync("tx-sell-1");

        var storedTransaction = await db.AssetTransactions.AsNoTracking().SingleAsync(x => x.Id == "tx-sell-1");
        Assert.True(storedTransaction.IsDeleted);

        var reloadedUserAsset = await db.UserAssets.AsNoTracking().SingleAsync(x => x.Id == userAssetId);
        Assert.Equal(10m, reloadedUserAsset.Quantity);

        var positionLoader = new PortfolioContextLoader(db);
        var contextAfterDelete = await positionLoader.TryLoadAsync(userId, assetId);
        Assert.NotNull(contextAfterDelete);
        Assert.True(contextAfterDelete!.HoldsInstrument);
        Assert.Equal(10m, contextAfterDelete.TotalQuantityHeld);

        var taxService = new TaxService(serviceProvider);
        var summaryAfterDelete = await taxService.GetAllPortfoliosTaxSummaryAsync(2026);
        Assert.Single(summaryAfterDelete);
        Assert.Empty(summaryAfterDelete[0].Positions);
        Assert.Equal(0m, summaryAfterDelete[0].TotalRealizedPnl);
    }

    [Fact]
    public async Task DeleteTransactionAsync_CalledTwice_IsIdempotent_AndDoesNotDoubleAdjustQuantity()
    {
        var userId = $"user-{Guid.NewGuid():N}";
        var (db, serviceProvider, scope) = await BuildContextForUserAsync(userId);
        await using var _ = scope;

        const string assetId = "asset-2";
        const string userAssetId = "user-asset-2";
        const string portfolioId = "portfolio-2";

        db.Users.Add(BuildUser(userId));

        db.Assets.Add(new Asset
        {
            Id = assetId,
            Symbol = "MC.PA",
            ProviderSymbol = "MC.PA",
            Exchange = "XPAR",
            Currency = "EUR",
            AssetType = AssetTypeEnum.Stock
        });

        db.UserAssets.Add(new UserAsset
        {
            Id = userAssetId,
            UserId = userId,
            AssetId = assetId,
            Quantity = 5m
        });

        db.Portfolios.Add(new Datas.Entities.Portfolio
        {
            Id = portfolioId,
            UserId = userId,
            Name = "CTO test 2",
            PortfolioType = PortfolioTypeEnum.CompteTitres,
            Status = PortfolioStatusEnum.Active
        });

        db.AssetTransactions.Add(new AssetTransaction
        {
            Id = "tx-buy-2",
            UserAssetId = userAssetId,
            PortfolioId = portfolioId,
            TimestampUtc = new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc),
            TransactionType = TransactionTypeEnum.Buy,
            Quantity = 5m,
            UnitPrice = 200m,
            Fees = 1m
        });

        await db.SaveChangesAsync();

        var transactionService = new ClientFinanceTransactionService(
            serviceProvider,
            Mock.Of<IClientFinanceAssetSupportService>(),
            Mock.Of<IPortfolioService>());

        await transactionService.DeleteTransactionAsync("tx-buy-2");
        await transactionService.DeleteTransactionAsync("tx-buy-2");

        var reloadedUserAsset = await db.UserAssets.AsNoTracking().SingleAsync(x => x.Id == userAssetId);
        Assert.Equal(0m, reloadedUserAsset.Quantity);
    }
}
