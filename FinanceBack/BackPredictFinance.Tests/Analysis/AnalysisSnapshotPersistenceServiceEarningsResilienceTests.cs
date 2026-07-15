using System.Reflection;
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
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace BackPredictFinance.Tests.Analysis;

public sealed class AnalysisSnapshotPersistenceServiceEarningsResilienceTests
{
    private static async Task<(AnalysisSnapshotPersistenceService Service, Mock<IFundamentalsProvider> ProviderMock, IAsyncDisposable Scope)> BuildContextAsync()
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

        return (service, providerMock, scope);
    }

    private static async Task<DateTime?> InvokeTryResolveEarningsDateUtcAsync(
        AnalysisSnapshotPersistenceService service,
        string symbol,
        CancellationToken ct = default)
    {
        var method = typeof(AnalysisSnapshotPersistenceService).GetMethod(
            "TryResolveEarningsDateUtcAsync",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingMethodException(nameof(AnalysisSnapshotPersistenceService), "TryResolveEarningsDateUtcAsync");

        var task = (Task<DateTime?>)method.Invoke(service, [symbol, ct])!;
        return await task;
    }

    [Fact]
    public async Task TryResolveEarningsDateUtcAsync_ProviderReturnsFundamentals_ReturnsEarningsDate()
    {
        var (service, providerMock, scope) = await BuildContextAsync();
        await using var _ = scope;

        var earningsDate = DateTime.UtcNow.AddDays(10);
        providerMock
            .Setup(x => x.GetFundamentalsAsync("AIR.PA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MarketFundamentalData { Symbol = "AIR.PA", EarningsDate = earningsDate });

        var result = await InvokeTryResolveEarningsDateUtcAsync(service, "AIR.PA");

        Assert.Equal(earningsDate, result);
    }

    [Fact]
    public async Task TryResolveEarningsDateUtcAsync_ProviderThrowsTimeoutRejectedException_ReturnsNullWithoutThrowing()
    {
        var (service, providerMock, scope) = await BuildContextAsync();
        await using var _ = scope;

        providerMock
            .Setup(x => x.GetFundamentalsAsync("AIR.PA", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutRejectedException());

        var result = await InvokeTryResolveEarningsDateUtcAsync(service, "AIR.PA");

        Assert.Null(result);
    }

    [Fact]
    public async Task TryResolveEarningsDateUtcAsync_ProviderThrowsBrokenCircuitException_ReturnsNullWithoutThrowing()
    {
        var (service, providerMock, scope) = await BuildContextAsync();
        await using var _ = scope;

        providerMock
            .Setup(x => x.GetFundamentalsAsync("AIR.PA", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BrokenCircuitException());

        var result = await InvokeTryResolveEarningsDateUtcAsync(service, "AIR.PA");

        Assert.Null(result);
    }

    [Fact]
    public async Task TryResolveEarningsDateUtcAsync_ProviderThrowsOperationCanceledException_ReturnsNullWithoutThrowing()
    {
        var (service, providerMock, scope) = await BuildContextAsync();
        await using var _ = scope;

        providerMock
            .Setup(x => x.GetFundamentalsAsync("AIR.PA", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("Simule un timeout HTTP interne au provider."));

        var result = await InvokeTryResolveEarningsDateUtcAsync(service, "AIR.PA");

        Assert.Null(result);
    }
}
