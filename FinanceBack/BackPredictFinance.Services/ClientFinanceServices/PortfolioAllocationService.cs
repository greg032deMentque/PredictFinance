using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface IPortfolioAllocationService
    {
        Task<PortfolioAllocationViewModel> ComputeAllocationAsync(
            string userId,
            string? portfolioId,
            CancellationToken ct = default);
    }

    public sealed class PortfolioAllocationService : BaseService, IPortfolioAllocationService
    {
        private const decimal LineAlertThreshold = 0.15m;
        private const decimal SectorAlertThreshold = 0.30m;
        private const decimal SectorGroupingThreshold = 0.03m;
        private const int MaxDistinctGroups = 8;
        private const decimal HhiConcentratedThreshold = 0.25m;
        private const decimal HhiModerateThreshold = 0.10m;

        private static readonly string[] BenchmarkSymbols = ["^FCHI", "URTH", "ACWI"];

        private readonly IClientFinanceAssetSupportService _assetSupportService;
        private readonly ILogger<PortfolioAllocationService> _holdingLogger;

        public PortfolioAllocationService(
            IServiceProvider serviceProvider,
            IClientFinanceAssetSupportService assetSupportService,
            ILogger<PortfolioAllocationService> holdingLogger)
            : base(serviceProvider)
        {
            _assetSupportService = assetSupportService;
            _holdingLogger = holdingLogger;
        }

        public async Task<PortfolioAllocationViewModel> ComputeAllocationAsync(
            string userId,
            string? portfolioId,
            CancellationToken ct = default)
        {
            var userAssets = await _financeDbContext.UserAssets
                .AsNoTracking()
                .Include(x => x.Asset)
                .Where(x => x.UserId == userId && x.Quantity > 0m)
                .ToListAsync(ct);

            if (userAssets.Count == 0)
            {
                return new PortfolioAllocationViewModel { BenchmarkUnavailable = true };
            }

            var assetIds = userAssets.Select(x => x.AssetId).ToList();
            var userAssetIds = userAssets.Select(x => x.Id).ToList();

            var latestCandles = await _financeDbContext.AssetCandleSnapshots
                .AsNoTracking()
                .Where(c => assetIds.Contains(c.AssetId) && c.Interval == "1d")
                .GroupBy(c => c.AssetId)
                .Select(g => g.OrderByDescending(c => c.TimestampUtc).First())
                .ToListAsync(ct);

            var latestPriceByAssetId = latestCandles.ToDictionary(c => c.AssetId, c => c.Close);

            // Transactions scopées au portefeuille demandé, ou vérité globale (tous portefeuilles,
            // archivés inclus) quand portfolioId est vide — même convention que
            // ClientFinanceWatchlistPortfolioService.BuildWatchlistAsync, pour rester cohérent avec
            // le pitfall documenté sur la reconstruction FIFO.
            var transactionQuery = _financeDbContext.AssetTransactions
                .AsNoTracking()
                .Where(x => userAssetIds.Contains(x.UserAssetId) && !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(portfolioId))
            {
                transactionQuery = transactionQuery.Where(x => x.PortfolioId == portfolioId);
            }

            var transactions = await transactionQuery.ToListAsync(ct);
            var groupedTransactions = transactions
                .GroupBy(x => x.UserAssetId)
                .ToDictionary(x => x.Key, x => (IReadOnlyCollection<AssetTransaction>)x.ToList());

            var distinctCurrencies = userAssets
                .Select(x => x.Asset.Currency)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var forexRateByCurrency = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var currency in distinctCurrencies)
            {
                forexRateByCurrency[currency] = await _assetSupportService.GetForexRateToEurAsync(currency, ct);
            }

            var positions = new List<PositionSlice>(userAssets.Count);
            foreach (var ua in userAssets)
            {
                latestPriceByAssetId.TryGetValue(ua.AssetId, out var price);
                var forexRate = forexRateByCurrency.GetValueOrDefault(ua.Asset.Currency, 1m);

                groupedTransactions.TryGetValue(ua.Id, out var history);
                history ??= [];
                var holding = PortfolioHoldingCalculator.Compute(history, _holdingLogger);

                var quantity = string.IsNullOrWhiteSpace(portfolioId) ? ua.Quantity : holding.Quantity;
                var valueEur = quantity * price * forexRate;
                var costBasisEur = decimal.Round(holding.InvestedAmount * forexRate, 2);

                positions.Add(new PositionSlice
                {
                    Symbol = ua.Asset.Symbol,
                    Sector = string.IsNullOrWhiteSpace(ua.Asset.Sector) ? "Autres" : ua.Asset.Sector,
                    Country = string.IsNullOrWhiteSpace(ua.Asset.Country) ? "Inconnu" : ua.Asset.Country,
                    Currency = string.IsNullOrWhiteSpace(ua.Asset.Currency) ? "USD" : ua.Asset.Currency,
                    ValueEur = valueEur,
                    CostBasisEur = costBasisEur
                });
            }

            positions = positions.Where(p => p.ValueEur > 0m).ToList();
            var totalValue = positions.Sum(p => p.ValueEur);

            if (totalValue <= 0m)
            {
                return new PortfolioAllocationViewModel { BenchmarkUnavailable = true };
            }

            var sectorAllocation = BuildGroupedAllocation(
                positions.GroupBy(p => p.Sector).Select(g => (g.Key, g.Sum(x => x.ValueEur))).ToList(),
                totalValue);

            var countryAllocation = BuildGroupedAllocation(
                positions.GroupBy(p => p.Country).Select(g => (g.Key, g.Sum(x => x.ValueEur))).ToList(),
                totalValue);

            var currencyAllocation = BuildGroupedAllocation(
                positions.GroupBy(p => p.Currency).Select(g => (g.Key, g.Sum(x => x.ValueEur))).ToList(),
                totalValue);

            var hhi = positions.Sum(p => Math.Pow((double)(p.ValueEur / totalValue), 2));
            var concentrationScore = decimal.Round((decimal)(hhi * 100), 1);
            var diversificationRating = ClassifyDiversification((decimal)hhi);

            var alerts = BuildAlerts(positions, totalValue, sectorAllocation);

            var ret365d = ComputeSimplePortfolioReturn365d(positions, totalValue);

            var benchmarkUnavailable = true;
            decimal? benchmarkReturn30d = null;
            decimal? benchmarkReturn90d = null;
            decimal? benchmarkReturn365d = null;

            var benchmarkAsset = await _financeDbContext.Assets
                .AsNoTracking()
                .FirstOrDefaultAsync(a => BenchmarkSymbols.Contains(a.Symbol), ct);

            if (benchmarkAsset != null)
            {
                var (b30, b90, b365) = await ComputeBenchmarkReturnsAsync(benchmarkAsset.Id, ct);
                if (b30 != null || b90 != null || b365 != null)
                {
                    benchmarkUnavailable = false;
                    benchmarkReturn30d = b30;
                    benchmarkReturn90d = b90;
                    benchmarkReturn365d = b365;
                }
            }

            return new PortfolioAllocationViewModel
            {
                SectorAllocation = sectorAllocation,
                CountryAllocation = countryAllocation,
                CurrencyAllocation = currencyAllocation,
                ConcentrationScore = concentrationScore,
                DiversificationRating = diversificationRating,
                ConcentrationAlerts = alerts,
                PortfolioReturn30d = null,
                PortfolioReturn90d = null,
                PortfolioReturn365d = ret365d,
                BenchmarkReturn30d = benchmarkReturn30d,
                BenchmarkReturn90d = benchmarkReturn90d,
                BenchmarkReturn365d = benchmarkReturn365d,
                BenchmarkUnavailable = benchmarkUnavailable
            };
        }

        private static List<AllocationSliceViewModel> BuildGroupedAllocation(
            List<(string Label, decimal ValueEur)> groups,
            decimal totalValue)
        {
            var ordered = groups
                .Select(g => new AllocationSliceViewModel
                {
                    Label = g.Label,
                    ValueEur = decimal.Round(g.ValueEur, 2),
                    WeightPct = totalValue > 0m ? decimal.Round(g.ValueEur / totalValue * 100m, 2) : 0m
                })
                .OrderByDescending(x => x.WeightPct)
                .ToList();

            if (ordered.Count <= MaxDistinctGroups)
            {
                return ordered;
            }

            var main = ordered.Where(x => x.WeightPct / 100m >= SectorGroupingThreshold).ToList();
            var autres = ordered.Where(x => x.WeightPct / 100m < SectorGroupingThreshold).ToList();

            if (autres.Count == 0)
            {
                return ordered;
            }

            main.Add(new AllocationSliceViewModel
            {
                Label = "Autres",
                WeightPct = decimal.Round(autres.Sum(x => x.WeightPct), 2),
                ValueEur = decimal.Round(autres.Sum(x => x.ValueEur), 2)
            });

            return main;
        }

        private static List<ConcentrationAlertViewModel> BuildAlerts(
            List<PositionSlice> positions,
            decimal totalValue,
            List<AllocationSliceViewModel> sectorAllocation)
        {
            var alerts = new List<ConcentrationAlertViewModel>();

            foreach (var pos in positions)
            {
                var weight = totalValue > 0m ? pos.ValueEur / totalValue : 0m;
                if (weight > LineAlertThreshold)
                {
                    alerts.Add(new ConcentrationAlertViewModel
                    {
                        Message = $"{pos.Symbol} représente {weight * 100m:F1}% du portefeuille"
                    });
                }
            }

            foreach (var sector in sectorAllocation)
            {
                if (sector.Label == "Autres")
                {
                    continue;
                }

                if (sector.WeightPct / 100m > SectorAlertThreshold)
                {
                    alerts.Add(new ConcentrationAlertViewModel
                    {
                        Message = $"Secteur {sector.Label} représente {sector.WeightPct:F1}%"
                    });
                }
            }

            return alerts;
        }

        private static DiversificationRating ClassifyDiversification(decimal hhi)
        {
            if (hhi > HhiConcentratedThreshold)
            {
                return DiversificationRating.Concentrated;
            }

            if (hhi > HhiModerateThreshold)
            {
                return DiversificationRating.Moderate;
            }

            return DiversificationRating.Diversified;
        }

        private static decimal? ComputeSimplePortfolioReturn365d(List<PositionSlice> positions, decimal totalCurrentValue)
        {
            var totalCost = positions.Sum(p => p.CostBasisEur);
            if (totalCost <= 0m)
            {
                return null;
            }

            return decimal.Round((totalCurrentValue - totalCost) / totalCost, 4);
        }

        private async Task<(decimal? Return30d, decimal? Return90d, decimal? Return365d)> ComputeBenchmarkReturnsAsync(
            string benchmarkAssetId,
            CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var cutoff = now.AddDays(-365);

            var candles = await _financeDbContext.AssetCandleSnapshots
                .AsNoTracking()
                .Where(c => c.AssetId == benchmarkAssetId && c.Interval == "1d" && c.TimestampUtc >= cutoff)
                .OrderBy(c => c.TimestampUtc)
                .ToListAsync(ct);

            if (candles.Count < 2)
            {
                return (null, null, null);
            }

            var latest = candles[^1];
            var ret30d = ComputeReturnFromDaysAgo(candles, latest.Close, 30, now);
            var ret90d = ComputeReturnFromDaysAgo(candles, latest.Close, 90, now);
            var ret365d = ComputeReturnFromDaysAgo(candles, latest.Close, 365, now);

            return (ret30d, ret90d, ret365d);
        }

        private static decimal? ComputeReturnFromDaysAgo(
            List<Datas.Entities.AssetCandleSnapshot> candles,
            decimal latestClose,
            int daysAgo,
            DateTime now)
        {
            var cutoff = now.AddDays(-daysAgo);
            var reference = candles
                .Where(c => c.TimestampUtc <= cutoff)
                .OrderByDescending(c => c.TimestampUtc)
                .FirstOrDefault();

            if (reference == null || reference.Close <= 0m)
            {
                return null;
            }

            return decimal.Round((latestClose - reference.Close) / reference.Close, 4);
        }

        private sealed class PositionSlice
        {
            public string Symbol { get; init; } = string.Empty;
            public string Sector { get; init; } = string.Empty;
            public string Country { get; init; } = string.Empty;
            public string Currency { get; init; } = string.Empty;
            public decimal ValueEur { get; init; }
            public decimal CostBasisEur { get; init; }
        }
    }
}
