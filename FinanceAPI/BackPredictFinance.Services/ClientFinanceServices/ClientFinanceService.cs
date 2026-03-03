using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.PythonServices;
using BackPredictFinance.Services.PythonServices.Models;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface IClientFinanceService
    {
        Task<ClientDashboardViewModel> GetDashboardAsync(string userId, CancellationToken ct = default);
        Task<List<AssetSearchItemViewModel>> SearchAssetsAsync(string query, CancellationToken ct = default);
        Task<List<WatchlistItemViewModel>> GetWatchlistAsync(string userId, CancellationToken ct = default);
        Task<WatchlistItemViewModel> AddToWatchlistAsync(string userId, WatchlistUpsertRequestViewModel request, CancellationToken ct = default);
        Task RemoveFromWatchlistAsync(string userId, string symbol, CancellationToken ct = default);
        Task<LiveQuoteViewModel> GetLiveQuoteAsync(string symbol, CancellationToken ct = default);
        Task<TransactionItemViewModel> RegisterTransactionAsync(string userId, TransactionCreateRequestViewModel request, CancellationToken ct = default);
        Task<List<TransactionItemViewModel>> GetTransactionsAsync(string userId, int take, CancellationToken ct = default);
        Task DeleteTransactionAsync(string userId, string transactionId, CancellationToken ct = default);
        Task<AnalysisResultViewModel> RunAnalysisAsync(string userId, AnalysisRunRequestViewModel request, CancellationToken ct = default);
        Task<List<AnalysisResultViewModel>> GetRecentAnalysesAsync(string userId, int take, CancellationToken ct = default);
        Task<SimulationResultViewModel> RunSimulationAsync(string userId, SimulationRequestViewModel request, CancellationToken ct = default);
    }

    public class ClientFinanceService : BaseService, IClientFinanceService
    {
        private readonly ITickerService _tickerService;
        private readonly IPythonApiService _pythonApiService;

        public ClientFinanceService(
            IServiceProvider serviceProvider,
            ITickerService tickerService,
            IPythonApiService pythonApiService)
            : base(serviceProvider)
        {
            _tickerService = tickerService;
            _pythonApiService = pythonApiService;
        }

        public async Task<ClientDashboardViewModel> GetDashboardAsync(string userId, CancellationToken ct = default)
        {
            var watchlist = await GetWatchlistAsync(userId, ct);
            var analysesWeekStart = DateTime.UtcNow.AddDays(-7);

            var userAssetIds = await _financeDbContext.UserAssets
                .Where(x => x.UserId == userId)
                .Select(x => x.Id)
                .ToListAsync(ct);

            var analysesThisWeek = await _financeDbContext.Set<Recommendation>()
                .AsNoTracking()
                .CountAsync(x => userAssetIds.Contains(x.UserAssetId) && x.RecommendedAtUtc >= analysesWeekStart, ct);

            var totalValue = watchlist.Sum(x => x.OutstandingAmount);
            var totalInvested = watchlist.Sum(x => x.InvestedAmount);

            var recommendationCount = await _financeDbContext.Set<Recommendation>()
                .AsNoTracking()
                .CountAsync(x => userAssetIds.Contains(x.UserAssetId), ct);

            var recommendationWinRate = recommendationCount == 0 || watchlist.Count == 0
                ? 0m
                : Math.Round((decimal)watchlist.Count(x => x.DayVariationPct > 0m) / watchlist.Count, 4);

            var dayProfitLoss = watchlist.Sum(x => x.OutstandingAmount * (x.DayVariationPct / 100m));

            return new ClientDashboardViewModel
            {
                TotalPortfolioValue = decimal.Round(totalValue, 2),
                DayProfitLoss = decimal.Round(dayProfitLoss, 2),
                OpenPositions = watchlist.Count(x => x.HeldQuantity > 0m),
                AnalysesThisWeek = analysesThisWeek,
                WatchlistCount = watchlist.Count,
                RecommendationWinRate = recommendationWinRate,
                NextMarketOpenAt = ComputeNextMarketOpenUtc(),
                TotalInvested = decimal.Round(totalInvested, 2),
                TotalOutstanding = decimal.Round(totalValue, 2)
            };
        }

        public async Task<List<AssetSearchItemViewModel>> SearchAssetsAsync(string query, CancellationToken ct = default)
        {
            var normalizedQuery = (query ?? string.Empty).Trim().ToUpperInvariant();
            if (normalizedQuery.Length < 1)
            {
                return [];
            }

            var allSymbols = await _tickerService.GetAllSymbolsAsync();
            var matches = allSymbols
                .Where(symbol => symbol.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                .Take(20)
                .ToList();

            var response = new List<AssetSearchItemViewModel>(matches.Count);
            foreach (var symbol in matches)
            {
                var quote = await BuildQuoteAsync(symbol);
                response.Add(new AssetSearchItemViewModel
                {
                    Symbol = symbol,
                    CompanyName = $"{symbol} Inc.",
                    Market = GuessMarket(symbol),
                    Currency = "USD",
                    LastPrice = quote.LastPrice,
                    DayVariationPct = quote.DayVariationPct
                });
            }

            return response;
        }

        public async Task<List<WatchlistItemViewModel>> GetWatchlistAsync(string userId, CancellationToken ct = default)
        {
            var userAssets = await _financeDbContext.UserAssets
                .AsNoTracking()
                .Include(x => x.Asset)
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.Asset.Symbol)
                .ToListAsync(ct);

            var userAssetIds = userAssets.Select(x => x.Id).ToList();
            var transactions = await _financeDbContext.AssetTransactions
                .AsNoTracking()
                .Where(x => userAssetIds.Contains(x.UserAssetId))
                .ToListAsync(ct);

            var groupedTransactions = transactions
                .GroupBy(x => x.UserAssetId)
                .ToDictionary(x => x.Key, x => x.ToList());

            var response = new List<WatchlistItemViewModel>(userAssets.Count);

            foreach (var userAsset in userAssets)
            {
                var quote = await BuildQuoteAsync(userAsset.Asset.Symbol);
                groupedTransactions.TryGetValue(userAsset.Id, out var history);
                history ??= [];

                var buyTransactions = history.Where(x => x.TransactionType == TransactionTypeEnum.Buy).ToList();
                var sellTransactions = history.Where(x => x.TransactionType == TransactionTypeEnum.Sell).ToList();

                var boughtQuantity = buyTransactions.Sum(x => x.Quantity);
                var soldQuantity = sellTransactions.Sum(x => x.Quantity);
                var totalBuyAmount = buyTransactions.Sum(x => (x.Quantity * x.UnitPrice) + x.Fees);
                var totalSellAmount = sellTransactions.Sum(x => (x.Quantity * x.UnitPrice) - x.Fees);

                var averageBuyPrice = boughtQuantity > 0m ? totalBuyAmount / boughtQuantity : 0m;
                var investedAmount = totalBuyAmount - totalSellAmount;
                var outstandingAmount = userAsset.Quantity * quote.LastPrice;

                response.Add(new WatchlistItemViewModel
                {
                    UserAssetId = userAsset.Id,
                    Symbol = userAsset.Asset.Symbol,
                    CompanyName = userAsset.Asset.Name ?? userAsset.Asset.Symbol,
                    Market = GuessMarket(userAsset.Asset.Symbol),
                    LastPrice = quote.LastPrice,
                    DayVariationPct = quote.DayVariationPct,
                    HeldQuantity = userAsset.Quantity,
                    AverageBuyPrice = decimal.Round(averageBuyPrice, 4),
                    InvestedAmount = decimal.Round(investedAmount, 2),
                    OutstandingAmount = decimal.Round(outstandingAmount, 2)
                });
            }

            return response;
        }

        public async Task<WatchlistItemViewModel> AddToWatchlistAsync(string userId, WatchlistUpsertRequestViewModel request, CancellationToken ct = default)
        {
            var symbol = NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request.Symbol));
            }

            var asset = await EnsureAssetAsync(symbol, request.CompanyName, ct);
            var userAsset = await _financeDbContext.UserAssets
                .FirstOrDefaultAsync(x => x.UserId == userId && x.AssetId == asset.Id, ct);

            if (userAsset == null)
            {
                userAsset = new UserAsset
                {
                    UserId = userId,
                    AssetId = asset.Id,
                    Quantity = 0m
                };

                await _financeDbContext.UserAssets.AddAsync(userAsset, ct);
                await _financeDbContext.SaveChangesAsync(ct);
            }

            var quote = await BuildQuoteAsync(symbol);

            return new WatchlistItemViewModel
            {
                UserAssetId = userAsset.Id,
                Symbol = asset.Symbol,
                CompanyName = asset.Name ?? asset.Symbol,
                Market = GuessMarket(asset.Symbol),
                LastPrice = quote.LastPrice,
                DayVariationPct = quote.DayVariationPct,
                HeldQuantity = userAsset.Quantity,
                AverageBuyPrice = 0m,
                InvestedAmount = 0m,
                OutstandingAmount = decimal.Round(userAsset.Quantity * quote.LastPrice, 2)
            };
        }

        public async Task<LiveQuoteViewModel> GetLiveQuoteAsync(string symbol, CancellationToken ct = default)
        {
            var normalizedSymbol = NormalizeSymbol(symbol);
            if (string.IsNullOrWhiteSpace(normalizedSymbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(symbol));
            }

            var quote = await BuildQuoteAsync(normalizedSymbol);
            var asset = await EnsureAssetAsync(normalizedSymbol, normalizedSymbol, ct);
            await SavePriceHistoryAsync(asset.Id, quote.LastPrice, ct);

            return new LiveQuoteViewModel
            {
                Symbol = normalizedSymbol,
                LastPrice = quote.LastPrice,
                DayVariationPct = quote.DayVariationPct,
                AsOfUtc = DateTime.UtcNow
            };
        }

        public async Task RemoveFromWatchlistAsync(string userId, string symbol, CancellationToken ct = default)
        {
            var normalizedSymbol = NormalizeSymbol(symbol);
            if (string.IsNullOrWhiteSpace(normalizedSymbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(symbol));
            }

            var userAsset = await _financeDbContext.UserAssets
                .Include(x => x.Asset)
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Asset.Symbol == normalizedSymbol, ct);

            if (userAsset == null)
            {
                return;
            }

            if (userAsset.Quantity > 0m)
            {
                throw new InvalidOperationException("Impossible de supprimer une valeur avec une position ouverte.");
            }

            _financeDbContext.UserAssets.Remove(userAsset);
            await _financeDbContext.SaveChangesAsync(ct);
        }

        public async Task<TransactionItemViewModel> RegisterTransactionAsync(string userId, TransactionCreateRequestViewModel request, CancellationToken ct = default)
        {
            if (request.Quantity <= 0m)
            {
                throw new ArgumentException("La quantite doit etre strictement positive.", nameof(request.Quantity));
            }

            if (request.UnitPrice <= 0m)
            {
                throw new ArgumentException("Le prix unitaire doit etre strictement positif.", nameof(request.UnitPrice));
            }

            if (request.Fees < 0m)
            {
                throw new ArgumentException("Les frais ne peuvent pas etre negatifs.", nameof(request.Fees));
            }

            var symbol = NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request.Symbol));
            }

            var transactionType = ParseTransactionType(request.TransactionType);
            var asset = await EnsureAssetAsync(symbol, symbol, ct);

            var userAsset = await _financeDbContext.UserAssets
                .FirstOrDefaultAsync(x => x.UserId == userId && x.AssetId == asset.Id, ct);

            if (userAsset == null)
            {
                userAsset = new UserAsset
                {
                    UserId = userId,
                    AssetId = asset.Id,
                    Quantity = 0m
                };

                await _financeDbContext.UserAssets.AddAsync(userAsset, ct);
            }

            if (transactionType == TransactionTypeEnum.Sell && userAsset.Quantity < request.Quantity)
            {
                throw new InvalidOperationException("Quantite insuffisante pour vendre.");
            }

            userAsset.Quantity = transactionType == TransactionTypeEnum.Buy
                ? userAsset.Quantity + request.Quantity
                : userAsset.Quantity - request.Quantity;

            var transaction = new AssetTransaction
            {
                UserAssetId = userAsset.Id,
                TimestampUtc = request.TimestampUtc?.ToUniversalTime() ?? DateTime.UtcNow,
                TransactionType = transactionType,
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice,
                Fees = request.Fees
            };

            await _financeDbContext.AssetTransactions.AddAsync(transaction, ct);
            await _financeDbContext.SaveChangesAsync(ct);

            var gross = request.Quantity * request.UnitPrice;
            var net = transactionType == TransactionTypeEnum.Buy ? gross + request.Fees : gross - request.Fees;

            return new TransactionItemViewModel
            {
                Id = transaction.Id,
                Symbol = asset.Symbol,
                CompanyName = asset.Name ?? asset.Symbol,
                TransactionType = transactionType.ToString(),
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice,
                Fees = request.Fees,
                GrossAmount = decimal.Round(gross, 2),
                NetAmount = decimal.Round(net, 2),
                TimestampUtc = transaction.TimestampUtc
            };
        }

        public async Task<List<TransactionItemViewModel>> GetTransactionsAsync(string userId, int take, CancellationToken ct = default)
        {
            var size = Math.Clamp(take, 1, 200);

            var transactions = await _financeDbContext.AssetTransactions
                .AsNoTracking()
                .Include(x => x.UserAsset)
                .ThenInclude(x => x.Asset)
                .Where(x => x.UserAsset.UserId == userId)
                .OrderByDescending(x => x.TimestampUtc)
                .Take(size)
                .ToListAsync(ct);

            return transactions
                .Select(transaction =>
                {
                    var gross = transaction.Quantity * transaction.UnitPrice;
                    var net = transaction.TransactionType == TransactionTypeEnum.Buy
                        ? gross + transaction.Fees
                        : gross - transaction.Fees;

                    return new TransactionItemViewModel
                    {
                        Id = transaction.Id,
                        Symbol = transaction.UserAsset.Asset.Symbol,
                        CompanyName = transaction.UserAsset.Asset.Name ?? transaction.UserAsset.Asset.Symbol,
                        TransactionType = transaction.TransactionType.ToString(),
                        Quantity = transaction.Quantity,
                        UnitPrice = transaction.UnitPrice,
                        Fees = transaction.Fees,
                        GrossAmount = decimal.Round(gross, 2),
                        NetAmount = decimal.Round(net, 2),
                        TimestampUtc = transaction.TimestampUtc
                    };
                })
                .ToList();
        }

        public async Task DeleteTransactionAsync(string userId, string transactionId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                throw new ArgumentException("L'identifiant de transaction est obligatoire.", nameof(transactionId));
            }

            var transaction = await _financeDbContext.AssetTransactions
                .Include(x => x.UserAsset)
                .ThenInclude(x => x.Asset)
                .FirstOrDefaultAsync(x => x.Id == transactionId && x.UserAsset.UserId == userId, ct);

            if (transaction == null)
            {
                return;
            }

            var userAsset = transaction.UserAsset;

            if (transaction.TransactionType == TransactionTypeEnum.Buy)
            {
                if (userAsset.Quantity < transaction.Quantity)
                {
                    throw new InvalidOperationException("Suppression impossible: quantite actuelle insuffisante.");
                }

                userAsset.Quantity -= transaction.Quantity;
            }
            else
            {
                userAsset.Quantity += transaction.Quantity;
            }

            _financeDbContext.AssetTransactions.Remove(transaction);
            await _financeDbContext.SaveChangesAsync(ct);
        }

        public async Task<AnalysisResultViewModel> RunAnalysisAsync(string userId, AnalysisRunRequestViewModel request, CancellationToken ct = default)
        {
            var symbol = NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request.Symbol));
            }

            var prediction = await _pythonApiService.PredictAsync(new AssetIn { Symbol = symbol });
            var asset = await EnsureAssetAsync(symbol, symbol, ct);
            var userAsset = await EnsureUserAssetAsync(userId, asset.Id, ct);

            var action = ParseRecommendationAction(prediction.SuggestedAction);
            var reason = BuildAnalysisReason(prediction.Pattern, prediction.ActionReason);

            var recommendation = new Recommendation
            {
                UserAssetId = userAsset.Id,
                Action = action,
                Confidence = prediction.ActionConfidence,
                RecommendedAtUtc = prediction.PredictedAt,
                Reason = reason
            };

            await _financeDbContext.Set<Recommendation>().AddAsync(recommendation, ct);
            await _financeDbContext.SaveChangesAsync(ct);

            return MapAnalysisResult(recommendation.Id, asset, prediction, reason);
        }

        public async Task<List<AnalysisResultViewModel>> GetRecentAnalysesAsync(string userId, int take, CancellationToken ct = default)
        {
            var size = Math.Clamp(take, 1, 100);

            var recommendations = await _financeDbContext.Set<Recommendation>()
                .AsNoTracking()
                .Include(x => x.UserAsset)
                .ThenInclude(x => x.Asset)
                .Where(x => x.UserAsset.UserId == userId)
                .OrderByDescending(x => x.RecommendedAtUtc)
                .Take(size)
                .ToListAsync(ct);

            return recommendations
                .Select(rec =>
                {
                    var (pattern, reason) = ParseAnalysisReason(rec.Reason);

                    return new AnalysisResultViewModel
                    {
                        Id = rec.Id,
                        Symbol = rec.UserAsset.Asset.Symbol,
                        CompanyName = rec.UserAsset.Asset.Name ?? rec.UserAsset.Asset.Symbol,
                        Pattern = pattern,
                        Confidence = rec.Confidence,
                        Recommendation = rec.Action.ToString(),
                        Reason = reason,
                        RiskLevel = InferRiskLevel(rec.Confidence),
                        HorizonDays = 5,
                        PredictedAt = rec.RecommendedAtUtc
                    };
                })
                .ToList();
        }

        public async Task<SimulationResultViewModel> RunSimulationAsync(string userId, SimulationRequestViewModel request, CancellationToken ct = default)
        {
            if (request.InvestmentAmount <= 0m)
            {
                throw new ArgumentException("Le montant d investissement doit etre strictement positif.", nameof(request.InvestmentAmount));
            }

            var symbol = NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request.Symbol));
            }

            var horizonDays = Math.Clamp(request.HorizonDays, 1, 365);
            var prediction = await _pythonApiService.PredictAsync(new AssetIn { Symbol = symbol });

            var trendFactor = prediction.SuggestedAction.ToLowerInvariant() switch
            {
                "buy" => 0.12m,
                "sell" => -0.08m,
                _ => 0.02m
            };

            var horizonFactor = horizonDays / 30m;
            var confidenceFactor = Math.Clamp(prediction.ActionConfidence, 0m, 1m);
            var estimatedReturnPct = trendFactor * confidenceFactor * horizonFactor;
            var estimatedReturnAmount = request.InvestmentAmount * estimatedReturnPct;

            return new SimulationResultViewModel
            {
                Symbol = symbol,
                InvestmentAmount = decimal.Round(request.InvestmentAmount, 2),
                HorizonDays = horizonDays,
                EstimatedReturnAmount = decimal.Round(estimatedReturnAmount, 2),
                EstimatedReturnPct = decimal.Round(estimatedReturnPct, 4),
                EstimatedFinalAmount = decimal.Round(request.InvestmentAmount + estimatedReturnAmount, 2),
                Recommendation = prediction.SuggestedAction,
                Assumption = "Simulation basee sur la confiance IA et un profil de marche simplifie."
            };
        }

        private async Task<(decimal LastPrice, decimal DayVariationPct)> BuildQuoteAsync(string symbol)
        {
            var candles = await _tickerService.GetTimeSeriesAsync(symbol, "1day", 2);
            if (candles.Candles.Count == 0)
            {
                return (0m, 0m);
            }

            var last = candles.Candles[^1].Close;
            if (candles.Candles.Count == 1 || candles.Candles[^2].Close == 0m)
            {
                return (decimal.Round(last, 4), 0m);
            }

            var previous = candles.Candles[^2].Close;
            var variation = ((last - previous) / previous) * 100m;
            return (decimal.Round(last, 4), decimal.Round(variation, 4));
        }

        private async Task SavePriceHistoryAsync(string assetId, decimal price, CancellationToken ct)
        {
            if (price <= 0m)
            {
                return;
            }

            var history = new PriceHistory
            {
                AssetId = assetId,
                RetrievedAtUtc = DateTime.UtcNow,
                Price = price,
                Volume = null
            };

            await _financeDbContext.PriceHistories.AddAsync(history, ct);
            await _financeDbContext.SaveChangesAsync(ct);
        }

        private async Task<Asset> EnsureAssetAsync(string symbol, string? companyName, CancellationToken ct)
        {
            var normalizedSymbol = NormalizeSymbol(symbol);

            var existing = await _financeDbContext.Assets
                .FirstOrDefaultAsync(x => x.Symbol == normalizedSymbol, ct);

            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(companyName) && string.IsNullOrWhiteSpace(existing.Name))
                {
                    existing.Name = companyName.Trim();
                    await _financeDbContext.SaveChangesAsync(ct);
                }

                return existing;
            }

            var asset = new Asset
            {
                Symbol = normalizedSymbol,
                Name = string.IsNullOrWhiteSpace(companyName) ? normalizedSymbol : companyName.Trim(),
                AssetType = AssetTypeEnum.Stock
            };

            await _financeDbContext.Assets.AddAsync(asset, ct);
            await _financeDbContext.SaveChangesAsync(ct);
            return asset;
        }

        private async Task<UserAsset> EnsureUserAssetAsync(string userId, string assetId, CancellationToken ct)
        {
            var userAsset = await _financeDbContext.UserAssets
                .FirstOrDefaultAsync(x => x.UserId == userId && x.AssetId == assetId, ct);

            if (userAsset != null)
            {
                return userAsset;
            }

            var created = new UserAsset
            {
                UserId = userId,
                AssetId = assetId,
                Quantity = 0m
            };

            await _financeDbContext.UserAssets.AddAsync(created, ct);
            await _financeDbContext.SaveChangesAsync(ct);
            return created;
        }

        private static string NormalizeSymbol(string? symbol)
        {
            return (symbol ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static TransactionTypeEnum ParseTransactionType(string? rawType)
        {
            var normalized = (rawType ?? string.Empty).Trim();
            if (Enum.TryParse<TransactionTypeEnum>(normalized, true, out var parsed))
            {
                return parsed;
            }

            throw new ArgumentException("Le type de transaction doit etre Buy ou Sell.", nameof(rawType));
        }

        private static RecommendationActionEnum ParseRecommendationAction(string? action)
        {
            var normalized = (action ?? string.Empty).Trim();
            if (Enum.TryParse<RecommendationActionEnum>(normalized, true, out var parsed))
            {
                return parsed;
            }

            return RecommendationActionEnum.Hold;
        }

        private static AnalysisResultViewModel MapAnalysisResult(
            string id,
            Asset asset,
            PredictOut prediction,
            string reason)
        {
            return new AnalysisResultViewModel
            {
                Id = id,
                Symbol = asset.Symbol,
                CompanyName = asset.Name ?? asset.Symbol,
                Pattern = prediction.Pattern,
                Confidence = prediction.ActionConfidence,
                Recommendation = prediction.SuggestedAction,
                Reason = reason,
                RiskLevel = InferRiskLevel(prediction.ActionConfidence),
                HorizonDays = 5,
                PredictedAt = prediction.PredictedAt
            };
        }

        private static string BuildAnalysisReason(string pattern, string? reason)
        {
            var safePattern = string.IsNullOrWhiteSpace(pattern) ? "UNKNOWN" : pattern.Trim();
            var safeReason = string.IsNullOrWhiteSpace(reason) ? "Aucune justification" : reason.Trim();
            return $"Pattern={safePattern};Reason={safeReason}";
        }

        private static (string Pattern, string Reason) ParseAnalysisReason(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ("UNKNOWN", "Aucune justification");
            }

            var chunks = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var pattern = "UNKNOWN";
            var reason = value;

            foreach (var chunk in chunks)
            {
                if (chunk.StartsWith("Pattern=", StringComparison.OrdinalIgnoreCase))
                {
                    pattern = chunk[8..].Trim();
                }
                else if (chunk.StartsWith("Reason=", StringComparison.OrdinalIgnoreCase))
                {
                    reason = chunk[7..].Trim();
                }
            }

            return (pattern, reason);
        }

        private static DateTime ComputeNextMarketOpenUtc()
        {
            var now = DateTime.UtcNow;
            var next = new DateTime(now.Year, now.Month, now.Day, 14, 30, 0, DateTimeKind.Utc);

            if (now >= next)
            {
                next = next.AddDays(1);
            }

            while (next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                next = next.AddDays(1);
            }

            return next;
        }

        private static string InferRiskLevel(decimal confidence)
        {
            if (confidence >= 0.75m)
            {
                return "Faible";
            }

            if (confidence >= 0.45m)
            {
                return "Modere";
            }

            return "Eleve";
        }

        private static string GuessMarket(string symbol)
        {
            return symbol switch
            {
                "AAPL" or "MSFT" or "NVDA" or "AMZN" or "GOOGL" or "META" => "NASDAQ",
                _ => "NYSE"
            };
        }
    }
}
