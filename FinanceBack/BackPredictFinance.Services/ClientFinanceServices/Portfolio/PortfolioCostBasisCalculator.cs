using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using Microsoft.Extensions.Logging;

namespace BackPredictFinance.Services.ClientFinanceServices.PortfolioCostBasis
{
    public sealed record RealizedSale
    {
        public required DateTime SaleDate { get; init; }
        public required decimal Quantity { get; init; }
        public required decimal NetUnitSellPrice { get; init; }
        public required decimal AvgCostAtSale { get; init; }
        public required decimal RealizedPnl { get; init; }
    }

    public sealed record PortfolioCostBasisResult
    {
        public required decimal QuantityHeld { get; init; }
        public required decimal? AverageUnitCost { get; init; }
        public required decimal InvestedAmount { get; init; }
        public required IReadOnlyList<RealizedSale> RealizedSales { get; init; }
        public required bool IsHistoryConsistent { get; init; }
    }

    /// <summary>
    /// Prix de revient unique du portefeuille : prix moyen pondéré (PMP), méthode retenue par le
    /// droit fiscal français pour les cessions de valeurs mobilières fongibles de particuliers
    /// (BOI-RPPM-PVBMI-20-10-20). Frais d'acquisition inclus au coût, frais de cession déduits du
    /// prix de vente net (BOI-RPPM-PVBMI-20-10-20-10). Composant unique consommé par la watchlist,
    /// l'allocation, le contexte d'analyse et l'écran fiscal (CORR-02).
    /// </summary>
    public static class PortfolioCostBasisCalculator
    {
        public static PortfolioCostBasisResult Compute(IReadOnlyCollection<AssetTransaction> transactions, ILogger? logger = null)
        {
            if (transactions.Count == 0)
            {
                return Empty();
            }

            var ordered = transactions.OrderBy(x => x.TimestampUtc).ThenBy(x => x.Id).ToList();
            var state = new WalkState();

            foreach (var transaction in ordered)
            {
                if (transaction.TransactionType == TransactionTypeEnum.Buy)
                {
                    ApplyBuy(state, transaction);
                }
                else
                {
                    ApplySell(state, transaction, logger);
                }
            }

            return BuildResult(state);
        }

        private static void ApplyBuy(WalkState state, AssetTransaction transaction)
        {
            if (transaction.Quantity <= 0m)
            {
                return;
            }

            var unitCost = transaction.UnitPrice + (transaction.Fees / transaction.Quantity);
            var newQuantity = state.QuantityHeld + transaction.Quantity;
            state.AverageCost = newQuantity > 0m
                ? ((state.QuantityHeld * state.AverageCost) + (transaction.Quantity * unitCost)) / newQuantity
                : 0m;
            state.QuantityHeld = newQuantity;
        }

        private static void ApplySell(WalkState state, AssetTransaction transaction, ILogger? logger)
        {
            if (transaction.Quantity <= 0m || state.QuantityHeld <= 0m)
            {
                return;
            }

            var sellQuantity = transaction.Quantity;
            if (sellQuantity > state.QuantityHeld)
            {
                logger?.LogWarning(
                    "Historique de transactions incohérent : vente de {SellQuantity} supérieure au stock suivi de {HeldQuantity}. Quantité plafonnée, historique marqué non fiable.",
                    sellQuantity, state.QuantityHeld);
                sellQuantity = state.QuantityHeld;
                state.IsHistoryConsistent = false;
            }

            var netUnitSellPrice = transaction.UnitPrice - (transaction.Fees / transaction.Quantity);
            var realizedPnl = decimal.Round(sellQuantity * (netUnitSellPrice - state.AverageCost), 2);

            state.RealizedSales.Add(new RealizedSale
            {
                SaleDate = transaction.TimestampUtc,
                Quantity = sellQuantity,
                NetUnitSellPrice = netUnitSellPrice,
                AvgCostAtSale = decimal.Round(state.AverageCost, 4),
                RealizedPnl = realizedPnl
            });

            state.QuantityHeld -= sellQuantity;
            if (state.QuantityHeld == 0m)
            {
                state.AverageCost = 0m;
            }
        }

        private static PortfolioCostBasisResult BuildResult(WalkState state)
        {
            var averageUnitCost = state.QuantityHeld > 0m ? decimal.Round(state.AverageCost, 4) : (decimal?)null;
            var investedAmount = state.QuantityHeld > 0m ? decimal.Round(state.QuantityHeld * state.AverageCost, 2) : 0m;

            return new PortfolioCostBasisResult
            {
                QuantityHeld = state.QuantityHeld,
                AverageUnitCost = averageUnitCost,
                InvestedAmount = investedAmount,
                RealizedSales = state.RealizedSales,
                IsHistoryConsistent = state.IsHistoryConsistent
            };
        }

        private static PortfolioCostBasisResult Empty() => new()
        {
            QuantityHeld = 0m,
            AverageUnitCost = null,
            InvestedAmount = 0m,
            RealizedSales = [],
            IsHistoryConsistent = true
        };

        private sealed class WalkState
        {
            public decimal QuantityHeld { get; set; }
            public decimal AverageCost { get; set; }
            public bool IsHistoryConsistent { get; set; } = true;
            public List<RealizedSale> RealizedSales { get; } = [];
        }
    }
}
