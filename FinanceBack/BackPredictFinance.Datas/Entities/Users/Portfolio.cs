using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Portefeuille nommé appartenant à un utilisateur (PEA, assurance vie, compte-titres, etc.).
    /// Soft-delete : un portefeuille supprimé est conservé (IsDeleted) mais n'est plus affiché.
    /// Archivage : un portefeuille archivé (Status) reste consultable directement mais est
    /// exclu des agrégats globaux.
    /// </summary>
    public class Portfolio : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public string Name { get; set; } = string.Empty;

        public PortfolioTypeEnum PortfolioType { get; set; }

        public PortfolioStatusEnum Status { get; set; } = PortfolioStatusEnum.Active;

        public bool IsDeleted { get; set; }

        public List<AssetTransaction> Transactions { get; set; } = new();
    }
}
