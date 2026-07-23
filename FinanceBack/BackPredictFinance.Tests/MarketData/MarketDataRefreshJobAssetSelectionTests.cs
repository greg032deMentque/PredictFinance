using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.BackgroundJobs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BackPredictFinance.Tests.MarketData;

public sealed class MarketDataRefreshJobAssetSelectionTests
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

    private static User BuildUser(string userId) => new()
    {
        Id = userId,
        UserName = $"{userId}@test.local",
        Email = $"{userId}@test.local",
        NormalizedEmail = $"{userId}@test.local".ToUpperInvariant(),
        NormalizedUserName = $"{userId}@test.local".ToUpperInvariant(),
        IsActive = true,
        AlertLevelCrossedEnabled = true,
        AlertPatternStateChangeEnabled = true,
        AlertDataStaleEnabled = true
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

    [Fact]
    public async Task ResolveTrackedAssetIds_WhenAssetInWatchlist_IsIncluded()
    {
        var dbName = $"refresh-watchlist-{Guid.NewGuid():N}";
        var db = BuildInMemoryDb(dbName);

        var user = BuildUser("user-refresh-1");
        var asset = BuildAsset("asset-refresh-1");

        db.Users.Add(user);
        db.Assets.Add(asset);
        db.UserAssets.Add(new UserAsset
        {
            UserId = user.Id,
            AssetId = asset.Id,
            Quantity = 10m
        });
        await db.SaveChangesAsync();

        var result = await MarketDataRefreshJob.ResolveTrackedAssetIdsAsync(db, CancellationToken.None);

        Assert.Contains("asset-refresh-1", result);
    }

    [Fact]
    public async Task ResolveTrackedAssetIds_WhenAssetHasStillOpenSignal_IsIncluded()
    {
        var dbName = $"refresh-stillopen-{Guid.NewGuid():N}";
        var db = BuildInMemoryDb(dbName);

        var user = BuildUser("user-refresh-2");
        var asset = BuildAsset("asset-refresh-2");

        var run = new AnalysisRun
        {
            Id = "run-refresh-2",
            UserId = user.Id,
            AssetId = asset.Id,
            Status = AnalysisRunStatusEnum.Completed,
            StartedAtUtc = DateTime.UtcNow.AddDays(-3),
            RawPayload = "{}"
        };

        var decision = new DecisionSignal
        {
            Id = "decision-refresh-2",
            AnalysisRunId = run.Id,
            Action = RecommendationActionEnum.Buy,
            IsActionable = true,
            Confidence = 0.75m,
            HorizonDays = 10
        };

        var assessment = new PatternAssessment
        {
            Id = "pa-refresh-2",
            AnalysisRunId = run.Id,
            PatternId = "RECTANGLE_CONTINUATION",
            Phase = "bullish_breakout_confirmed",
            ProgressStatus = PatternProgressStatusEnum.Confirmed,
            Probability = 0.75m,
            Confidence = 0.75m,
            IsPrimary = true
        };

        var outcome = new SignalOutcome
        {
            Id = "so-refresh-2",
            AnalysisRunId = run.Id,
            PatternAssessmentId = assessment.Id,
            DecisionSignalId = decision.Id,
            Outcome = SignalOutcomeEnum.StillOpen,
            EvaluationWindowDays = 10,
            EvaluatedAtUtc = DateTime.UtcNow,
            PolicyVersion = "test",
            ConfidenceLabel = ConfidenceLabelEnum.High
        };

        run.DecisionSignal = decision;

        db.Users.Add(user);
        db.Assets.Add(asset);
        db.AnalysisRuns.Add(run);
        db.DecisionSignals.Add(decision);
        db.PatternAssessments.Add(assessment);
        db.SignalOutcomes.Add(outcome);
        await db.SaveChangesAsync();

        var result = await MarketDataRefreshJob.ResolveTrackedAssetIdsAsync(db, CancellationToken.None);

        Assert.Contains("asset-refresh-2", result);
    }

    [Fact]
    public async Task ResolveTrackedAssetIds_WhenAssetNotInWatchlistAndNoOpenSignal_IsExcluded()
    {
        var dbName = $"refresh-excluded-{Guid.NewGuid():N}";
        var db = BuildInMemoryDb(dbName);

        var user = BuildUser("user-refresh-3");
        var asset = BuildAsset("asset-refresh-3");

        db.Users.Add(user);
        db.Assets.Add(asset);
        await db.SaveChangesAsync();

        var result = await MarketDataRefreshJob.ResolveTrackedAssetIdsAsync(db, CancellationToken.None);

        Assert.DoesNotContain("asset-refresh-3", result);
    }

    [Fact]
    public async Task ResolveTrackedAssetIds_WhenAssetInBothWatchlistAndOpenSignal_AppearsOnce()
    {
        var dbName = $"refresh-dedup-{Guid.NewGuid():N}";
        var db = BuildInMemoryDb(dbName);

        var user = BuildUser("user-refresh-4");
        var asset = BuildAsset("asset-refresh-4");

        var run = new AnalysisRun
        {
            Id = "run-refresh-4",
            UserId = user.Id,
            AssetId = asset.Id,
            Status = AnalysisRunStatusEnum.Completed,
            StartedAtUtc = DateTime.UtcNow.AddDays(-2),
            RawPayload = "{}"
        };

        var decision = new DecisionSignal
        {
            Id = "decision-refresh-4",
            AnalysisRunId = run.Id,
            Action = RecommendationActionEnum.Buy,
            IsActionable = true,
            Confidence = 0.80m,
            HorizonDays = 7
        };

        var assessment = new PatternAssessment
        {
            Id = "pa-refresh-4",
            AnalysisRunId = run.Id,
            PatternId = "RECTANGLE_CONTINUATION",
            Phase = "bullish_breakout_confirmed",
            ProgressStatus = PatternProgressStatusEnum.Confirmed,
            Probability = 0.80m,
            Confidence = 0.80m,
            IsPrimary = true
        };

        var outcome = new SignalOutcome
        {
            Id = "so-refresh-4",
            AnalysisRunId = run.Id,
            PatternAssessmentId = assessment.Id,
            DecisionSignalId = decision.Id,
            Outcome = SignalOutcomeEnum.StillOpen,
            EvaluationWindowDays = 7,
            EvaluatedAtUtc = DateTime.UtcNow,
            PolicyVersion = "test",
            ConfidenceLabel = ConfidenceLabelEnum.High
        };

        run.DecisionSignal = decision;

        db.Users.Add(user);
        db.Assets.Add(asset);
        db.UserAssets.Add(new UserAsset { UserId = user.Id, AssetId = asset.Id, Quantity = 5m });
        db.AnalysisRuns.Add(run);
        db.DecisionSignals.Add(decision);
        db.PatternAssessments.Add(assessment);
        db.SignalOutcomes.Add(outcome);
        await db.SaveChangesAsync();

        var result = await MarketDataRefreshJob.ResolveTrackedAssetIdsAsync(db, CancellationToken.None);

        Assert.Single(result.Where(id => id == "asset-refresh-4"));
    }

    [Fact]
    public async Task ResolveTrackedAssetIds_WhenSignalIsTerminal_AssetExcludedFromOpenSignals()
    {
        var dbName = $"refresh-terminal-{Guid.NewGuid():N}";
        var db = BuildInMemoryDb(dbName);

        var user = BuildUser("user-refresh-5");
        var assetWithTerminal = BuildAsset("asset-refresh-5");
        var assetWithOpen = BuildAsset("asset-refresh-5b");

        var runTerminal = new AnalysisRun
        {
            Id = "run-refresh-5",
            UserId = user.Id,
            AssetId = assetWithTerminal.Id,
            Status = AnalysisRunStatusEnum.Completed,
            StartedAtUtc = DateTime.UtcNow.AddDays(-10),
            RawPayload = "{}"
        };

        var decisionTerminal = new DecisionSignal
        {
            Id = "decision-refresh-5",
            AnalysisRunId = runTerminal.Id,
            Action = RecommendationActionEnum.Buy,
            IsActionable = true,
            Confidence = 0.70m,
            HorizonDays = 5
        };

        var assessmentTerminal = new PatternAssessment
        {
            Id = "pa-refresh-5",
            AnalysisRunId = runTerminal.Id,
            PatternId = "RECTANGLE_CONTINUATION",
            Phase = "bullish_breakout_confirmed",
            ProgressStatus = PatternProgressStatusEnum.Confirmed,
            Probability = 0.70m,
            Confidence = 0.70m,
            IsPrimary = true
        };

        var terminalOutcome = new SignalOutcome
        {
            Id = "so-refresh-5",
            AnalysisRunId = runTerminal.Id,
            PatternAssessmentId = assessmentTerminal.Id,
            DecisionSignalId = decisionTerminal.Id,
            Outcome = SignalOutcomeEnum.TargetHit,
            EvaluationWindowDays = 5,
            EvaluatedAtUtc = DateTime.UtcNow,
            PolicyVersion = "test",
            ConfidenceLabel = ConfidenceLabelEnum.Medium
        };

        runTerminal.DecisionSignal = decisionTerminal;

        var runOpen = new AnalysisRun
        {
            Id = "run-refresh-5b",
            UserId = user.Id,
            AssetId = assetWithOpen.Id,
            Status = AnalysisRunStatusEnum.Completed,
            StartedAtUtc = DateTime.UtcNow.AddDays(-1),
            RawPayload = "{}"
        };

        var decisionOpen = new DecisionSignal
        {
            Id = "decision-refresh-5b",
            AnalysisRunId = runOpen.Id,
            Action = RecommendationActionEnum.Buy,
            IsActionable = true,
            Confidence = 0.78m,
            HorizonDays = 8
        };

        var assessmentOpen = new PatternAssessment
        {
            Id = "pa-refresh-5b",
            AnalysisRunId = runOpen.Id,
            PatternId = "RECTANGLE_CONTINUATION",
            Phase = "bullish_breakout_confirmed",
            ProgressStatus = PatternProgressStatusEnum.Monitoring,
            Probability = 0.78m,
            Confidence = 0.78m,
            IsPrimary = true
        };

        var openOutcome = new SignalOutcome
        {
            Id = "so-refresh-5b",
            AnalysisRunId = runOpen.Id,
            PatternAssessmentId = assessmentOpen.Id,
            DecisionSignalId = decisionOpen.Id,
            Outcome = SignalOutcomeEnum.StillOpen,
            EvaluationWindowDays = 8,
            EvaluatedAtUtc = DateTime.UtcNow,
            PolicyVersion = "test",
            ConfidenceLabel = ConfidenceLabelEnum.High
        };

        runOpen.DecisionSignal = decisionOpen;

        db.Users.Add(user);
        db.Assets.Add(assetWithTerminal);
        db.Assets.Add(assetWithOpen);
        db.AnalysisRuns.Add(runTerminal);
        db.AnalysisRuns.Add(runOpen);
        db.DecisionSignals.Add(decisionTerminal);
        db.DecisionSignals.Add(decisionOpen);
        db.PatternAssessments.Add(assessmentTerminal);
        db.PatternAssessments.Add(assessmentOpen);
        db.SignalOutcomes.Add(terminalOutcome);
        db.SignalOutcomes.Add(openOutcome);
        await db.SaveChangesAsync();

        var result = await MarketDataRefreshJob.ResolveTrackedAssetIdsAsync(db, CancellationToken.None);

        Assert.DoesNotContain("asset-refresh-5", result);
        Assert.Contains("asset-refresh-5b", result);
    }
}
