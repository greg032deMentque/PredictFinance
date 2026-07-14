using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis
{
    /// <summary>
    /// Représente l'évaluation ex post d'une analyse : ce qui s'est réellement passé
    /// à l'horizon de revue prévu (RM-28 : boucle ex post, C-11).
    /// </summary>
    public sealed class ExPostEvaluationViewModel
    {
        // NOT_APPLICABLE | PENDING | DATA_UNAVAILABLE | TARGET_REACHED | INVALIDATED | NEUTRAL
        public string Status { get; set; } = "NOT_APPLICABLE";
        public string StatusLabel { get; set; } = string.Empty;
        public DateTime? ReviewScheduledAtUtc { get; set; }
        public decimal? PriceAtReview { get; set; }
        public decimal? TargetPrice { get; set; }
        public decimal? InvalidationPrice { get; set; }
        public string? PedagogicalNote { get; set; }

        public static ExPostEvaluationViewModel NotApplicable() => new()
        {
            Status = "NOT_APPLICABLE",
            StatusLabel = "Non applicable"
        };

        public static ExPostEvaluationViewModel Pending(DateTime reviewScheduledAt) => new()
        {
            Status = "PENDING",
            StatusLabel = "Revue à venir",
            ReviewScheduledAtUtc = reviewScheduledAt,
            PedagogicalNote = $"La revue est prévue le {reviewScheduledAt:dd/MM/yyyy}. Revenez à cette date pour voir si le scénario s'est réalisé."
        };

        public static ExPostEvaluationViewModel DataUnavailable(DateTime reviewScheduledAt) => new()
        {
            Status = "DATA_UNAVAILABLE",
            StatusLabel = "Données non disponibles",
            ReviewScheduledAtUtc = reviewScheduledAt,
            PedagogicalNote = "Les données de clôture pour la date de revue ne sont pas disponibles. Relancez une analyse sur cet instrument pour actualiser les données."
        };

        public static ExPostEvaluationViewModel Evaluate(
            DateTime reviewScheduledAt,
            decimal priceAtReview,
            decimal? targetPrice,
            decimal? invalidationPrice)
        {
            string status;
            string label;
            string note;

            if (targetPrice.HasValue && priceAtReview >= targetPrice.Value)
            {
                status = "TARGET_REACHED";
                label = "Cible atteinte";
                note = $"Le cours ({priceAtReview:N2}) a atteint ou dépassé la cible ({targetPrice.Value:N2}). Le scénario haussier s'est réalisé.";
            }
            else if (invalidationPrice.HasValue && priceAtReview <= invalidationPrice.Value)
            {
                status = "INVALIDATED";
                label = "Invalidation déclenchée";
                note = $"Le cours ({priceAtReview:N2}) est passé sous le niveau d'invalidation ({invalidationPrice.Value:N2}). Le scénario ne s'est pas réalisé — c'est une leçon utile.";
            }
            else
            {
                status = "NEUTRAL";
                label = "Résultat neutre";
                note = $"À la date de revue, le cours ({priceAtReview:N2}) n'a ni atteint la cible ni franchi l'invalidation. Le signal était prématuré ou le marché consolidait encore.";
            }

            return new ExPostEvaluationViewModel
            {
                Status = status,
                StatusLabel = label,
                ReviewScheduledAtUtc = reviewScheduledAt,
                PriceAtReview = priceAtReview,
                TargetPrice = targetPrice,
                InvalidationPrice = invalidationPrice,
                PedagogicalNote = note
            };
        }
    }
}
