using BackPredictFinance.Common.enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPredictFinance.Datas.Models
{
    /// <summary>
    /// Historiser chaque opération pour reproduire l’évolution du portefeuille et calculer les gains/pertes.
    /// </summary>
    public class AssetTransaction : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserAssetId { get; set; }
        public UserAsset UserAsset { get; set; } = null!;
        public DateTime TimestampUtc { get; set; }
        public TransactionTypeEnum TransactionType { get; set; }  // Buy ou Sell

        [Column(TypeName = "decimal(18,8)")] 
        public decimal Quantity { get; set; }
        [Column(TypeName = "decimal(18,8)")] 
        public decimal UnitPrice { get; set; }
        [Column(TypeName = "decimal(18,8)")] 
        public decimal Fees { get; set; }
    }
}
