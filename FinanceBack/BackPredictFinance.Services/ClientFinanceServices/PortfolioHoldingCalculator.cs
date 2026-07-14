using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using Microsoft.Extensions.Logging;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    /// <summary>
    /// Position détenue reconstruite à partir des transactions d'un actif au sein d'un portefeuille.
    /// </summary>
    public sealed record PortfolioHolding
    {
        /// <summary>Quantité encore détenue après consommation FIFO des ventes.</summary>
        public required decimal Quantity { get; init; }

        /// <summary>Prix de revient unitaire moyen des lots encore détenus (frais inclus).</summary>
        public required decimal AverageBuyPrice { get; init; }

        /// <summary>Montant investi encore détenu (coût FIFO des lots restants).</summary>
        public required decimal InvestedAmount { get; init; }
    }

    /// <summary>
    /// Reconstruit la position d'un actif dans un portefeuille à partir de ses transactions.
    /// Applique un calcul FIFO (les ventes consomment les lots d'achat les plus anciens) quand
    /// l'historique est cohérent ; bascule sur un calcul net simple en repli si une vente dépasse
    /// le stock disponible (historique antérieur au FIFO ou incohérent).
    /// </summary>
    public static class PortfolioHoldingCalculator
    {
        /// <summary>
        /// Calcule la position détenue pour la liste de transactions fournie (un actif, un portefeuille).
        /// </summary>
        public static PortfolioHolding Compute(IReadOnlyCollection<AssetTransaction> transactions, ILogger? logger = null)
        {
            if (transactions.Count == 0)
            {
                return new PortfolioHolding { Quantity = 0m, AverageBuyPrice = 0m, InvestedAmount = 0m };
            }

            var ordered = transactions.OrderBy(x => x.TimestampUtc).ToList();
            var lots = new List<BuyLot>();

            foreach (var transaction in ordered)
            {
                if (transaction.TransactionType == TransactionTypeEnum.Buy)
                {
                    if (transaction.Quantity <= 0m)
                    {
                        continue;
                    }

                    lots.Add(new BuyLot
                    {
                        Quantity = transaction.Quantity,
                        UnitCost = transaction.UnitPrice + (transaction.Quantity > 0m ? transaction.Fees / transaction.Quantity : 0m)
                    });
                }
                else
                {
                    if (!TryConsumeFifo(lots, transaction.Quantity))
                    {
                        logger?.LogWarning(
                            "Calcul FIFO indisponible (vente supérieure au stock détenu) pour {Count} transactions. Repli sur le calcul net simple.",
                            transactions.Count);
                        return ComputeNetFallback(ordered);
                    }
                }
            }

            var remainingQuantity = lots.Sum(x => x.Quantity);
            var investedAmount = lots.Sum(x => x.Quantity * x.UnitCost);
            var averageBuyPrice = remainingQuantity > 0m ? investedAmount / remainingQuantity : 0m;

            return new PortfolioHolding
            {
                Quantity = remainingQuantity,
                AverageBuyPrice = decimal.Round(averageBuyPrice, 4),
                InvestedAmount = decimal.Round(investedAmount, 2)
            };
        }

        private static bool TryConsumeFifo(List<BuyLot> lots, decimal quantityToSell)
        {
            var remainingToSell = quantityToSell;

            while (remainingToSell > 0m)
            {
                if (lots.Count == 0)
                {
                    return false;
                }

                var oldest = lots[0];
                if (oldest.Quantity > remainingToSell)
                {
                    oldest.Quantity -= remainingToSell;
                    remainingToSell = 0m;
                }
                else
                {
                    remainingToSell -= oldest.Quantity;
                    lots.RemoveAt(0);
                }
            }

            return true;
        }

        private static PortfolioHolding ComputeNetFallback(IReadOnlyCollection<AssetTransaction> transactions)
        {
            var buys = transactions.Where(x => x.TransactionType == TransactionTypeEnum.Buy).ToList();
            var sells = transactions.Where(x => x.TransactionType == TransactionTypeEnum.Sell).ToList();

            var boughtQuantity = buys.Sum(x => x.Quantity);
            var soldQuantity = sells.Sum(x => x.Quantity);
            var netQuantity = Math.Max(boughtQuantity - soldQuantity, 0m);

            var totalBuyAmount = buys.Sum(x => (x.Quantity * x.UnitPrice) + x.Fees);
            var totalSellAmount = sells.Sum(x => (x.Quantity * x.UnitPrice) - x.Fees);
            var investedAmount = Math.Max(totalBuyAmount - totalSellAmount, 0m);
            var averageBuyPrice = boughtQuantity > 0m ? totalBuyAmount / boughtQuantity : 0m;

            return new PortfolioHolding
            {
                Quantity = netQuantity,
                AverageBuyPrice = decimal.Round(averageBuyPrice, 4),
                InvestedAmount = decimal.Round(investedAmount, 2)
            };
        }

        private sealed class BuyLot
        {
            public decimal Quantity { get; set; }
            public decimal UnitCost { get; init; }
        }
    }
}
