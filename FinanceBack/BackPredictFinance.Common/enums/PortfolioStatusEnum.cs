namespace BackPredictFinance.Common.enums
{
    /// <summary>
    /// Statut d'un portefeuille. Un portefeuille archivé reste consultable directement
    /// mais est exclu des agrégats globaux.
    /// </summary>
    public enum PortfolioStatusEnum
    {
        Active,
        Archived
    }
}
