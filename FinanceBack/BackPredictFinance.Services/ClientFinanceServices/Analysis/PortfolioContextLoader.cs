using BackPredictFinance.Datas.Context;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Services.ClientFinanceServices.PortfolioCostBasis;
using Microsoft.EntityFrameworkCore;
using BackPredictFinance.Services.ClientFinanceServices;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
/// <summary>
/// Charge le contexte portefeuille utilisé pour contextualiser une analyse.
/// </summary>
public interface IPortfolioContextLoader
{
    /// <summary>
    /// Tente de reconstruire le contexte portefeuille courant d'un utilisateur pour un instrument.
    /// </summary>
    Task<PortfolioContext?> TryLoadAsync(string userId, string instrumentId, CancellationToken ct = default);
}


    /// <summary>
    /// Implémente la reconstruction du contexte portefeuille à partir des transactions persistées.
    /// Ne lève jamais d'exception sur un historique incohérent (CORR-02) : dégrade et signale via
    /// <see cref="PortfolioContext.HasDataIntegrityWarning"/>, pour ne jamais bloquer le pipeline
    /// d'analyse ou la page de détail d'un instrument sur une seule transaction mal saisie.
    /// </summary>
    public sealed class PortfolioContextLoader : IPortfolioContextLoader
    {
        private readonly FinanceDbContext _financeDbContext;

        public PortfolioContextLoader(FinanceDbContext financeDbContext)
        {
            _financeDbContext = financeDbContext;
        }

        public async Task<PortfolioContext?> TryLoadAsync(string userId, string instrumentId, CancellationToken ct = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            ArgumentException.ThrowIfNullOrWhiteSpace(instrumentId);

            var userAsset = await _financeDbContext.UserAssets
                .AsNoTracking()
                .Include(x => x.Asset)
                .FirstOrDefaultAsync(x => x.UserId == userId && x.AssetId == instrumentId, ct);

            var currencyCode = userAsset?.Asset.Currency ?? string.Empty;
            if (userAsset == null)
            {
                return BuildEmptyContext(userId, instrumentId, currencyCode);
            }

            var transactions = await _financeDbContext.AssetTransactions
                .AsNoTracking()
                .Where(x => x.UserAssetId == userAsset.Id && !x.IsDeleted)
                .OrderBy(x => x.TimestampUtc)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);

            if (transactions.Count == 0)
            {
                return userAsset.Quantity > 0m
                    ? BuildQuantityOnlyContext(userId, instrumentId, userAsset.Quantity, currencyCode)
                    : BuildEmptyContext(userId, instrumentId, currencyCode);
            }

            var result = PortfolioCostBasisCalculator.Compute(transactions);
            var quantityMismatch = result.QuantityHeld != userAsset.Quantity;
            var hasDataIntegrityWarning = !result.IsHistoryConsistent || quantityMismatch;

            if (result.QuantityHeld <= 0m)
            {
                return BuildEmptyContext(userId, instrumentId, currencyCode, hasDataIntegrityWarning);
            }

            var buyDates = transactions
                .Where(x => x.TransactionType == TransactionTypeEnum.Buy)
                .Select(x => DateOnly.FromDateTime(x.TimestampUtc))
                .ToList();

            // Sous PMP, la position est une ligne unique fusionnée (plus de lots FIFO datés) :
            // les dates d'achat les plus anciennes/récentes de l'historique complet approximent
            // OldestOpenBuyDate/LatestOpenBuyDate. Aucun consommateur ne lit ces deux champs pour
            // une décision métier (vérifié) — seule la valeur agrégée AverageUnitCost est affichée.
            var openLine = new PortfolioContextLine
            {
                Quantity = result.QuantityHeld,
                UnitBuyPrice = result.AverageUnitCost ?? 0m,
                BuyDate = buyDates.Count > 0 ? buyDates.Max() : DateOnly.FromDateTime(transactions[^1].TimestampUtc),
                FeesAmount = 0m,
                CurrencyCode = currencyCode
            };

            return new PortfolioContext
            {
                UserId = userId,
                InstrumentId = instrumentId,
                HoldsInstrument = true,
                OpenLineCount = 1,
                TotalQuantityHeld = result.QuantityHeld,
                AverageUnitCost = result.AverageUnitCost,
                CurrencyCode = currencyCode,
                OpenLines = [openLine],
                OldestOpenBuyDate = buyDates.Count > 0 ? buyDates.Min() : null,
                LatestOpenBuyDate = buyDates.Count > 0 ? buyDates.Max() : null,
                HasDataIntegrityWarning = hasDataIntegrityWarning
            };
        }

        private static PortfolioContext BuildEmptyContext(
            string userId,
            string instrumentId,
            string currencyCode,
            bool hasDataIntegrityWarning = false)
        {
            return new PortfolioContext
            {
                UserId = userId,
                InstrumentId = instrumentId,
                HoldsInstrument = false,
                OpenLineCount = 0,
                TotalQuantityHeld = 0m,
                AverageUnitCost = null,
                CurrencyCode = currencyCode,
                OpenLines = [],
                HasDataIntegrityWarning = hasDataIntegrityWarning
            };
        }

        private static PortfolioContext BuildQuantityOnlyContext(
            string userId,
            string instrumentId,
            decimal quantity,
            string currencyCode)
        {
            return new PortfolioContext
            {
                UserId = userId,
                InstrumentId = instrumentId,
                HoldsInstrument = true,
                OpenLineCount = 0,
                TotalQuantityHeld = quantity,
                AverageUnitCost = null,
                CurrencyCode = currencyCode,
                OpenLines = [],
                HasDataIntegrityWarning = true
            };
        }
    }
}
