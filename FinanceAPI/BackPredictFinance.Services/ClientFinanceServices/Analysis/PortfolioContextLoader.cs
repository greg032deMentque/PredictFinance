using BackPredictFinance.Datas.Context;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Contracts.Analysis;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{

public interface IPortfolioContextLoader
{
    Task<PortfolioContext?> TryLoadAsync(string userId, string instrumentId, DateOnly? asOfDate, CancellationToken ct = default);
}


    public sealed class PortfolioContextLoader : IPortfolioContextLoader
    {
        private readonly FinanceDbContext _financeDbContext;

        public PortfolioContextLoader(FinanceDbContext financeDbContext)
        {
            _financeDbContext = financeDbContext;
        }

        public async Task<PortfolioContext?> TryLoadAsync(string userId, string instrumentId, DateOnly? asOfDate, CancellationToken ct = default)
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

            var transactionQuery = _financeDbContext.AssetTransactions
                .AsNoTracking()
                .Where(x => x.UserAssetId == userAsset.Id);

            if (asOfDate.HasValue)
            {
                var endOfDayUtc = DateTime.SpecifyKind(asOfDate.Value.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);
                transactionQuery = transactionQuery.Where(x => x.TimestampUtc <= endOfDayUtc);
            }

            var transactions = await transactionQuery
                .OrderBy(x => x.TimestampUtc)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);

            if (transactions.Count == 0)
            {
                return userAsset.Quantity > 0m
                    ? throw new InvalidOperationException("Le contexte portefeuille ne peut pas etre reconstruit sans historique de transactions.")
                    : BuildEmptyContext(userId, instrumentId, currencyCode);
            }

            var openLines = new Queue<ReconstructedPortfolioLine>();
            foreach (var transaction in transactions)
            {
                if (transaction.TransactionType == TransactionTypeEnum.Buy)
                {
                    openLines.Enqueue(new ReconstructedPortfolioLine
                    {
                        OriginalQuantity = transaction.Quantity,
                        RemainingQuantity = transaction.Quantity,
                        UnitBuyPrice = transaction.UnitPrice,
                        BuyDate = DateOnly.FromDateTime(transaction.TimestampUtc),
                        RemainingFeesAmount = transaction.Fees
                    });
                    continue;
                }

                var remainingToConsume = transaction.Quantity;
                while (remainingToConsume > 0m && openLines.Count > 0)
                {
                    var line = openLines.Peek();
                    var quantityBeforeConsumption = line.RemainingQuantity;
                    var consumedQuantity = Math.Min(quantityBeforeConsumption, remainingToConsume);
                    if (quantityBeforeConsumption <= 0m)
                    {
                        openLines.Dequeue();
                        continue;
                    }

                    var consumedFeesAmount = line.RemainingFeesAmount * (consumedQuantity / quantityBeforeConsumption);
                    line.RemainingQuantity -= consumedQuantity;
                    line.RemainingFeesAmount -= consumedFeesAmount;
                    remainingToConsume -= consumedQuantity;

                    if (line.RemainingQuantity == 0m)
                    {
                        line.RemainingFeesAmount = 0m;
                        openLines.Dequeue();
                    }
                }

                if (remainingToConsume > 0m)
                {
                    throw new InvalidOperationException("Le contexte portefeuille FIFO est incoherent: une vente depasse les quantites achetees disponibles.");
                }
            }

            var remainingOpenLines = openLines
                .Where(x => x.RemainingQuantity > 0m)
                .Select(x => new PortfolioContextLine
                {
                    Quantity = x.RemainingQuantity,
                    UnitBuyPrice = x.UnitBuyPrice,
                    BuyDate = x.BuyDate,
                    FeesAmount = x.RemainingFeesAmount,
                    CurrencyCode = currencyCode
                })
                .ToList();

            if (remainingOpenLines.Count == 0)
            {
                return BuildEmptyContext(userId, instrumentId, currencyCode);
            }

            var totalQuantityHeld = remainingOpenLines.Sum(x => x.Quantity);
            if (totalQuantityHeld != userAsset.Quantity)
            {
                throw new InvalidOperationException("Le contexte portefeuille FIFO est incoherent: la quantite reconstruite ne correspond pas a la quantite agregee persistee.");
            }

            var totalCost = remainingOpenLines.Sum(x => (x.Quantity * x.UnitBuyPrice) + x.FeesAmount);

            return new PortfolioContext
            {
                UserId = userId,
                InstrumentId = instrumentId,
                HoldsInstrument = true,
                OpenLineCount = remainingOpenLines.Count,
                TotalQuantityHeld = totalQuantityHeld,
                AverageUnitCost = totalQuantityHeld > 0m ? totalCost / totalQuantityHeld : null,
                CurrencyCode = currencyCode,
                OpenLines = remainingOpenLines,
                OldestOpenBuyDate = remainingOpenLines.Min(x => x.BuyDate),
                LatestOpenBuyDate = remainingOpenLines.Max(x => x.BuyDate)
            };
        }

        private static PortfolioContext BuildEmptyContext(string userId, string instrumentId, string currencyCode)
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
                OpenLines = []
            };
        }

        private sealed class ReconstructedPortfolioLine
        {
            public decimal OriginalQuantity { get; set; }
            public decimal RemainingQuantity { get; set; }
            public decimal UnitBuyPrice { get; set; }
            public DateOnly BuyDate { get; set; }
            public decimal RemainingFeesAmount { get; set; }
        }
    }
}
