using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPredictFinance.Datas.Models
{
    /// <summary>
    /// Historique des valeurs d'un actif à chaque appel d'API
    /// </summary>
    public class PriceHistory : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string AssetId { get; set; }
        public Asset Asset { get; set; } = null!;

        /// <summary>
        /// Horodatage de la requête API
        /// </summary>
        public DateTime RetrievedAtUtc { get; set; }

        /// <summary>
        /// Prix unitaire retourné par l'API
        /// </summary>
        [Column(TypeName = "decimal(18,8)")]
        public decimal Price { get; set; }

        /// <summary>
        /// Volume associé (optionnel)
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal? Volume { get; set; }
    }
}
