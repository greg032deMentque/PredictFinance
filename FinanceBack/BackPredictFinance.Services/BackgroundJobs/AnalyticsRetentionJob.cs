using BackPredictFinance.Datas.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace BackPredictFinance.Services.BackgroundJobs
{
    public sealed class AnalyticsRetentionJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AnalyticsRetentionJob> _logger;

        public AnalyticsRetentionJob(IServiceScopeFactory scopeFactory, ILogger<AnalyticsRetentionJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = ComputeDelayUntilNextRun();
                _logger.LogInformation("AnalyticsRetentionJob: prochaine exécution dans {Delay}", delay);

                await Task.Delay(delay, stoppingToken);

                try
                {
                    await AnonymizeOldAnalyticsAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "AnalyticsRetentionJob: erreur lors de l'anonymisation");
                }
            }
        }

        private static TimeSpan ComputeDelayUntilNextRun()
        {
            var now = DateTime.UtcNow;
            var next = new DateTime(now.Year, now.Month, 1, 2, 0, 0, DateTimeKind.Utc).AddMonths(1);
            return next - now;
        }

        private async Task AnonymizeOldAnalyticsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

            var cutoff = DateTime.UtcNow.AddMonths(-13);
            var batch = await db.Analytics
                .Where(a => a.Date < cutoff && !a.Login.EndsWith("@anon"))
                .Take(5000)
                .ToListAsync(ct);

            if (batch.Count == 0)
            {
                _logger.LogInformation("AnalyticsRetentionJob: aucune ligne à anonymiser");
                return;
            }

            foreach (var entry in batch)
            {
                entry.Login = ComputeShortHash(entry.Login) + "@anon";
                entry.Ip = "0.0.0.0";
                entry.Body = string.Empty;
                entry.UserAgent = string.Empty;
                entry.Referer = string.Empty;
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("AnalyticsRetentionJob: {Count} lignes anonymisées", batch.Count);
        }

        private static string ComputeShortHash(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes)[..12].ToLowerInvariant();
        }
    }
}
