using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BackPredictFinance.Services.BackgroundJobs
{
    public sealed class InstrumentWatchJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<InstrumentWatchJob> _logger;

        public InstrumentWatchJob(
            IServiceScopeFactory scopeFactory,
            ILogger<InstrumentWatchJob> logger)
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
                    await RunWatchCycleAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "InstrumentWatchJob: erreur lors du cycle de surveillance");
                }
            }
        }

        private static TimeSpan ComputeDelayUntilNextRun()
        {
            var now = DateTime.UtcNow;
            var nextRunUtc = now.Date.AddHours(6);
            if (nextRunUtc <= now)
            {
                nextRunUtc = nextRunUtc.AddDays(1);
            }
            return nextRunUtc - now;
        }

        private async Task RunWatchCycleAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
            var emitter = scope.ServiceProvider.GetRequiredService<IProactiveAlertEmitter>();
            var now = DateTime.UtcNow;

            var trackedAssets = await db.UserAssets
                .AsNoTracking()
                .Select(ua => new { ua.UserId, ua.AssetId })
                .Distinct()
                .ToListAsync(ct);

            _logger.LogInformation("InstrumentWatchJob: {Count} paires (user, actif) a surveiller", trackedAssets.Count);

            foreach (var pair in trackedAssets)
            {
                await ProcessPatternStateChangeAsync(db, emitter, pair.UserId, pair.AssetId, now, ct);
                await ProcessDataStaleAsync(db, emitter, pair.UserId, pair.AssetId, now, ct);
            }
        }

        private async Task ProcessPatternStateChangeAsync(
            FinanceDbContext db,
            IProactiveAlertEmitter emitter,
            string userId,
            string assetId,
            DateTime now,
            CancellationToken ct)
        {
            var lastTwoPrimary = await db.PatternAssessments
                .AsNoTracking()
                .Where(pa => pa.IsPrimary && pa.AnalysisRun.AssetId == assetId && pa.AnalysisRun.UserId == userId)
                .OrderByDescending(pa => pa.CreatedAtUtc)
                .Take(2)
                .Select(pa => new { pa.ProgressStatus, pa.AnalysisRun.AssetId })
                .ToListAsync(ct);

            if (lastTwoPrimary.Count < 2)
            {
                return;
            }

            var current = lastTwoPrimary[0].ProgressStatus;
            var previous = lastTwoPrimary[1].ProgressStatus;

            var isSignificantTransition =
                (previous == PatternProgressStatusEnum.Monitoring && current == PatternProgressStatusEnum.Confirmed)
                || current == PatternProgressStatusEnum.Invalidated;

            if (!isSignificantTransition)
            {
                return;
            }

            var (title, summary) = current == PatternProgressStatusEnum.Confirmed
                ? ("Pattern confirme", "Le pattern technique detecte sur cet instrument vient d'etre confirme. Consultez votre analyse pour plus de details.")
                : ("Pattern invalide", "Le pattern technique detecte sur cet instrument vient d'etre invalide. Consultez votre analyse pour evaluer la situation.");

            await emitter.EmitAsync(
                db,
                userId,
                AlertTrigger.PatternStateChange,
                NotificationTargetScreenEnum.AnalysisResult,
                assetId,
                now,
                title,
                summary,
                ct);
        }

        internal async Task ProcessDataStaleAsync(
            FinanceDbContext db,
            IProactiveAlertEmitter emitter,
            string userId,
            string assetId,
            DateTime now,
            CancellationToken ct)
        {
            var latestCandle = await db.AssetCandleSnapshots
                .AsNoTracking()
                .Where(c => c.AssetId == assetId && c.Interval == "1d")
                .OrderByDescending(c => c.TimestampUtc)
                .Select(c => (DateTime?)c.TimestampUtc)
                .FirstOrDefaultAsync(ct);

            var freshness = FreshnessClassifier.Classify(latestCandle, now, _logger);

            if (freshness != FreshnessStatusEnum.Stale)
            {
                return;
            }

            await emitter.EmitAsync(
                db,
                userId,
                AlertTrigger.DataStale,
                NotificationTargetScreenEnum.InstrumentDetail,
                assetId,
                now,
                "Donnees de marche obsoletes",
                "Les donnees de marche de cet instrument n'ont pas ete actualisees depuis plusieurs jours de bourse. Consultez la fiche instrument.",
                ct);
        }
    }
}
