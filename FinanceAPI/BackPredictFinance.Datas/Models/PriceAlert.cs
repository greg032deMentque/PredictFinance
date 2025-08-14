using BackPredictFinance.Common.enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPredictFinance.Datas.Models
{
    /// <summary>
    /// Pour que l’utilisateur soit averti si un actif passe un certain seuil :
    /// </summary>
    public class PriceAlert : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public User User { get; set; } = null!;
        public string AssetId { get; set; }
        public Asset Asset { get; set; } = null!;
        [Column(TypeName = "decimal(18,8)")] 
        public decimal Threshold { get; set; }
        public AlertDirectionEnum Direction { get; set; }
        public bool IsActive { get; set; } = true;
    }


}
