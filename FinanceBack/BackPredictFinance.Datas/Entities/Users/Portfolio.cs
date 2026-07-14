using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Portefeuille nommé appartenant à un utilisateur (PEA, assurance vie, compte-titres, etc.).
    /// Soft-delete : un portefeuille supprimé est conservé (IsDeleted) mais n'est plus affiché.
    /// </summary>
    public class Portfolio : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public string Name { get; set; } = string.Empty;

        public PortfolioTypeEnum PortfolioType { get; set; }

        public bool IsDeleted { get; set; }

        public List<AssetTransaction> Transactions { get; set; } = new();
    }
}
