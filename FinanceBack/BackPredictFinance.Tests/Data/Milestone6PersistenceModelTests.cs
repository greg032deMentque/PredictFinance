using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using Microsoft.Extensions.DependencyInjection;

using BackPredictFinance.Tests.Infrastructure;

namespace BackPredictFinance.Tests.Data;

public sealed class Milestone6PersistenceModelTests : IClassFixture<ApiIntegrationTestFactory>
{
    private readonly ApiIntegrationTestFactory _factory;

    public Milestone6PersistenceModelTests(ApiIntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void RecommendationWordingScenario_Model_DefinesUniqueScenarioCodePerVersion()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        var entityType = dbContext.Model.FindEntityType(typeof(RecommendationWordingScenario));

        Assert.NotNull(entityType);
        Assert.Contains(entityType!.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(["WordingVersionId", "ScenarioCode"]));
    }
}
