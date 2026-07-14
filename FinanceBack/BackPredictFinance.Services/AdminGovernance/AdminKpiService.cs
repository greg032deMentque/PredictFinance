using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.AdminViewModels.Kpi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BackPredictFinance.Services.AdminGovernance
{
    public interface IAdminKpiService
    {
        Task<List<AdminKpiCardViewModel>> GetKpiCardsAsync(CancellationToken ct = default);
        Task<AdminEngagementKpiViewModel> GetEngagementKpiAsync(KpiWindow window, CancellationToken ct = default);
    }

    public sealed class AdminKpiService : BaseService, IAdminKpiService
    {
        public AdminKpiService(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public async Task<List<AdminKpiCardViewModel>> GetKpiCardsAsync(CancellationToken ct = default)
        {
            var engagement = await GetEngagementKpiAsync(KpiWindow.D30, ct);
            var activationRate = engagement.ActivationFunnel.LastOrDefault()?.Rate ?? 0m;

            return
            [
                new AdminKpiCardViewModel { Key = "SIGNAL_QUALITY", Label = "Qualité signal", Value = 0, Unit = "%" },
                new AdminKpiCardViewModel { Key = "ENGAGEMENT", Label = "Utilisateurs actifs (30j)", Value = engagement.ActiveUsers, Unit = "users" },
                new AdminKpiCardViewModel { Key = "USAGE_FUNNEL", Label = "Taux activation", Value = Math.Round(activationRate * 100, 1), Unit = "%" },
                new AdminKpiCardViewModel { Key = "OPS_HEALTH", Label = "Succès analyses", Value = engagement.OpsSuccessRate, Unit = "%" },
            ];
        }

        public async Task<AdminEngagementKpiViewModel> GetEngagementKpiAsync(KpiWindow window, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var windowStart = now.AddDays(-(int)window);

            var consentedLogins = await _financeDbContext.Users
                .Where(u => u.AnalyticsConsent)
                .Select(u => u.UserName!)
                .ToListAsync(ct);

            var dau = await _financeDbContext.Analytics
                .Where(a => a.Date >= now.AddDays(-1) && consentedLogins.Contains(a.Login))
                .Select(a => a.Login).Distinct().CountAsync(ct);

            var wau = await _financeDbContext.Analytics
                .Where(a => a.Date >= now.AddDays(-7) && consentedLogins.Contains(a.Login))
                .Select(a => a.Login).Distinct().CountAsync(ct);

            var mau = await _financeDbContext.Analytics
                .Where(a => a.Date >= now.AddDays(-30) && consentedLogins.Contains(a.Login))
                .Select(a => a.Login).Distinct().CountAsync(ct);

            var activeUsers = await _financeDbContext.Analytics
                .Where(a => a.Date >= windowStart && consentedLogins.Contains(a.Login))
                .Select(a => a.Login).Distinct().CountAsync(ct);

            var stickiness = mau > 0 ? Math.Round((decimal)dau / mau, 4) : 0m;

            var cohorts = await BuildRetentionCohortsAsync(window, consentedLogins, now, ct);
            var funnel = await BuildActivationFunnelAsync(windowStart, ct);

            var totalNotifs = await _financeDbContext.UserNotifications
                .CountAsync(n => n.CreatedAtUtc >= windowStart, ct);
            var readNotifs = await _financeDbContext.UserNotifications
                .CountAsync(n => n.CreatedAtUtc >= windowStart && n.ReadAtUtc.HasValue, ct);
            var notifReadRate = totalNotifs > 0 ? Math.Round((decimal)readNotifs / totalNotifs, 4) : 0m;

            var totalRuns = await _financeDbContext.AnalysisRuns
                .CountAsync(r => r.CreatedAtUtc >= windowStart, ct);
            var successRuns = await _financeDbContext.AnalysisRuns
                .CountAsync(r => r.CreatedAtUtc >= windowStart && r.Status == AnalysisRunStatusEnum.Completed, ct);
            var opsSuccessRate = totalRuns > 0 ? Math.Round((decimal)successRuns / totalRuns * 100, 2) : 0m;

            var completedRuns = await _financeDbContext.AnalysisRuns
                .Where(r => r.CreatedAtUtc >= windowStart && r.CompletedAtUtc.HasValue)
                .Select(r => new { r.StartedAtUtc, r.CompletedAtUtc })
                .ToListAsync(ct);
            var avgDurationMs = completedRuns.Count > 0
                ? completedRuns.Average(r => (r.CompletedAtUtc!.Value - r.StartedAtUtc).TotalMilliseconds)
                : 0d;

            var staleAssets = await _financeDbContext.Assets
                .CountAsync(a => !_financeDbContext.AnalysisRuns
                    .Any(r => r.AssetId == a.Id && r.CreatedAtUtc >= now.AddDays(-7)), ct);

            return new AdminEngagementKpiViewModel
            {
                Window = window.ToString(),
                Dau = dau,
                Wau = wau,
                Mau = mau,
                Stickiness = stickiness,
                ActiveUsers = activeUsers,
                RetentionCohorts = cohorts,
                ActivationFunnel = funnel,
                NotificationReadRate = notifReadRate,
                OpsSuccessRate = opsSuccessRate,
                OpsAvgDurationMs = Math.Round(avgDurationMs, 0),
                StaleAssets = staleAssets,
            };
        }

        private async Task<List<RetentionCohortViewModel>> BuildRetentionCohortsAsync(
            KpiWindow window, List<string> consentedLogins, DateTime now, CancellationToken ct)
        {
            var windowStart = now.AddDays(-(int)window);

            var cohortUsers = await _financeDbContext.Users
                .Where(u => u.CreatedAt >= windowStart)
                .Select(u => new { u.UserName, u.CreatedAt })
                .ToListAsync(ct);

            if (cohortUsers.Count == 0)
            {
                var empty = new List<RetentionCohortViewModel>
                {
                    new() { Label = "J+1", Rate = 0, SampleSize = 0 },
                    new() { Label = "J+7", Rate = 0, SampleSize = 0 },
                };
                if (window >= KpiWindow.D30)
                    empty.Add(new() { Label = "J+30", Rate = 0, SampleSize = 0 });
                return empty;
            }

            var userNames = cohortUsers.Select(u => u.UserName!).ToList();
            var analyticsByLogin = await _financeDbContext.Analytics
                .Where(a => userNames.Contains(a.Login))
                .Select(a => new { a.Login, a.Date })
                .ToListAsync(ct);

            int j1 = 0, j7 = 0, j30 = 0, sample = cohortUsers.Count;

            foreach (var user in cohortUsers)
            {
                var dates = analyticsByLogin
                    .Where(a => a.Login == user.UserName)
                    .Select(a => a.Date)
                    .ToList();

                if (dates.Any(d => d >= user.CreatedAt.AddDays(1) && d < user.CreatedAt.AddDays(2))) j1++;
                if (dates.Any(d => d >= user.CreatedAt.AddDays(7) && d < user.CreatedAt.AddDays(8))) j7++;
                if (window >= KpiWindow.D30 &&
                    dates.Any(d => d >= user.CreatedAt.AddDays(30) && d < user.CreatedAt.AddDays(31))) j30++;
            }

            var cohorts = new List<RetentionCohortViewModel>
            {
                new() { Label = "J+1", Rate = Math.Round((decimal)j1 / sample, 4), SampleSize = sample },
                new() { Label = "J+7", Rate = Math.Round((decimal)j7 / sample, 4), SampleSize = sample },
            };
            if (window >= KpiWindow.D30)
                cohorts.Add(new() { Label = "J+30", Rate = Math.Round((decimal)j30 / sample, 4), SampleSize = sample });

            return cohorts;
        }

        private async Task<List<ActivationFunnelStepViewModel>> BuildActivationFunnelAsync(
            DateTime windowStart, CancellationToken ct)
        {
            var step1 = await _financeDbContext.Users
                .CountAsync(u => u.CreatedAt >= windowStart, ct);

            var step2 = await _financeDbContext.Users
                .CountAsync(u => u.CreatedAt >= windowStart && u.EmailConfirmed, ct);

            var activeLogins = await _financeDbContext.Analytics
                .Where(a => a.Date >= windowStart)
                .Select(a => a.Login).Distinct().ToListAsync(ct);

            var step3UserIds = await _financeDbContext.Users
                .Where(u => u.CreatedAt >= windowStart && activeLogins.Contains(u.UserName!))
                .Select(u => u.Id)
                .ToListAsync(ct);
            var step3 = step3UserIds.Count;

            var step4UserIds = await _financeDbContext.AnalysisRuns
                .Where(r => r.CreatedAtUtc >= windowStart && step3UserIds.Contains(r.UserId))
                .Select(r => r.UserId).Distinct().ToListAsync(ct);
            var step4 = step4UserIds.Count;

            var step5 = 0;
            if (step4UserIds.Count > 0)
            {
                var firstRunByUser = await _financeDbContext.AnalysisRuns
                    .Where(r => step4UserIds.Contains(r.UserId))
                    .GroupBy(r => r.UserId)
                    .Select(g => new { UserId = g.Key, FirstRun = g.Min(r => r.CreatedAtUtc) })
                    .ToListAsync(ct);

                var userNames = await _financeDbContext.Users
                    .Where(u => step4UserIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.UserName })
                    .ToListAsync(ct);

                var relevantAnalytics = await _financeDbContext.Analytics
                    .Where(a => userNames.Select(u => u.UserName!).Contains(a.Login))
                    .Select(a => new { a.Login, a.Date })
                    .ToListAsync(ct);

                foreach (var entry in firstRunByUser)
                {
                    var userName = userNames.FirstOrDefault(u => u.Id == entry.UserId)?.UserName;
                    if (userName is null) continue;
                    var target = entry.FirstRun.AddDays(7);
                    if (relevantAnalytics.Any(a => a.Login == userName && a.Date >= target && a.Date < target.AddDays(1)))
                        step5++;
                }
            }

            decimal r(int num, int denom) => denom > 0 ? Math.Round((decimal)num / denom, 4) : 0m;

            return
            [
                new() { Step = 1, Label = "Inscrits", Count = step1, Rate = 1m },
                new() { Step = 2, Label = "Email confirmé", Count = step2, Rate = r(step2, step1) },
                new() { Step = 3, Label = "Au moins 1 session", Count = step3, Rate = r(step3, step1) },
                new() { Step = 4, Label = "Au moins 1 analyse", Count = step4, Rate = r(step4, step1) },
                new() { Step = 5, Label = "Actif J+7 post-analyse", Count = step5, Rate = r(step5, step4) },
            ];
        }
    }
}
