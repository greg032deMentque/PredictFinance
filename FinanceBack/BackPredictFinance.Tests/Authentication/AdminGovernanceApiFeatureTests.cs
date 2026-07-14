using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.AdminViewModels.Overview;
using BackPredictFinance.ViewModels.AdminViewModels.Kpi;
using BackPredictFinance.ViewModels.AdminViewModels.SignalQuality;
using BackPredictFinance.ViewModels.AdminViewModels.Instruments;
using BackPredictFinance.ViewModels.AdminViewModels.Pea;
using BackPredictFinance.ViewModels.AdminViewModels.ScoringPolicy;
using BackPredictFinance.ViewModels.AdminViewModels.ParameterDictionary;
using BackPredictFinance.ViewModels.AdminViewModels.Wording;
using BackPredictFinance.ViewModels.AdminViewModels.SnapshotAudit;
using BackPredictFinance.ViewModels.AdminViewModels.DataQuality;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using BackPredictFinance.Tests.Infrastructure;

namespace BackPredictFinance.Tests.Authentication;

public sealed class AdminGovernanceApiFeatureTests : IClassFixture<ApiIntegrationTestFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly ApiIntegrationTestFactory _factory;

    public AdminGovernanceApiFeatureTests(ApiIntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminOverview_ReturnsForbidden_ForStandardUser()
    {
        var client = _factory.CreateAuthenticatedClient(ApiIntegrationTestFactory.StandardUserId, UserRoleEnum.User);

        var response = await client.GetAsync("/api/admin/overview");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminOverview_ReturnsGovernedCounts_ForAdmin()
    {
        await SeedAdminGovernanceDataAsync();
        var client = _factory.CreateAuthenticatedClient(ApiIntegrationTestFactory.AdminUserId, UserRoleEnum.Admin);

        var response = await client.GetAsync("/api/admin/overview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AdminOverviewViewModel>(JsonOptions);
        Assert.NotNull(payload);
        Assert.True(payload!.TotalUsers >= 3);
        Assert.Equal(1, payload.TotalAssets);
        Assert.Equal(2, payload.TotalAnalysisRuns);
        Assert.Equal(1, payload.CompletedAnalysisRuns);
        Assert.Equal(1, payload.FailedAnalysisRuns);
        Assert.Equal(1, payload.ConfirmedEligiblePeaEntries);
        Assert.Equal(0, payload.UnknownPeaEntries);
        Assert.True(payload.PublishedParameterEntries >= 1);
    }

    [Fact]
    public async Task AdminPeaRegistry_ReturnsTraceableRegistryProjection_ForAdmin()
    {
        await SeedAdminGovernanceDataAsync();
        var client = _factory.CreateAuthenticatedClient(ApiIntegrationTestFactory.AdminUserId, UserRoleEnum.Admin);

        var response = await client.GetAsync("/api/admin/pea-registry");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<AdminPeaRegistryItemViewModel>>(JsonOptions);
        Assert.NotNull(payload);
        var entry = Assert.Single(payload!);
        Assert.Equal("AIRP", entry.Symbol);
        Assert.Equal("PEA_FR_EQUITIES", entry.UniverseId);
        Assert.Equal(PeaEligibilityStatusEnum.ConfirmedEligible, entry.EligibilityStatus);
        Assert.Equal(PeaEligibilitySourceTypeEnum.ManualRegistry, entry.SourceType);
        Assert.Equal("REG-001", entry.SourceReference);
        Assert.Equal("PEA_REGISTRY_V1", entry.PolicyVersion);
    }

    [Fact]
    public async Task AdminSnapshotAudit_ExposesRecentAndDetail_ForAdmin()
    {
        await SeedAdminGovernanceDataAsync();
        var client = _factory.CreateAuthenticatedClient(ApiIntegrationTestFactory.AdminUserId, UserRoleEnum.Admin);

        var recentResponse = await client.GetAsync("/api/admin/snapshot-audit?take=10");

        Assert.Equal(HttpStatusCode.OK, recentResponse.StatusCode);
        var recentPayload = await recentResponse.Content.ReadFromJsonAsync<List<SnapshotAuditItemViewModel>>(JsonOptions);
        Assert.NotNull(recentPayload);
        var completedItem = recentPayload!.Single(x => x.AnalysisRunId == "analysis-completed-1");
        Assert.Equal("trace-001", completedItem.TraceId);
        Assert.Equal("RectangleContinuation", completedItem.PrimaryPatternId);
        Assert.Equal(["RectangleContinuation", "BullFlagContinuation"], completedItem.ExecutedPatternIds);
        Assert.Equal("Buy", completedItem.RecommendationAction);
        Assert.Equal(ModelStatusEnum.Go, completedItem.ModelStatus);

        var detailResponse = await client.GetAsync("/api/admin/snapshot-audit/analysis-completed-1");

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detailPayload = await detailResponse.Content.ReadFromJsonAsync<SnapshotAuditDetailViewModel>(JsonOptions);
        Assert.NotNull(detailPayload);
        Assert.Equal("trace-001", detailPayload!.TraceId);
        Assert.Equal("ENGINE_V1", detailPayload.AnalysisEngineVersion);
        Assert.Equal("Buy", detailPayload.RecommendationAction);
        Assert.Equal("Buy", detailPayload.DecisionAction);
        Assert.Equal("Pattern validated", detailPayload.DecisionSummary);
    }

    [Fact]
    public async Task AdminWordingVersions_ReturnsForbidden_ForStandardUser()
    {
        var client = _factory.CreateAuthenticatedClient(ApiIntegrationTestFactory.StandardUserId, UserRoleEnum.User);

        var response = await client.GetAsync("/api/admin/wording-versions");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminWordingVersions_ReturnsActiveGovernance_ForAdmin()
    {
        var client = _factory.CreateAuthenticatedClient(ApiIntegrationTestFactory.AdminUserId, UserRoleEnum.Admin);

        var listResponse = await client.GetAsync("/api/admin/wording-versions");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listPayload = await listResponse.Content.ReadFromJsonAsync<List<AdminWordingVersionListItemViewModel>>(JsonOptions);
        Assert.NotNull(listPayload);
        var activeVersion = Assert.Single(listPayload!);
        Assert.Equal("REC_WORDING_V1", activeVersion.WordingVersionId);
        Assert.True(activeVersion.IsActive);
        Assert.Equal(7, activeVersion.ScenarioCount);
        Assert.Equal("analysis-v1-policy@prompt3", activeVersion.PublicationState.RecommendationPolicyVersion);
        Assert.Equal("analysis-v1-explanation@prompt5", activeVersion.PublicationState.ExplanationPolicyVersion);

        var detailResponse = await client.GetAsync("/api/admin/wording-scenarios/HELD_REINFORCE");

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detailPayload = await detailResponse.Content.ReadFromJsonAsync<AdminWordingVersionDetailViewModel>(JsonOptions);
        Assert.NotNull(detailPayload);
        Assert.Equal("REC_WORDING_V1", detailPayload!.WordingVersionId);
        Assert.Equal("HELD_REINFORCE", detailPayload.Scenario.ScenarioCode);
        Assert.Equal(RecommendationKind.Reinforce, detailPayload.Scenario.RecommendationKind);
        Assert.Equal(HoldingStatusEnum.Held, detailPayload.Scenario.HoldingStatus);
        Assert.Equal("REINFORCE", detailPayload.Scenario.ActionVerbFamilyCode);
        Assert.Equal([RecommendationStrengthEnum.Low, RecommendationStrengthEnum.Medium, RecommendationStrengthEnum.High], detailPayload.Scenario.SupportedStrengths);
    }

    [Fact]
    public async Task AdminDataQuality_ReturnsGovernedIssueCounts_ForAdmin()
    {
        await SeedAdminGovernanceDataAsync();
        var client = _factory.CreateAuthenticatedClient(ApiIntegrationTestFactory.AdminUserId, UserRoleEnum.Admin);

        var response = await client.GetAsync("/api/admin/data-quality");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AdminDataQualityViewModel>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(0, payload!.AssetsMissingProfileSyncCount);
        Assert.Equal(0, payload.AssetsWithoutPeaRegistryCount);
        Assert.Equal(0, payload.PeaRegistryUnknownStatusCount);
        Assert.Equal(0, payload.CompletedAnalysisRunsWithoutModelSnapshotCount);
        Assert.Equal(0, payload.CompletedAnalysisRunsWithoutDecisionSignalCount);
    }

    private async Task SeedAdminGovernanceDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

        if (await dbContext.Assets.AnyAsync(x => x.Id == "asset-airp"))
        {
            return;
        }

        var asset = new Asset
        {
            Id = "asset-airp",
            Symbol = "AIRP",
            ProviderSymbol = "AIRP.PA",
            Name = "Air France Test",
            Exchange = "XPAR",
            Currency = "EUR",
            Country = "FR",
            Sector = "Industrials",
            Category = "Equity",
            Summary = "Test asset",
            LastProfileSyncUtc = DateTime.UtcNow.AddDays(-1),
            AssetType = AssetTypeEnum.Stock
        };

        var peaEntry = new AssetPeaEligibility
        {
            Id = "pea-1",
            AssetId = asset.Id,
            UniverseId = "PEA_FR_EQUITIES",
            EligibilityStatus = PeaEligibilityStatusEnum.ConfirmedEligible,
            SourceType = PeaEligibilitySourceTypeEnum.ManualRegistry,
            SourceReference = "REG-001",
            CheckedUtc = DateTime.UtcNow.AddDays(-1),
            PolicyVersion = "PEA_REGISTRY_V1",
            ReviewerNote = "Validated"
        };



        var completedRun = new AnalysisRun
        {
            Id = "analysis-completed-1",
            UserId = ApiIntegrationTestFactory.StandardUserId,
            AssetId = asset.Id,
            Status = AnalysisRunStatusEnum.Completed,
            StartedAtUtc = new DateTime(2026, 4, 10, 8, 0, 0, DateTimeKind.Utc),
            CompletedAtUtc = new DateTime(2026, 4, 10, 8, 5, 0, DateTimeKind.Utc),
            RawPayload = JsonSerializer.Serialize(new
            {
                TraceId = "trace-001",
                RequestedPatternIds = new[] { "RectangleContinuation" },
                ExecutedPatternIds = new[] { "RectangleContinuation", "BullFlagContinuation" },
                PrimaryPatternId = "RectangleContinuation",
                Recommendation = new
                {
                    Action = "Buy"
                },
                RecommendationPolicyVersion = "REC_V1",
                ExplanationPolicyVersion = "EXP_V1",
                AnalysisEngineVersion = "ENGINE_V1"
            }),
            ErrorMessage = (string?)null
        };

        var completedDecision = new DecisionSignal
        {
            Id = "decision-1",
            AnalysisRunId = completedRun.Id,
            Action = RecommendationActionEnum.Buy,
            IsActionable = true,
            Confidence = 0.82m,
            HorizonDays = 20,
            Reason = "Pattern validated"
        };

        var completedModel = new ModelSnapshot
        {
            Id = "model-1",
            AnalysisRunId = completedRun.Id,
            ModelStatus = ModelStatusEnum.Go,
            ModelMessage = "Ready",
            ModelVersion = "ENGINE_V1",
            Precision = 0.81m,
            F1 = 0.77m,
            RocAuc = 0.79m,
            PositiveSamples = 42,
            SelectedThreshold = 0.66m
        };

        var failedRun = new AnalysisRun
        {
            Id = "analysis-failed-1",
            UserId = ApiIntegrationTestFactory.StandardUserId,
            AssetId = asset.Id,
            Status = AnalysisRunStatusEnum.Failed,
            StartedAtUtc = new DateTime(2026, 4, 11, 8, 0, 0, DateTimeKind.Utc),
            CompletedAtUtc = new DateTime(2026, 4, 11, 8, 1, 0, DateTimeKind.Utc),
            RawPayload = "{}",
            ErrorMessage = "provider timeout"
        };

        var paramEntry = new ParameterDictionaryEntry
        {
            ParameterId = "param-governance-1",
            CategoryCode = "pattern",
            DisplayLabel = "Test Parameter",
            RoleInCategory = "indicator",
            SimpleDefinition = "A test parameter for governance.",
            HowToRead = "high = strong signal",
            WhyItMatters = "confirms pattern",
            LimitsOfInterpretation = "none",
            WhatItSupports = "decision",
            WhatItDoesNotProve = "certainty",
            ImplicationWithoutPosition = "observe",
            ImplicationWithPosition = "monitor",
            IsActive = true,
            IsPublished = true
        };

        await dbContext.Assets.AddAsync(asset);
        await dbContext.AssetPeaEligibilities.AddAsync(peaEntry);
        await dbContext.AnalysisRuns.AddRangeAsync(completedRun, failedRun);
        await dbContext.DecisionSignals.AddAsync(completedDecision);
        await dbContext.ModelSnapshots.AddAsync(completedModel);
        await dbContext.ParameterDictionaryEntries.AddAsync(paramEntry);
        await dbContext.SaveChangesAsync();
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
