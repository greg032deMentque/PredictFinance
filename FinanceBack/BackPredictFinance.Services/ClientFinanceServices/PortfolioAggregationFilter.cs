using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    /// <summary>
    /// Point unique d'exclusion des portefeuilles archivés pour les agrégats globaux.
    /// Les accès ciblés à un portefeuille précis (par portfolioId) ne doivent PAS utiliser
    /// ce filtre : un portefeuille archivé reste consultable directement.
    /// </summary>
    public static class PortfolioAggregationFilter
    {
        /// <summary>
        /// Exclut les transactions rattachées à un portefeuille archivé.
        /// À appliquer uniquement sur les calculs transverses (tous portefeuilles confondus).
        /// </summary>
        public static IQueryable<AssetTransaction> ExcludeArchivedPortfolios(this IQueryable<AssetTransaction> query)
            => query.Where(x => x.Portfolio.Status != PortfolioStatusEnum.Archived);
    }
}
