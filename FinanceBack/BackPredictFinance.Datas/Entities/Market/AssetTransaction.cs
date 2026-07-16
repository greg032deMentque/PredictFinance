using BackPredictFinance.Common.enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Historise chaque transaction d'un utilisateur pour reconstruire son portefeuille.
    /// </summary>
    public class AssetTransaction : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserAssetId { get; set; } = string.Empty;
        public UserAsset UserAsset { get; set; } = null!;

        /// <summary>
        /// Portefeuille auquel la transaction est rattachée (PEA, CTO, assurance vie...).
        /// La détention reste une vérité globale (somme sur tous les portefeuilles) ; seules les
        /// positions et la valorisation sont segmentées par portefeuille.
        /// </summary>
        public string PortfolioId { get; set; } = string.Empty;
        public Portfolio Portfolio { get; set; } = null!;

        public DateTime TimestampUtc { get; set; }
        public TransactionTypeEnum TransactionType { get; set; }  // Buy ou Sell

        [Column(TypeName = "decimal(18,8)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,8)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,8)")]
        public decimal Fees { get; set; }

        public bool IsDeleted { get; set; }
    }
}
