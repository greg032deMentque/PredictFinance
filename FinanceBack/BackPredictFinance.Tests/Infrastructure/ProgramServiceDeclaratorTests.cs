using System.Reflection;
using BackPredictFinance.API.ProgramSubFiles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackPredictFinance.Tests.Infrastructure;

public sealed class ProgramServiceDeclaratorTests
{
    private static IConfiguration BuildEmptyConfiguration() =>
        new ConfigurationBuilder().Build();

    [Fact]
    public void ServicesDeclarator_RegistersEveryServiceDependencyUsedByControllers()
    {
        var services = new ServiceCollection();

        ProgramServiceDeclarator.ServicesDeclarator(services, BuildEmptyConfiguration());

        var controllerDependencyTypes = typeof(ProgramServiceDeclarator).Assembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .SelectMany(type => type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .Where(type => type.IsInterface)
            .Where(type => type.Namespace is not null
                && (type.Namespace.StartsWith("BackPredictFinance.Services", StringComparison.Ordinal)
                    || type.Namespace.StartsWith("BackPredictFinance.Patterns", StringComparison.Ordinal)))
            .Distinct()
            .OrderBy(type => type.FullName)
            .ToList();

        var missingTypes = controllerDependencyTypes
            .Where(type => !services.Any(descriptor => descriptor.ServiceType == type))
            .Select(type => type.FullName)
            .ToList();

        Assert.Empty(missingTypes);
    }

    [Fact]
    public void ServicesDeclarator_RegistersCoreClientFinanceAndGovernanceServices()
    {
        var services = new ServiceCollection();

        ProgramServiceDeclarator.ServicesDeclarator(services, BuildEmptyConfiguration());

        var requiredServiceTypes = new[]
        {
            "BackPredictFinance.Services.AuthServices.IAccountService",
            "BackPredictFinance.Services.AuthServices.ICurrentUserSessionService",
            "BackPredictFinance.Services.UserServices.IUserService",
            "BackPredictFinance.Services.UserServices.IUserRoleDataService",
            "BackPredictFinance.Services.ClientFinanceServices.IClientFinanceService",
            "BackPredictFinance.Services.ClientFinanceServices.IClientFinanceInstrumentDetailService",
            "BackPredictFinance.Services.ClientFinanceServices.IClientFinanceContactService",
            "BackPredictFinance.Services.ClientFinanceServices.IClientFinanceSnapshotComparisonService",
            "BackPredictFinance.Services.Fundamentals.IFundamentalScoringService",
            "BackPredictFinance.Services.AdminGovernance.IAdminOverviewService",
            "BackPredictFinance.Services.AdminGovernance.IAdminWordingVersionService",
            "BackPredictFinance.Services.Notifications.INotificationCenterService",
            "BackPredictFinance.Patterns.IAnalysisPatternRegistry"
        };

        var registeredTypeNames = services
            .Select(descriptor => descriptor.ServiceType.FullName)
            .Where(typeName => !string.IsNullOrWhiteSpace(typeName))
            .ToHashSet(StringComparer.Ordinal);

        var missingTypes = requiredServiceTypes
            .Where(typeName => !registeredTypeNames.Contains(typeName))
            .ToList();

        Assert.Empty(missingTypes);
    }
}
