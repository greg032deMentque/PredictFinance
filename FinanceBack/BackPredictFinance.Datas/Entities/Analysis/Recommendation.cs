using BackPredictFinance.Common.enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Historise une recommandation liée à une ligne utilisateur et à un instant donné.
    /// </summary>
    public class Recommendation : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserAssetId { get; set; } = string.Empty;
        public UserAsset UserAsset { get; set; } = null!;

        [Required]
        public RecommendationActionEnum Action { get; set; }

        /// <summary>
        /// Confiance associée à la recommandation (0..1).
        /// </summary>
        [Column(TypeName = "decimal(5,4)")]
        public decimal Confidence { get; set; }

        /// <summary>
        /// Horodatage de la recommandation.
        /// </summary>
        public DateTime RecommendedAtUtc { get; set; }

        /// <summary>
        /// Prix cible optionnel si pertinent pour la recommandation.
        /// </summary>
        [Column(TypeName = "decimal(18,8)")]
        public decimal? TargetPrice { get; set; }

        /// <summary>
        /// Motif ou explication libre.
        /// </summary>
        public string? Reason { get; set; }
    }
}
