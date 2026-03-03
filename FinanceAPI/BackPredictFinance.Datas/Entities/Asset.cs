using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Datas.Entities
{
    public class Asset : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Symbol { get; set; } = string.Empty;
        public string? Name { get; set; }
        public AssetTypeEnum AssetType { get; set; }

        public List<UserAsset> UserAssets { get; set; } = new List<UserAsset>();
        public List<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
    }
}
