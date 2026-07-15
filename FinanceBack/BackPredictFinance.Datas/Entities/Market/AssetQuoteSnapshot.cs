namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Enregistre un snapshot de cotation temps quasi réel pour un actif.
    /// </summary>
    public class AssetQuoteSnapshot : AuditableEntityBase, IAssetSnapshot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AssetId { get; set; } = string.Empty;
        public Asset Asset { get; set; } = null!;
        public DateTime AsOfUtc { get; set; }
        public decimal LastPrice { get; set; }
        public decimal DayVariationPct { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
