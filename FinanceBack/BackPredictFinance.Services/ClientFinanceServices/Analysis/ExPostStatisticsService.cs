using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public interface IExPostStatisticsService
    {
        Task<ExPostPatternStatisticsViewModel> GetPatternStatisticsAsync(CancellationToken ct = default);
    }

    public sealed class ExPostStatisticsService : BaseService, IExPostStatisticsService
    {
        // En dessous de 20 échantillons, l'intervalle de confiance est trop large pour être
        // exploitable ; on affiche InsufficientData plutôt qu'un taux de réussite trompeur.
        private const int MinSampleSize = 20;
        // Score z à 1.96 = intervalle de confiance à 95% pour l'intervalle de Wilson.
        private const double WilsonZ = 1.96;

        private readonly IClientFinanceAssetSupportService _assetSupportService;

        public ExPostStatisticsService(IServiceProvider serviceProvider, IClientFinanceAssetSupportService assetSupportService)
            : base(serviceProvider)
        {
            _assetSupportService = assetSupportService;
        }

        /// <summary>
        /// Calcule le taux de réussite historique par pattern (ex post) pour l'utilisateur courant.
        /// Piège de biais de sélection : seuls les signaux arrivés à un état TERMINAL sont comptés
        /// (TargetHit/InvalidationHit/TargetMiss) — les signaux encore ouverts sont exclus, ce qui
        /// peut biaiser temporairement les stats si beaucoup de signaux récents sont encore en cours
        /// (SelectionBiasDisclaimer est donc toujours renvoyé à true).
        /// </summary>
        public async Task<ExPostPatternStatisticsViewModel> GetPatternStatisticsAsync(CancellationToken ct = default)
        {
            var userId = _assetSupportService.GetRequiredCurrentUserId();
            var terminalOutcomes = new[] { SignalOutcomeEnum.TargetHit, SignalOutcomeEnum.InvalidationHit, SignalOutcomeEnum.TargetMiss };

            var rows = await _financeDbContext.SignalOutcomes
                .AsNoTracking()
                .Where(so => so.AnalysisRun.UserId == userId && terminalOutcomes.Contains(so.Outcome))
                .Select(so => new
                {
                    so.PatternAssessment.PatternId,
                    so.Outcome,
                    so.EvaluationWindowDays,
                    EmissionDateUtc = so.AnalysisRun.CompletedAtUtc ?? so.AnalysisRun.StartedAtUtc,
                    EarningsDateUtc = so.DecisionSignal.EarningsDateUtc
                })
                .ToListAsync(ct);

            // Regroupement par pattern ET par présence d'une publication de résultats dans la fenêtre
            // d'évaluation : un pattern technique a statistiquement une fiabilité différente si une
            // annonce de résultats (imprévisible techniquement) tombe pendant la période surveillée.
            // Séparer les deux populations évite de mélanger un taux de réussite "pattern pur" avec un
            // taux perturbé par un événement fondamental.
            var grouped = rows
                .GroupBy(r => new
                {
                    r.PatternId,
                    HasEarningsInWindow = EarningsHorizonEvaluator.IsWithinHorizon(r.EarningsDateUtc, r.EvaluationWindowDays, r.EmissionDateUtc)
                })
                .Select(g =>
                {
                    var n = g.Count();
                    var wins = g.Count(r => r.Outcome == SignalOutcomeEnum.TargetHit);

                    if (n < MinSampleSize)
                    {
                        return new ExPostStatisticsViewModel
                        {
                            PatternId = g.Key.PatternId,
                            HasEarningsInWindow = g.Key.HasEarningsInWindow,
                            SampleSize = n,
                            InsufficientData = true,
                            SelectionBiasDisclaimer = true
                        };
                    }

                    var (winRate, low, high) = ComputeWilsonInterval(wins, n);

                    return new ExPostStatisticsViewModel
                    {
                        PatternId = g.Key.PatternId,
                        HasEarningsInWindow = g.Key.HasEarningsInWindow,
                        SampleSize = n,
                        InsufficientData = false,
                        WinRate = decimal.Round((decimal)winRate, 4),
                        WinRateLow = decimal.Round((decimal)low, 4),
                        WinRateHigh = decimal.Round((decimal)high, 4),
                        SelectionBiasDisclaimer = true
                    };
                })
                .ToList();

            return new ExPostPatternStatisticsViewModel
            {
                PatternStats = grouped,
                SelectionBiasDisclaimer = true
            };
        }

        // Intervalle de Wilson plutôt qu'un intervalle normal classique (Wald) : reste borné dans
        // [0,1] et beaucoup plus fiable sur les petits échantillons ou les taux proches de 0%/100%,
        // ce qui est le cas fréquent ici (échantillons de quelques dizaines de signaux par pattern).
        private static (double WinRate, double Low, double High) ComputeWilsonInterval(int wins, int n)
        {
            var pHat = (double)wins / n;
            var z2 = WilsonZ * WilsonZ;
            var denominator = 1.0 + z2 / n;
            var center = (pHat + z2 / (2.0 * n)) / denominator;
            var margin = WilsonZ * Math.Sqrt(pHat * (1.0 - pHat) / n + z2 / (4.0 * n * n)) / denominator;

            var low = Math.Max(0.0, center - margin);
            var high = Math.Min(1.0, center + margin);

            return (pHat, low, high);
        }
    }
}
