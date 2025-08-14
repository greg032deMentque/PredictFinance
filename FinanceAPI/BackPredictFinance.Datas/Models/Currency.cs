using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPredictFinance.Datas.Models
{
    /// <summary>
    /// Si l’utilisateur peut suivre des actifs libellés dans plusieurs monnaies :
    /// </summary>
    public class Currency : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Code { get; set; } = null!;  // ex. "USD", "EUR"
        public string Name { get; set; } = null!;
        [Column(TypeName = "decimal(18,8)")] 
        public decimal RateToBase { get; set; }
        public DateTime RetrievedAtUtc { get; set; }
    }



}
