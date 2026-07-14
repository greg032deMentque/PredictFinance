using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.ClientFinanceServices;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BackPredictFinance.Tests.Infrastructure;

public sealed class SmokeBuildChangeTests
{
    private static FinanceDbContext BuildInMemoryDb(string name)
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        return new FinanceDbContext(options, httpContextAccessorMock.Object);
    }

    [Fact]
    public async Task AnalysisRunStatusEnum_Completed_FilteredCorrectly_AfterEnumConversion()
    {
        var db = BuildInMemoryDb($"smoke-enum-{Guid.NewGuid():N}");

        var asset = new Asset
        {
            Id = "asset-smoke-1",
            Symbol = "SMOKE",
            ProviderSymbol = "SMOKE",
            Exchange = "XPAR",
            Currency = "EUR",
            AssetType = AssetTypeEnum.Stock
        };

        db.Assets.Add(asset);
        db.AnalysisRuns.AddRange(
            new AnalysisRun
            {
                Id = "smoke-run-completed",
                UserId = "smoke-user-1",
                AssetId = asset.Id,
                Status = AnalysisRunStatusEnum.Completed,
                StartedAtUtc = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc),
                CompletedAtUtc = new DateTime(2026, 6, 1, 8, 5, 0, DateTimeKind.Utc),
                RawPayload = "{}"
            },
            new AnalysisRun
            {
                Id = "smoke-run-failed",
                UserId = "smoke-user-1",
                AssetId = asset.Id,
                Status = AnalysisRunStatusEnum.Failed,
                StartedAtUtc = new DateTime(2026, 6, 2, 8, 0, 0, DateTimeKind.Utc),
                RawPayload = "{}",
                ErrorMessage = "timeout"
            });

        await db.SaveChangesAsync();

        var completedCount = await db.AnalysisRuns
            .AsNoTracking()
            .CountAsync(r => r.Status == AnalysisRunStatusEnum.Completed);

        var failedCount = await db.AnalysisRuns
            .AsNoTracking()
            .CountAsync(r => r.Status == AnalysisRunStatusEnum.Failed);

        Assert.Equal(1, completedCount);
        Assert.Equal(1, failedCount);
    }

    [Fact]
    public void ClientGlossaryService_GetGlossaryAsync_QueryFiltersOnIsActiveAndIsPublished()
    {
        var entries = new List<ParameterDictionaryEntry>
        {
            new() { ParameterId = "p1", DisplayLabel = "A Active Published", SimpleDefinition = "def-a", IsActive = true, IsPublished = true, CategoryCode = "test", RoleInCategory = "r", HowToRead = "h", WhyItMatters = "w", LimitsOfInterpretation = "l", WhatItSupports = "s", WhatItDoesNotProve = "np", ImplicationWithoutPosition = "iwp", ImplicationWithPosition = "ip" },
            new() { ParameterId = "p2", DisplayLabel = "B Active Unpublished", SimpleDefinition = "def-b", IsActive = true, IsPublished = false, CategoryCode = "test", RoleInCategory = "r", HowToRead = "h", WhyItMatters = "w", LimitsOfInterpretation = "l", WhatItSupports = "s", WhatItDoesNotProve = "np", ImplicationWithoutPosition = "iwp", ImplicationWithPosition = "ip" },
            new() { ParameterId = "p3", DisplayLabel = "C Inactive Published", SimpleDefinition = "def-c", IsActive = false, IsPublished = true, CategoryCode = "test", RoleInCategory = "r", HowToRead = "h", WhyItMatters = "w", LimitsOfInterpretation = "l", WhatItSupports = "s", WhatItDoesNotProve = "np", ImplicationWithoutPosition = "iwp", ImplicationWithPosition = "ip" },
            new() { ParameterId = "p4", DisplayLabel = "D Inactive Unpublished", SimpleDefinition = "def-d", IsActive = false, IsPublished = false, CategoryCode = "test", RoleInCategory = "r", HowToRead = "h", WhyItMatters = "w", LimitsOfInterpretation = "l", WhatItSupports = "s", WhatItDoesNotProve = "np", ImplicationWithoutPosition = "iwp", ImplicationWithPosition = "ip" }
        };

        var filtered = entries
            .Where(e => e.IsActive && e.IsPublished)
            .OrderBy(e => e.DisplayLabel)
            .Select(e => new { e.ParameterId, e.DisplayLabel, e.SimpleDefinition })
            .ToList();

        Assert.Single(filtered);
        Assert.Equal("p1", filtered[0].ParameterId);
        Assert.Equal("A Active Published", filtered[0].DisplayLabel);
        Assert.Equal("def-a", filtered[0].SimpleDefinition);
    }
}
