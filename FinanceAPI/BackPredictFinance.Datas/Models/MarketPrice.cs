using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackPredictFinance.Datas.Models
{
    public class MarketPrice : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public DateTime Date { get; set; }

        public decimal Open { get; set; }

        public decimal High { get; set; }

        public decimal Low { get; set; }

        public decimal Close { get; set; }

        public decimal? Volume { get; set; }

        public string AssetId { get; set; }

        public Asset Asset { get; set; } = null!;
    }
}
