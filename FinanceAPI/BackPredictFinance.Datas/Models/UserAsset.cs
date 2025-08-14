using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPredictFinance.Datas.Models
{
    public class UserAsset : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string UserId { get; set; }
        public User User { get; set; } = null!;

        public string AssetId { get; set; }
        public Asset Asset { get; set; } = null!;

        /// <summary>
        /// Quantité de tokens / parts que l'utilisateur détient
        /// </summary>
        [Column(TypeName = "decimal(18,8)")]
        public decimal Quantity { get; set; }

        public List<Recommendation> Recommendations { get; set; } = new();

    }

}
