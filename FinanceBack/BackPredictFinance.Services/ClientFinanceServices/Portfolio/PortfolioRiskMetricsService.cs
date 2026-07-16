using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Portfolio;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices.PortfolioMetrics
{
    public interface IPortfolioRiskMetricsService
    {
        Task<PortfolioRiskMetricsViewModel?> GetMetricsAsync(string portfolioId, CancellationToken ct = default);
    }

    public sealed class PortfolioRiskMetricsService : BaseService, IPortfolioRiskMetricsService
    {
        private const int MinDataPoints = 20;
        private const decimal TradingDaysPerYear = 252m;

        public PortfolioRiskMetricsService(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public async Task<PortfolioRiskMetricsViewModel?> GetMetricsAsync(string portfolioId, CancellationToken ct = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(portfolioId);

            var portfolio = await _financeDbContext.Portfolios
                .AsNoTracking()
                .Where(p => p.Id == portfolioId && p.UserId == _currentUserId && !p.IsDeleted)
                .FirstOrDefaultAsync(ct);

            if (portfolio == null) return null;

            var transactions = await _financeDbContext.AssetTransactions
                .AsNoTracking()
                .Include(t => t.UserAsset).ThenInclude(ua => ua.Asset)
                .Where(t => t.PortfolioId == portfolioId && !t.IsDeleted)
                .OrderBy(t => t.TimestampUtc)
                .ToListAsync(ct);

            if (transactions.Count == 0)
                return new PortfolioRiskMetricsViewModel { DataPointsUsed = 0 };

            var assetIds = transactions.Select(t => t.UserAsset.AssetId).Distinct().ToList();

            var priceHistories = await _financeDbContext.PriceHistories
                .AsNoTracking()
                .Where(ph => assetIds.Contains(ph.AssetId))
                .OrderBy(ph => ph.AssetId).ThenBy(ph => ph.RetrievedAtUtc)
                .ToListAsync(ct);

            var assetPrices = BuildAssetPriceLookup(priceHistories);
            var transactionDates = transactions.Select(t => t.TimestampUtc.Date).ToHashSet();
            var valueSeries = BuildPortfolioValueSeries(transactions, assetPrices);

            if (valueSeries.Count < 2)
                return new PortfolioRiskMetricsViewModel { DataPointsUsed = valueSeries.Count };

            var dailyReturns = ComputeDailyReturns(valueSeries, transactionDates);

            if (dailyReturns.Count < MinDataPoints)
                return new PortfolioRiskMetricsViewModel { DataPointsUsed = dailyReturns.Count };

            return ComputeMetrics(dailyReturns, valueSeries);
        }

        private static Dictionary<string, List<(DateTime Date, decimal Price)>> BuildAssetPriceLookup(
            List<PriceHistory> histories)
        {
            return histories
                .GroupBy(p => p.AssetId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(p => p.RetrievedAtUtc.Date)
                          .Select(dg => (dg.Key, dg.OrderByDescending(p => p.RetrievedAtUtc).First().Price))
                          .OrderBy(x => x.Key)
                          .ToList());
        }

        private static decimal? GetLatestPrice(
            Dictionary<string, List<(DateTime Date, decimal Price)>> lookup,
            string assetId,
            DateTime date)
        {
            if (!lookup.TryGetValue(assetId, out var prices)) return null;
            var idx = prices.FindLastIndex(p => p.Date <= date);
            return idx < 0 ? null : (decimal?)prices[idx].Price;
        }

        private static List<(DateTime Date, decimal Value)> BuildPortfolioValueSeries(
            List<AssetTransaction> transactions,
            Dictionary<string, List<(DateTime Date, decimal Price)>> assetPrices)
        {
            var firstTxDate = transactions.Min(t => t.TimestampUtc.Date);

            var allDates = assetPrices.Values
                .SelectMany(prices => prices.Select(p => p.Date))
                .Where(d => d >= firstTxDate)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (allDates.Count == 0) return [];

            var txByDate = transactions
                .GroupBy(t => t.TimestampUtc.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            var holdings = new Dictionary<string, decimal>();
            var result = new List<(DateTime, decimal)>();

            foreach (var date in allDates)
            {
                if (txByDate.TryGetValue(date, out var dayTxs))
                {
                    foreach (var tx in dayTxs.OrderBy(t => t.TimestampUtc))
                    {
                        holdings.TryAdd(tx.UserAsset.AssetId, 0m);
                        holdings[tx.UserAsset.AssetId] += tx.TransactionType == TransactionTypeEnum.Buy
                            ? tx.Quantity
                            : -tx.Quantity;
                    }
                }

                decimal portfolioValue = 0m;
                foreach (var (assetId, qty) in holdings)
                {
                    if (qty <= 0m) continue;
                    var price = GetLatestPrice(assetPrices, assetId, date);
                    if (price.HasValue)
                        portfolioValue += qty * price.Value;
                }

                if (portfolioValue > 0m)
                    result.Add((date, portfolioValue));
            }

            return result;
        }

        private static List<decimal> ComputeDailyReturns(
            List<(DateTime Date, decimal Value)> valueSeries,
            HashSet<DateTime> transactionDates)
        {
            var returns = new List<decimal>();
            for (int i = 1; i < valueSeries.Count; i++)
            {
                if (transactionDates.Contains(valueSeries[i].Date)) continue;
                if (valueSeries[i - 1].Value <= 0m) continue;
                returns.Add((valueSeries[i].Value - valueSeries[i - 1].Value) / valueSeries[i - 1].Value);
            }
            return returns;
        }

        private static PortfolioRiskMetricsViewModel ComputeMetrics(
            List<decimal> dailyReturns,
            List<(DateTime Date, decimal Value)> valueSeries)
        {
            var twr = ComputeTwr(dailyReturns);
            var vol = ComputeAnnualizedVolatility(dailyReturns);
            var annualizedReturn = ComputeAnnualizedReturn(dailyReturns);
            var sharpe = vol is > 0m ? annualizedReturn / vol : null;
            var maxDrawdown = ComputeMaxDrawdown(valueSeries);

            return new PortfolioRiskMetricsViewModel
            {
                DataPointsUsed = dailyReturns.Count,
                Twr = twr,
                AnnualizedVolatility = vol,
                SharpeRatio = sharpe.HasValue ? Math.Round(sharpe.Value, 2) : null,
                MaxDrawdown = maxDrawdown,
                PeriodStartUtc = valueSeries.Count > 0 ? valueSeries[0].Date : null,
                PeriodEndUtc = valueSeries.Count > 0 ? valueSeries[^1].Date : null
            };
        }

        private static decimal ComputeTwr(List<decimal> dailyReturns)
        {
            var cumulative = dailyReturns.Aggregate(1m, (acc, r) => acc * (1m + r)) - 1m;
            return Math.Round(cumulative, 4);
        }

        private static decimal ComputeAnnualizedReturn(List<decimal> dailyReturns)
        {
            var product = dailyReturns.Aggregate(1m, (acc, r) => acc * (1m + r));
            var exponent = (double)TradingDaysPerYear / dailyReturns.Count;
            return (decimal)Math.Pow((double)product, exponent) - 1m;
        }

        private static decimal? ComputeAnnualizedVolatility(List<decimal> dailyReturns)
        {
            if (dailyReturns.Count < 2) return null;
            var mean = dailyReturns.Average();
            var variance = dailyReturns.Sum(r => (r - mean) * (r - mean)) / (dailyReturns.Count - 1);
            var dailyVol = (decimal)Math.Sqrt((double)variance);
            return Math.Round(dailyVol * (decimal)Math.Sqrt((double)TradingDaysPerYear), 4);
        }

        private static decimal? ComputeMaxDrawdown(List<(DateTime Date, decimal Value)> valueSeries)
        {
            if (valueSeries.Count < 2) return null;
            decimal peak = valueSeries[0].Value;
            decimal maxDd = 0m;
            foreach (var (_, value) in valueSeries)
            {
                if (value > peak) peak = value;
                if (peak > 0m)
                {
                    var dd = (value - peak) / peak;
                    if (dd < maxDd) maxDd = dd;
                }
            }
            return Math.Round(maxDd, 4);
        }
    }
}
