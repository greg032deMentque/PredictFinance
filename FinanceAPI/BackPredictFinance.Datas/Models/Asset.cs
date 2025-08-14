using BackPredictFinance.Common.enums;
using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.Datas.Models
{
    public class Asset : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();  // conserve GUID ou symbole

        public string Symbol { get; set; } = string.Empty;
        public string? Name { get; set; }
        public AssetTypeEnum AssetType { get; set; }

        /// <summary>
        /// Prix agrégés journaliers ou historiques (si nécessaire)
        /// </summary>
        public List<MarketPrice> Prices { get; set; } = new List<MarketPrice>();

        /// <summary>
        /// Histoire des valeurs récupérées à chaque appel d'API
        /// </summary>
        public List<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();

        /// <summary>
        /// Utilisateurs abonnés à cet actif
        /// </summary>
        public List<UserAsset> UserAssets { get; set; } = new List<UserAsset>();
        public List<PatternPrediction> PatternPredictions { get; set; } = new();

    }
}
