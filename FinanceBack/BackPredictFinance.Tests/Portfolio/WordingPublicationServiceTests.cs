using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.AdminGovernance;
using Microsoft.Extensions.DependencyInjection;

using BackPredictFinance.Tests.Infrastructure;

namespace BackPredictFinance.Tests.Portfolio;

public sealed class WordingPublicationServiceTests : IClassFixture<ApiIntegrationTestFactory>
{
    private readonly ApiIntegrationTestFactory _factory;

    public WordingPublicationServiceTests(ApiIntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ResolveScenarioAsync_ReturnsDeterministicTemplate_ForClosedRecommendationState()
    {
        using var scope = _factory.Services.CreateScope();
        var wordingPublicationService = scope.ServiceProvider.GetRequiredService<IWordingPublicationService>();

        var first = await wordingPublicationService.ResolveScenarioAsync(RecommendationKind.Buy, HoldingStatusEnum.NotHeld);
        var second = await wordingPublicationService.ResolveScenarioAsync(RecommendationKind.Buy, HoldingStatusEnum.NotHeld);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal("NOT_HELD_BUY", first!.ScenarioCode);
        Assert.Equal(first.ScenarioCode, second!.ScenarioCode);
        Assert.Equal(first.ActionVerbFamilyCode, second.ActionVerbFamilyCode);
        Assert.Equal(first.TemplateSummary, second.TemplateSummary);
        Assert.Equal([RecommendationStrengthEnum.Low, RecommendationStrengthEnum.Medium, RecommendationStrengthEnum.High], first.SupportedStrengths);
    }
}
