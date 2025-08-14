using BackPredictFinance.Common.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackPredictFinance.Datas.Models
{
    public class PatternPrediction : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string AssetId { get; set; }
        public Asset Asset { get; set; } = null!;

        [Required]
        public TradingPatternEnum PatternType { get; set; }

        /// <summary>
        /// Probabilité (0…1) que le pattern soit détecté
        /// </summary>
        [Column(TypeName = "decimal(5,4)")]
        public decimal Probability { get; set; }

        /// <summary>
        /// Horodatage de la prédiction
        /// </summary>
        public DateTime PredictedAtUtc { get; set; }

        /// <summary>
        /// Version du modèle IA utilisé
        /// </summary>
        public string IAModelVersionId { get; set; }
        public IAModelVersion IAModelVersion { get; set; } = null!;
    }
}
