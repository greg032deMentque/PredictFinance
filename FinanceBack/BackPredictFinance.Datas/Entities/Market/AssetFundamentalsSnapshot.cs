namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Enregistre un snapshot horodaté des indicateurs fondamentaux d'un actif (capitalisation,
    /// PER, rendement du dividende) pour alimenter le filtrage du screener sans dépendre d'un
    /// appel temps réel au fournisseur de données.
    /// </summary>
    public class AssetFundamentalsSnapshot : AuditableEntityBase, IAssetSnapshot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AssetId { get; set; } = string.Empty;
        public Asset Asset { get; set; } = null!;
        public DateTime AsOfUtc { get; set; }
        public decimal? MarketCap { get; set; }
        public decimal? TrailingPE { get; set; }
        public decimal? DividendYield { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
