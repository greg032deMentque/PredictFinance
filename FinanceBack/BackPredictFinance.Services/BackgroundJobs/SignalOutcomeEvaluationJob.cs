using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BackPredictFinance.Services.BackgroundJobs
{
    public sealed class SignalOutcomeEvaluationJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SignalOutcomeEvaluationJob> _logger;

        public SignalOutcomeEvaluationJob(
            IServiceScopeFactory scopeFactory,
            ILogger<SignalOutcomeEvaluationJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(ComputeDelayUntilNextRun(), stoppingToken);

                try
                {
                    await EvaluatePendingSignalsAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "SignalOutcomeEvaluationJob: erreur");
                }
            }
        }

        private static TimeSpan ComputeDelayUntilNextRun()
        {
            var now = DateTime.UtcNow;
            var nextRunUtc = now.Date.AddHours(3);
            if (nextRunUtc <= now)
            {
                nextRunUtc = nextRunUtc.AddDays(1);
            }

            return nextRunUtc - now;
        }

        private async Task EvaluatePendingSignalsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
            var emitter = scope.ServiceProvider.GetRequiredService<IProactiveAlertEmitter>();
            var now = DateTime.UtcNow;

            var candidates = await db.PatternAssessments
                .AsNoTracking()
                .Where(patternAssessment => patternAssessment.IsPrimary
                    && patternAssessment.AnalysisRun.DecisionSignal != null
                    && patternAssessment.AnalysisRun.DecisionSignal.IsActionable
                    && (!db.SignalOutcomes.Any(signalOutcome => signalOutcome.PatternAssessmentId == patternAssessment.Id)
                        || db.SignalOutcomes.Any(signalOutcome =>
                            signalOutcome.PatternAssessmentId == patternAssessment.Id
                            && signalOutcome.Outcome == SignalOutcomeEnum.StillOpen)))
                .Include(patternAssessment => patternAssessment.AnalysisRun)
                    .ThenInclude(analysisRun => analysisRun.DecisionSignal)
                .Include(patternAssessment => patternAssessment.AnalysisRun)
                    .ThenInclude(analysisRun => analysisRun.ModelSnapshot)
                .ToListAsync(ct);

            _logger.LogInformation("SignalOutcomeEvaluationJob: {Count} candidats", candidates.Count);

            foreach (var patternAssessment in candidates)
            {
                var previousOutcome = await db.SignalOutcomes
                    .FirstOrDefaultAsync(signalOutcome => signalOutcome.PatternAssessmentId == patternAssessment.Id, ct);

                var evaluatedOutcome = await EvaluateAsync(db, patternAssessment, now, ct);

                var wasStillOpen = previousOutcome?.Outcome == SignalOutcomeEnum.StillOpen;
                var isNowTerminal = evaluatedOutcome.Outcome is SignalOutcomeEnum.TargetHit or SignalOutcomeEnum.InvalidationHit;

                if (previousOutcome is null)
                {
                    await db.SignalOutcomes.AddAsync(evaluatedOutcome, ct);
                }
                else
                {
                    previousOutcome.AnalysisRunId = evaluatedOutcome.AnalysisRunId;
                    previousOutcome.DecisionSignalId = evaluatedOutcome.DecisionSignalId;
                    previousOutcome.EvaluationWindowDays = evaluatedOutcome.EvaluationWindowDays;
                    previousOutcome.EvaluatedAtUtc = evaluatedOutcome.EvaluatedAtUtc;
                    previousOutcome.FirstHitAtUtc = evaluatedOutcome.FirstHitAtUtc;
                    previousOutcome.PolicyVersion = evaluatedOutcome.PolicyVersion;
                    previousOutcome.ConfidenceLabel = evaluatedOutcome.ConfidenceLabel;
                    previousOutcome.Outcome = evaluatedOutcome.Outcome;
                }

                await db.SaveChangesAsync(ct);

                if (wasStillOpen && isNowTerminal)
                {
                    await EmitLevelCrossedAlertAsync(emitter, db, patternAssessment, evaluatedOutcome, now, ct);
                }
            }
        }

        private async Task EmitLevelCrossedAlertAsync(
            IProactiveAlertEmitter emitter,
            FinanceDbContext db,
            PatternAssessment patternAssessment,
            SignalOutcome outcome,
            DateTime now,
            CancellationToken ct)
        {
            var userId = patternAssessment.AnalysisRun.UserId;
            var assetId = patternAssessment.AnalysisRun.AssetId;

            var (title, summary) = outcome.Outcome == SignalOutcomeEnum.TargetHit
                ? ("Objectif de cours atteint", "Un niveau cible que vous suiviez vient d'etre franchi. Consultez la fiche instrument pour en savoir plus.")
                : ("Niveau d'invalidation touche", "Un niveau d'invalidation que vous suiviez vient d'etre touche. Consultez la fiche instrument pour evaluer votre situation.");

            await emitter.EmitAsync(
                db,
                userId,
                AlertTrigger.LevelCrossed,
                NotificationTargetScreenEnum.InstrumentDetail,
                assetId,
                now,
                title,
                summary,
                ct);
        }

        private static async Task<SignalOutcome> EvaluateAsync(
            FinanceDbContext db,
            PatternAssessment patternAssessment,
            DateTime now,
            CancellationToken ct)
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
                ConfidenceLabel = BucketConfidence(patternAssessment.Confidence)
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
                .ToListAsync(ct);

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

            // Couverture minimale : au moins 1 bougie par tranche de 7 jours ouvrables attendus.
            // Un horizon de N jours calendaires ≈ N * 5/7 jours de bourse.
            // On exige candles.Count >= max(1, evaluationWindowDays * 5 / 7 / 7).
            // En pratique : fenêtre de 5j → 1 bougie min ; 30j → 3 bougies min ; 90j → 9 bougies min.
            var minCandlesRequired = Math.Max(1, evaluationWindowDays * 5 / 7 / 7);
            if (candles.Count < minCandlesRequired)
            {
                signalOutcome.Outcome = SignalOutcomeEnum.NotEvaluable;
                return signalOutcome;
            }

            signalOutcome.Outcome = SignalOutcomeEnum.TargetMiss;
            return signalOutcome;
        }

        private static ConfidenceLabelEnum BucketConfidence(decimal confidence)
        {
            return confidence switch
            {
                < ConfidenceThresholds.MediumFloor => ConfidenceLabelEnum.Low,
                < ConfidenceThresholds.HighFloor => ConfidenceLabelEnum.Medium,
                _ => ConfidenceLabelEnum.High
            };
        }
    }
}
