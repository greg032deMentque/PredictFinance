using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Tax;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices.Tax
{
    public interface ITaxService
    {
        Task<List<TaxSummaryViewModel>> GetAllPortfoliosTaxSummaryAsync(int year, CancellationToken ct = default);
    }

    public sealed class TaxService : BaseService, ITaxService
    {
        private const decimal PfuRate = 30m;
        private const decimal PeaReducedRate = 17.2m;
        private const int PeaExemptionYears = 5;

        public TaxService(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public async Task<List<TaxSummaryViewModel>> GetAllPortfoliosTaxSummaryAsync(int year, CancellationToken ct = default)
        {
            var portfolios = await _financeDbContext.Portfolios
                .AsNoTracking()
                .Where(p => p.UserId == _currentUserId && !p.IsDeleted)
                .ToListAsync(ct);

            if (portfolios.Count == 0) return [];

            var portfolioIds = portfolios.Select(p => p.Id).ToList();

            var allTransactions = await _financeDbContext.AssetTransactions
                .AsNoTracking()
                .Include(t => t.UserAsset).ThenInclude(ua => ua.Asset)
                .Where(t => portfolioIds.Contains(t.PortfolioId) && !t.IsDeleted)
                .OrderBy(t => t.PortfolioId).ThenBy(t => t.UserAsset.AssetId).ThenBy(t => t.TimestampUtc)
                .ToListAsync(ct);

            var txByPortfolio = allTransactions.GroupBy(t => t.PortfolioId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<TaxSummaryViewModel>();

            foreach (var portfolio in portfolios)
            {
                txByPortfolio.TryGetValue(portfolio.Id, out var transactions);
                var summary = ComputePortfolioSummary(portfolio, transactions ?? [], year);
                result.Add(summary);
            }

            return result;
        }

        private static TaxSummaryViewModel ComputePortfolioSummary(
            Portfolio portfolio,
            List<AssetTransaction> transactions,
            int year)
        {
            var firstTxDate = transactions.Count > 0
                ? transactions.Min(t => t.TimestampUtc)
                : (DateTime?)null;

            var (taxRate, peaYears) = DetermineTaxRate(portfolio.PortfolioType, firstTxDate, year);

            var txByAsset = transactions
                .GroupBy(t => t.UserAsset.AssetId)
                .ToDictionary(g => g.Key, g => g.OrderBy(t => t.TimestampUtc).ToList());

            var positions = new List<RealizedPositionViewModel>();

            foreach (var (_, assetTxs) in txByAsset)
            {
                var sales = ComputeRealizedSales(assetTxs, year);
                if (sales.Count == 0) continue;

                var asset = assetTxs[0].UserAsset.Asset;
                positions.Add(new RealizedPositionViewModel
                {
                    Symbol = asset.Symbol,
                    DisplayName = asset.Name ?? asset.Symbol,
                    RealizedPnl = sales.Sum(s => s.RealizedPnl),
                    Sales = sales
                });
            }

            var totalPnl = positions.Sum(p => p.RealizedPnl);
            var taxableGain = totalPnl > 0m ? totalPnl : 0m;

            return new TaxSummaryViewModel
            {
                Year = year,
                PortfolioId = portfolio.Id,
                PortfolioName = portfolio.Name,
                PortfolioTypeLabel = GetPortfolioTypeLabel(portfolio.PortfolioType),
                TaxRatePct = taxRate,
                PeaAncienneteYears = peaYears,
                TotalRealizedPnl = Math.Round(totalPnl, 2),
                EstimatedTax = Math.Round(taxableGain * taxRate / 100m, 2),
                Positions = positions.OrderByDescending(p => Math.Abs(p.RealizedPnl)).ToList()
            };
        }

        private static List<RealizedSaleViewModel> ComputeRealizedSales(
            List<AssetTransaction> transactions,
            int year)
        {
            decimal avgCost = 0m;
            decimal qtyHeld = 0m;
            var sales = new List<RealizedSaleViewModel>();

            foreach (var tx in transactions)
            {
                if (tx.TransactionType == TransactionTypeEnum.Buy)
                {
                    if (qtyHeld + tx.Quantity > 0m)
                        avgCost = (qtyHeld * avgCost + tx.Quantity * tx.UnitPrice) / (qtyHeld + tx.Quantity);
                    qtyHeld += tx.Quantity;
                }
                else
                {
                    var sellQty = Math.Min(tx.Quantity, qtyHeld);
                    if (sellQty <= 0m || qtyHeld <= 0m) continue;

                    var pnl = Math.Round(sellQty * (tx.UnitPrice - avgCost), 2);
                    qtyHeld -= sellQty;

                    if (tx.TimestampUtc.Year != year) continue;

                    sales.Add(new RealizedSaleViewModel
                    {
                        SaleDate = tx.TimestampUtc,
                        Quantity = sellQty,
                        SellPrice = tx.UnitPrice,
                        AvgCostAtSale = Math.Round(avgCost, 4),
                        RealizedPnl = pnl
                    });
                }
            }

            return sales;
        }

        private static (decimal Rate, int? PeaYears) DetermineTaxRate(
            PortfolioTypeEnum type,
            DateTime? firstTxDate,
            int year)
        {
            if (type != PortfolioTypeEnum.Pea)
                return (PfuRate, null);

            if (firstTxDate == null)
                return (PfuRate, null);

            var refDate = new DateTime(year, 12, 31);
            var years = (int)((refDate - firstTxDate.Value).TotalDays / 365.25);
            var rate = years >= PeaExemptionYears ? PeaReducedRate : PfuRate;
            return (rate, years);
        }

        private static string GetPortfolioTypeLabel(PortfolioTypeEnum type) => type switch
        {
            PortfolioTypeEnum.Pea => "PEA",
            PortfolioTypeEnum.CompteTitres => "Compte-titres",
            PortfolioTypeEnum.AssuranceVie => "Assurance-vie",
            PortfolioTypeEnum.Per => "PER",
            _ => "Autre"
        };
    }
}
