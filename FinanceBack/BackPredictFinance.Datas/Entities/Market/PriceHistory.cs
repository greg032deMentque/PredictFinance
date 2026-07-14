using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Historise les prix récupérés pour un actif afin de tracer les lectures de marché.
    /// </summary>
    public class PriceHistory : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string AssetId { get; set; } = string.Empty;
        public Asset Asset { get; set; } = null!;

        /// <summary>
        /// Horodatage de la récupération.
        /// </summary>
        public DateTime RetrievedAtUtc { get; set; }

        /// <summary>
        /// Prix unitaire retourné par le fournisseur.
        /// </summary>
        [Column(TypeName = "decimal(18,8)")]
        public decimal Price { get; set; }

        /// <summary>
        /// Volume associé (optionnel).
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal? Volume { get; set; }
    }
}
