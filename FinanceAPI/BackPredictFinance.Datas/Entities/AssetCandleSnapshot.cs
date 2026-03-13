namespace BackPredictFinance.Datas.Entities
{
    public class AssetCandleSnapshot : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AssetId { get; set; } = string.Empty;
        public Asset Asset { get; set; } = null!;
        public DateTime TimestampUtc { get; set; }
        public string Interval { get; set; } = "1d";
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
