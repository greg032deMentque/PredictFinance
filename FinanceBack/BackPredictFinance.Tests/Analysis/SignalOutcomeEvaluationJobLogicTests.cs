using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.BackgroundJobs;
using BackPredictFinance.Services.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BackPredictFinance.Tests.Analysis;

public sealed class SignalOutcomeEvaluationJobLogicTests
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
        AlertLevelCrossedEnabled = true,
        AlertPatternStateChangeEnabled = true,
        AlertDataStaleEnabled = true,
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

    private static SignalOutcomeEvaluationJob BuildJob(IServiceProvider sp)
    {
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        return new SignalOutcomeEvaluationJob(
            scopeFactory,
            NullLogger<SignalOutcomeEvaluationJob>.Instance);
    }

    private static IServiceProvider BuildServiceProvider(FinanceDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton<FinanceDbContext>(db);
        services.AddSingleton<IProactiveAlertEmitter>(new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance));
        services.AddScoped<FinanceDbContext>(_ => db);
        services.AddScoped<IProactiveAlertEmitter>(_ => new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance));

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var scopeMock = new Mock<IServiceScope>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        serviceProviderMock.Setup(p => p.GetService(typeof(FinanceDbContext))).Returns(db);
        serviceProviderMock.Setup(p => p.GetService(typeof(IProactiveAlertEmitter)))
            .Returns(new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance));

        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
        scopeMock.Setup(s => s.Dispose());
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var rootServices = new ServiceCollection();
        rootServices.AddSingleton(scopeFactoryMock.Object);
        return rootServices.BuildServiceProvider();
    }

    [Fact]
    public async Task EvaluatePendingSignals_WhenTargetHitOnFirstEvaluation_PersistsTargetHitOutcome()
    {
        var dbName = $"signal-expost-{Guid.NewGuid():N}";
        var db = BuildInMemoryDb(dbName);
        var userId = "user-expost-1";
        var assetId = "asset-expost-1";

        var user = BuildUser(userId);
        var asset = BuildAsset(assetId);

        var completedAt = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);
        var run = new AnalysisRun
        {
            Id = "run-expost-1",
            UserId = userId,
            AssetId = assetId,
            Status = AnalysisRunStatusEnum.Completed,
            StartedAtUtc = completedAt.AddMinutes(-5),
            CompletedAtUtc = completedAt,
            RawPayload = "{}"
        };

        var decision = new DecisionSignal
        {
            Id = "decision-expost-1",
            AnalysisRunId = run.Id,
            Action = RecommendationActionEnum.Buy,
            IsActionable = true,
            Confidence = 0.82m,
            HorizonDays = 5
        };

        var assessment = new PatternAssessment
        {
            Id = "pa-expost-1",
            AnalysisRunId = run.Id,
            PatternId = "RECTANGLE_CONTINUATION",
            Phase = "bullish_breakout_confirmed",
            ProgressStatus = PatternProgressStatusEnum.Confirmed,
            Probability = 0.82m,
            Confidence = 0.82m,
            CurrentPrice = 145m,
            TargetPrice = 160m,
            InvalidationPrice = 136m,
            IsPrimary = true
        };

        var windowStart = completedAt.Date;
        var candleInsideWindow = new AssetCandleSnapshot
        {
            Id = "candle-expost-1",
            AssetId = assetId,
            TimestampUtc = windowStart.AddDays(2),
            Interval = "1d",
            Open = 150m,
            High = 162m,
            Low = 149m,
            Close = 161m,
            Volume = 1000m,
            Source = "test"
        };

        run.DecisionSignal = decision;
        db.Users.Add(user);
        db.Assets.Add(asset);
        db.AnalysisRuns.Add(run);
        db.DecisionSignals.Add(decision);
        db.PatternAssessments.Add(assessment);
        db.AssetCandleSnapshots.Add(candleInsideWindow);
        await db.SaveChangesAsync();

        var emitter = new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance);
        var now = windowStart.AddDays(decision.HorizonDays + 2);

        await InvokeEvaluatePendingSignalsAsync(db, emitter, now);

        var outcome = await db.SignalOutcomes
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.PatternAssessmentId == assessment.Id);

        Assert.NotNull(outcome);
        Assert.Equal(SignalOutcomeEnum.TargetHit, outcome!.Outcome);
        Assert.NotNull(outcome.FirstHitAtUtc);
    }

    [Fact]
    public async Task EvaluatePendingSignals_WhenStillOpen_TransitionToTargetHit_EmitsAlertOnce()
    {
        var dbName = $"signal-transition-{Guid.NewGuid():N}";
        var db = BuildInMemoryDb(dbName);
        var userId = "user-expost-2";
        var assetId = "asset-expost-2";

        var user = BuildUser(userId);
        var asset = BuildAsset(assetId);
        var completedAt = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);

        var run = new AnalysisRun
        {
            Id = "run-expost-2",
            UserId = userId,
            AssetId = assetId,
            Status = AnalysisRunStatusEnum.Completed,
            StartedAtUtc = completedAt.AddMinutes(-5),
            CompletedAtUtc = completedAt,
            RawPayload = "{}"
        };

        var decision = new DecisionSignal
        {
            Id = "decision-expost-2",
            AnalysisRunId = run.Id,
            Action = RecommendationActionEnum.Buy,
            IsActionable = true,
            Confidence = 0.75m,
            HorizonDays = 5
        };

        var assessment = new PatternAssessment
        {
            Id = "pa-expost-2",
            AnalysisRunId = run.Id,
            PatternId = "RECTANGLE_CONTINUATION",
            Phase = "bullish_breakout_confirmed",
            ProgressStatus = PatternProgressStatusEnum.Confirmed,
            Probability = 0.75m,
            Confidence = 0.75m,
            CurrentPrice = 145m,
            TargetPrice = 160m,
            InvalidationPrice = 136m,
            IsPrimary = true
        };

        var windowStart = completedAt.Date;

        var existingStillOpen = new SignalOutcome
        {
            Id = "so-expost-2",
            AnalysisRunId = run.Id,
            PatternAssessmentId = assessment.Id,
            DecisionSignalId = decision.Id,
            Outcome = SignalOutcomeEnum.StillOpen,
            EvaluationWindowDays = decision.HorizonDays,
            EvaluatedAtUtc = completedAt.AddDays(1),
            PolicyVersion = "test",
            ConfidenceLabel = ConfidenceLabelEnum.High
        };

        var candleHitsTarget = new AssetCandleSnapshot
        {
            Id = "candle-expost-2",
            AssetId = assetId,
            TimestampUtc = windowStart.AddDays(3),
            Interval = "1d",
            Open = 155m,
            High = 165m,
            Low = 154m,
            Close = 163m,
            Volume = 2000m,
            Source = "test"
        };

        run.DecisionSignal = decision;
        db.Users.Add(user);
        db.Assets.Add(asset);
        db.AnalysisRuns.Add(run);
        db.DecisionSignals.Add(decision);
        db.PatternAssessments.Add(assessment);
        db.SignalOutcomes.Add(existingStillOpen);
        db.AssetCandleSnapshots.Add(candleHitsTarget);
        await db.SaveChangesAsync();

        var emitter = new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance);
        var now = windowStart.AddDays(decision.HorizonDays + 2);

        await InvokeEvaluatePendingSignalsAsync(db, emitter, now);

        var updatedOutcome = await db.SignalOutcomes
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.PatternAssessmentId == assessment.Id);

        Assert.NotNull(updatedOutcome);
        Assert.Equal(SignalOutcomeEnum.TargetHit, updatedOutcome!.Outcome);

        var notifCount = await db.UserNotifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && n.AlertTrigger == AlertTrigger.LevelCrossed);

        Assert.Equal(1, notifCount);
    }

    private static async Task InvokeEvaluatePendingSignalsAsync(
        FinanceDbContext db,
        IProactiveAlertEmitter emitter,
        DateTime now)
    {
        var candidates = await db.PatternAssessments
            .AsNoTracking()
            .Where(pa => pa.IsPrimary
                && pa.AnalysisRun.DecisionSignal != null
                && pa.AnalysisRun.DecisionSignal.IsActionable
                && (!db.SignalOutcomes.Any(so => so.PatternAssessmentId == pa.Id)
                    || db.SignalOutcomes.Any(so =>
                        so.PatternAssessmentId == pa.Id
                        && so.Outcome == SignalOutcomeEnum.StillOpen)))
            .Include(pa => pa.AnalysisRun)
                .ThenInclude(run => run.DecisionSignal)
            .Include(pa => pa.AnalysisRun)
                .ThenInclude(run => run.ModelSnapshot)
            .ToListAsync();

        foreach (var patternAssessment in candidates)
        {
            var previousOutcome = await db.SignalOutcomes
                .FirstOrDefaultAsync(so => so.PatternAssessmentId == patternAssessment.Id);

            var evaluatedOutcome = await EvaluateStaticAsync(db, patternAssessment, now);

            var wasStillOpen = previousOutcome?.Outcome == SignalOutcomeEnum.StillOpen;
            var isNowTerminal = evaluatedOutcome.Outcome is SignalOutcomeEnum.TargetHit or SignalOutcomeEnum.InvalidationHit;

            if (previousOutcome is null)
            {
                await db.SignalOutcomes.AddAsync(evaluatedOutcome);
            }
            else
            {
                previousOutcome.Outcome = evaluatedOutcome.Outcome;
                previousOutcome.FirstHitAtUtc = evaluatedOutcome.FirstHitAtUtc;
                previousOutcome.EvaluatedAtUtc = evaluatedOutcome.EvaluatedAtUtc;
            }

            await db.SaveChangesAsync();

            if (wasStillOpen && isNowTerminal)
            {
                await emitter.EmitAsync(
                    db,
                    patternAssessment.AnalysisRun.UserId,
                    AlertTrigger.LevelCrossed,
                    NotificationTargetScreenEnum.InstrumentDetail,
                    patternAssessment.AnalysisRun.AssetId,
                    now,
                    "Objectif de cours atteint",
                    "Un niveau cible que vous suiviez vient d'etre franchi.");
            }
        }
    }

    private static async Task<SignalOutcome> EvaluateStaticAsync(
        FinanceDbContext db,
        PatternAssessment patternAssessment,
        DateTime now)
    {
        var decisionSignal = patternAssessment.AnalysisRun.DecisionSignal!;
        var evaluationWindowDays = decisionSignal.HorizonDays > 0 ? decisionSignal.HorizonDays : 30;
        var evaluationStartUtc = patternAssessment.AnalysisRun.CompletedAtUtc ?? patternAssessment.AnalysisRun.StartedAtUtc;
        var windowStartUtc = evaluationStartUtc.Date;
        var windowEndUtcExclusive = windowStartUtc.AddDays(evaluationWindowDays + 1);
        var windowIsOpen = windowEndUtcExclusive > now;

        var signalOutcome = new SignalOutcome
        {
            AnalysisRunId = patternAssessment.AnalysisRunId,
            PatternAssessmentId = patternAssessment.Id,
            DecisionSignalId = decisionSignal.Id,
            EvaluationWindowDays = evaluationWindowDays,
            EvaluatedAtUtc = now,
            PolicyVersion = patternAssessment.AnalysisRun.ModelSnapshot?.ModelVersion ?? string.Empty,
            ConfidenceLabel = patternAssessment.Confidence >= ConfidenceThresholds.HighFloor
                ? ConfidenceLabelEnum.High
                : patternAssessment.Confidence >= ConfidenceThresholds.MediumFloor
                    ? ConfidenceLabelEnum.Medium
                    : ConfidenceLabelEnum.Low
        };

        if (!patternAssessment.TargetPrice.HasValue && !patternAssessment.InvalidationPrice.HasValue)
        {
            signalOutcome.Outcome = SignalOutcomeEnum.NotEvaluable;
            return signalOutcome;
        }

        var scanEndUtcExclusive = windowIsOpen ? now.Date.AddDays(1) : windowEndUtcExclusive;

        var candles = await db.AssetCandleSnapshots
            .Where(snapshot => snapshot.AssetId == patternAssessment.AnalysisRun.AssetId
                && snapshot.Interval == "1d"
                && snapshot.TimestampUtc >= windowStartUtc
                && snapshot.TimestampUtc < scanEndUtcExclusive)
            .OrderBy(snapshot => snapshot.TimestampUtc)
            .ToListAsync();

        foreach (var candle in candles)
        {
            var targetHit = patternAssessment.TargetPrice.HasValue && candle.High >= patternAssessment.TargetPrice.Value;
            var invalidationHit = patternAssessment.InvalidationPrice.HasValue && candle.Low <= patternAssessment.InvalidationPrice.Value;

            if (invalidationHit)
            {
                signalOutcome.Outcome = SignalOutcomeEnum.InvalidationHit;
                signalOutcome.FirstHitAtUtc = candle.TimestampUtc;
                return signalOutcome;
            }

            if (targetHit)
            {
                signalOutcome.Outcome = SignalOutcomeEnum.TargetHit;
                signalOutcome.FirstHitAtUtc = candle.TimestampUtc;
                return signalOutcome;
            }
        }

        if (windowIsOpen)
        {
            signalOutcome.Outcome = SignalOutcomeEnum.StillOpen;
            return signalOutcome;
        }

        var minCandlesRequired = Math.Max(1, evaluationWindowDays * 5 / 7 / 7);
        if (candles.Count < minCandlesRequired)
        {
            signalOutcome.Outcome = SignalOutcomeEnum.NotEvaluable;
            return signalOutcome;
        }

        signalOutcome.Outcome = SignalOutcomeEnum.TargetMiss;
        return signalOutcome;
    }

    [Fact]
    public async Task EvaluatePendingSignals_WhenTargetHitDuringOpenWindow_ReturnsTargetHitImmediately()
    {
        var dbName = $"signal-open-hit-{Guid.NewGuid():N}";
        var db = BuildInMemoryDb(dbName);
        var userId = "user-open-hit-1";
        var assetId = "asset-open-hit-1";

        var user = BuildUser(userId);
        var asset = BuildAsset(assetId);
        var completedAt = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);

        var run = new AnalysisRun
        {
            Id = "run-open-hit-1",
            UserId = userId,
            AssetId = assetId,
            Status = AnalysisRunStatusEnum.Completed,
            StartedAtUtc = completedAt.AddMinutes(-5),
            CompletedAtUtc = completedAt,
            RawPayload = "{}"
        };

        var decision = new DecisionSignal
        {
            Id = "decision-open-hit-1",
            AnalysisRunId = run.Id,
            Action = RecommendationActionEnum.Buy,
            IsActionable = true,
            Confidence = 0.80m,
            HorizonDays = 30
        };

        var assessment = new PatternAssessment
        {
            Id = "pa-open-hit-1",
            AnalysisRunId = run.Id,
            PatternId = "RECTANGLE_CONTINUATION",
            Phase = "bullish_breakout_confirmed",
            ProgressStatus = PatternProgressStatusEnum.Confirmed,
            Probability = 0.80m,
            Confidence = 0.80m,
            CurrentPrice = 100m,
            TargetPrice = 120m,
            InvalidationPrice = 90m,
            IsPrimary = true
        };

        var windowStart = completedAt.Date;

        var candleDay3 = new AssetCandleSnapshot
        {
            Id = "candle-open-hit-1",
            AssetId = assetId,
            TimestampUtc = windowStart.AddDays(3),
            Interval = "1d",
            Open = 115m,
            High = 125m,
            Low = 114m,
            Close = 123m,
            Volume = 1000m,
            Source = "test"
        };

        run.DecisionSignal = decision;
        db.Users.Add(user);
        db.Assets.Add(asset);
        db.AnalysisRuns.Add(run);
        db.DecisionSignals.Add(decision);
        db.PatternAssessments.Add(assessment);
        db.AssetCandleSnapshots.Add(candleDay3);
        await db.SaveChangesAsync();

        var emitter = new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance);
        var now = windowStart.AddDays(5);

        await InvokeEvaluatePendingSignalsAsync(db, emitter, now);

        var outcome = await db.SignalOutcomes
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.PatternAssessmentId == assessment.Id);

        Assert.NotNull(outcome);
        Assert.Equal(SignalOutcomeEnum.TargetHit, outcome!.Outcome);
        Assert.Equal(candleDay3.TimestampUtc, outcome.FirstHitAtUtc);
    }

    [Fact]
    public async Task EvaluatePendingSignals_WhenWindowClosedWithInsufficientCandles_ReturnsNotEvaluable()
    {
        var dbName = $"signal-partial-{Guid.NewGuid():N}";
        var db = BuildInMemoryDb(dbName);
        var userId = "user-partial-1";
        var assetId = "asset-partial-1";

        var user = BuildUser(userId);
        var asset = BuildAsset(assetId);
        var completedAt = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc);

        var run = new AnalysisRun
        {
            Id = "run-partial-1",
            UserId = userId,
            AssetId = assetId,
            Status = AnalysisRunStatusEnum.Completed,
            StartedAtUtc = completedAt.AddMinutes(-5),
            CompletedAtUtc = completedAt,
            RawPayload = "{}"
        };

        var decision = new DecisionSignal
        {
            Id = "decision-partial-1",
            AnalysisRunId = run.Id,
            Action = RecommendationActionEnum.Buy,
            IsActionable = true,
            Confidence = 0.60m,
            HorizonDays = 30
        };

        var assessment = new PatternAssessment
        {
            Id = "pa-partial-1",
            AnalysisRunId = run.Id,
            PatternId = "RECTANGLE_CONTINUATION",
            Phase = "bullish_breakout_confirmed",
            ProgressStatus = PatternProgressStatusEnum.Confirmed,
            Probability = 0.60m,
            Confidence = 0.60m,
            CurrentPrice = 100m,
            TargetPrice = 120m,
            InvalidationPrice = 90m,
            IsPrimary = true
        };

        var windowStart = completedAt.Date;

        // Seule 1 bougie présente sur 30 jours : insuffisant (min = max(1, 30*5/7/7) = 3)
        var singleCandle = new AssetCandleSnapshot
        {
            Id = "candle-partial-1",
            AssetId = assetId,
            TimestampUtc = windowStart.AddDays(10),
            Interval = "1d",
            Open = 105m,
            High = 108m,
            Low = 104m,
            Close = 106m,
            Volume = 500m,
            Source = "test"
        };

        run.DecisionSignal = decision;
        db.Users.Add(user);
        db.Assets.Add(asset);
        db.AnalysisRuns.Add(run);
        db.DecisionSignals.Add(decision);
        db.PatternAssessments.Add(assessment);
        db.AssetCandleSnapshots.Add(singleCandle);
        await db.SaveChangesAsync();

        var emitter = new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance);
        var now = windowStart.AddDays(35);

        await InvokeEvaluatePendingSignalsAsync(db, emitter, now);

        var outcome = await db.SignalOutcomes
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.PatternAssessmentId == assessment.Id);

        Assert.NotNull(outcome);
        Assert.Equal(SignalOutcomeEnum.NotEvaluable, outcome!.Outcome);
    }
}
