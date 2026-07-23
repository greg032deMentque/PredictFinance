using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Représente le rattachement d'un actif à un utilisateur dans sa watchlist ou son portefeuille.
    /// </summary>
    public class UserAsset : AuditableEntityBase, ISoftDeletable
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public string AssetId { get; set; } = string.Empty;
        public Asset Asset { get; set; } = null!;

        /// <summary>
        /// Quantité de titres ou de parts actuellement détenus.
        /// </summary>
        [Column(TypeName = "decimal(18,8)")]
        public decimal Quantity { get; set; }

        public bool IsDeleted { get; set; }

        public List<Recommendation> Recommendations { get; set; } = new();
    }
}
