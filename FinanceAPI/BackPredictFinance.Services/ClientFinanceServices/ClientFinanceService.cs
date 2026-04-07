using BackPredictFinance.Contracts.MarketData;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface IClientFinanceService
    {
        Task<ClientDashboardViewModel> GetDashboardAsync(CancellationToken ct = default);
        Task<List<AssetSearchItemViewModel>> SearchAssetsAsync(string query, CancellationToken ct = default);
        Task<List<WatchlistItemViewModel>> GetWatchlistAsync(CancellationToken ct = default);
        Task<WatchlistItemViewModel> AddToWatchlistAsync(WatchlistUpsertRequestViewModel request, CancellationToken ct = default);
        Task RemoveFromWatchlistAsync(string symbol, CancellationToken ct = default);
        Task<LiveQuoteViewModel> GetLiveQuoteAsync(string symbol, CancellationToken ct = default);
        Task<TransactionItemViewModel> RegisterTransactionAsync(TransactionCreateRequestViewModel request, CancellationToken ct = default);
        Task<List<TransactionItemViewModel>> GetTransactionsAsync(int take, CancellationToken ct = default);
        Task DeleteTransactionAsync(string transactionId, CancellationToken ct = default);
        Task<AnalysisResultViewModel> RunAnalysisAsync(AnalysisRunRequestViewModel request, CancellationToken ct = default);
        Task<List<AnalysisResultViewModel>> GetRecentAnalysesAsync(int take, CancellationToken ct = default);
        Task<SimulationResultViewModel> RunSimulationAsync(SimulationRequestViewModel request, CancellationToken ct = default);
    }

    public class ClientFinanceService : BaseService, IClientFinanceService
    {
        private readonly ITickerService _tickerService;
        private readonly IAnalysisRequestCompatibilityResolver _analysisRequestCompatibilityResolver;
        private readonly IAnalysisLegacyCompatibilityService _analysisLegacyCompatibilityService;
        private readonly IAnalysisOrchestrator _analysisOrchestrator;

        public ClientFinanceService(
            IServiceProvider serviceProvider,
            ITickerService tickerService,
            IAnalysisRequestCompatibilityResolver analysisRequestCompatibilityResolver,
            IAnalysisLegacyCompatibilityService analysisLegacyCompatibilityService,
            IAnalysisOrchestrator analysisOrchestrator)
            : base(serviceProvider)
        {
            _tickerService = tickerService;
            _analysisRequestCompatibilityResolver = analysisRequestCompatibilityResolver;
            _analysisLegacyCompatibilityService = analysisLegacyCompatibilityService;
            _analysisOrchestrator = analysisOrchestrator;
        }

        public async Task<ClientDashboardViewModel> GetDashboardAsync(CancellationToken ct = default)
        {
            var watchlist = await GetWatchlistAsync(ct);
            var analysesWeekStart = DateTime.UtcNow.AddDays(-7);

            var userAssetIds = await _financeDbContext.UserAssets
                .Where(x => x.UserId == _currentUserId)
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

            var matches = await _tickerService.SearchAssetsAsync(normalizedQuery, ct);
            var response = new List<AssetSearchItemViewModel>(matches.Count);
            foreach (var asset in matches)
            {
                response.Add(new AssetSearchItemViewModel
                {
                    Symbol = asset.Symbol,
                    AssetType = MapAssetType(asset.AssetType),
                    CompanyName = asset.CompanyName,
                    Market = asset.Exchange,
                    Currency = asset.Currency,
                    LastPrice = asset.LastPrice,
                    DayVariationPct = asset.DayVariationPct
                });
            }

            return response;
        }

        public async Task<List<WatchlistItemViewModel>> GetWatchlistAsync(CancellationToken ct = default)
        {
            var userAssets = await _financeDbContext.UserAssets
                .AsNoTracking()
                .Include(x => x.Asset)
                .Where(x => x.UserId == _currentUserId)
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
                var quote = await BuildQuoteAsync(userAsset.Asset.Symbol, ct);
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
                    AssetType = MapAssetType(userAsset.Asset.AssetType),
                    CompanyName = userAsset.Asset.Name ?? userAsset.Asset.Symbol,
                    Market = userAsset.Asset.Exchange,
                    Currency = userAsset.Asset.Currency,
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

        public async Task<WatchlistItemViewModel> AddToWatchlistAsync(WatchlistUpsertRequestViewModel request, CancellationToken ct = default)
        {
            var symbol = NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request.Symbol));
            }

            var asset = await EnsureAssetAsync(symbol, request.CompanyName, ct);
            var userAsset = await _financeDbContext.UserAssets
                .FirstOrDefaultAsync(x => x.UserId == _currentUserId && x.AssetId == asset.Id, ct);

            if (userAsset == null)
            {
                userAsset = new UserAsset
                {
                    UserId = GetRequiredCurrentUserId(),
                    AssetId = asset.Id,
                    Quantity = 0m
                };

                await _financeDbContext.UserAssets.AddAsync(userAsset, ct);
                await _financeDbContext.SaveChangesAsync(ct);
            }

            var quote = await BuildQuoteAsync(symbol, ct);

            return new WatchlistItemViewModel
            {
                UserAssetId = userAsset.Id,
                Symbol = asset.Symbol,
                AssetType = MapAssetType(asset.AssetType),
                CompanyName = asset.Name ?? asset.Symbol,
                Market = asset.Exchange,
                Currency = asset.Currency,
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

            var asset = await EnsureAssetAsync(normalizedSymbol, null, ct);
            var quote = await BuildQuoteAsync(normalizedSymbol, ct);
            await SavePriceHistoryAsync(asset.Id, quote.LastPrice, ct);

            return new LiveQuoteViewModel
            {
                Symbol = normalizedSymbol,
                AssetType = MapAssetType(asset.AssetType),
                LastPrice = quote.LastPrice,
                DayVariationPct = quote.DayVariationPct,
                AsOfUtc = DateTime.UtcNow
            };
        }

        public async Task RemoveFromWatchlistAsync(string symbol, CancellationToken ct = default)
        {
            var normalizedSymbol = NormalizeSymbol(symbol);
            if (string.IsNullOrWhiteSpace(normalizedSymbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(symbol));
            }

            var userAsset = await _financeDbContext.UserAssets
                .Include(x => x.Asset)
                .FirstOrDefaultAsync(x => x.UserId == _currentUserId && x.Asset.Symbol == normalizedSymbol, ct);

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

        public async Task<TransactionItemViewModel> RegisterTransactionAsync(TransactionCreateRequestViewModel request, CancellationToken ct = default)
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
                .FirstOrDefaultAsync(x => x.UserId == _currentUserId && x.AssetId == asset.Id, ct);

            if (userAsset == null)
            {
                userAsset = new UserAsset
                {
                    UserId = GetRequiredCurrentUserId(),
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

        public async Task<List<TransactionItemViewModel>> GetTransactionsAsync(int take, CancellationToken ct = default)
        {
            var size = Math.Clamp(take, 1, 200);

            var transactions = await _financeDbContext.AssetTransactions
                .AsNoTracking()
                .Include(x => x.UserAsset)
                .ThenInclude(x => x.Asset)
                .Where(x => x.UserAsset.UserId == _currentUserId)
                .OrderByDescending(x => x.TimestampUtc)
                .Take(size)
                .ToListAsync(ct);

            return _mapper.Map<List<TransactionItemViewModel>>(transactions);
        }

        public async Task DeleteTransactionAsync(string transactionId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                throw new ArgumentException("L'identifiant de transaction est obligatoire.", nameof(transactionId));
            }

            var transaction = await _financeDbContext.AssetTransactions
                .Include(x => x.UserAsset)
                .ThenInclude(x => x.Asset)
                .FirstOrDefaultAsync(x => x.Id == transactionId && x.UserAsset.UserId == _currentUserId, ct);

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

        public async Task<AnalysisResultViewModel> RunAnalysisAsync(AnalysisRunRequestViewModel request, CancellationToken ct = default)
        {
            var normalizedRequest = new AnalysisRunRequestViewModel
            {
                Symbol = NormalizeSymbol(request.Symbol),
                RequestedPattern = request.RequestedPattern
            };

            if (string.IsNullOrWhiteSpace(normalizedRequest.Symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request.Symbol));
            }

            await EnsureAssetAsync(normalizedRequest.Symbol, null, ct);
            var resolvedRequest = await _analysisRequestCompatibilityResolver.ResolveAsync(normalizedRequest, GetRequiredCurrentUserId(), ct);
            var analysisResponse = await _analysisOrchestrator.RunAnalysisAsync(resolvedRequest, ct);
            return _analysisLegacyCompatibilityService.MapRunResult(analysisResponse);
        }

        public async Task<List<AnalysisResultViewModel>> GetRecentAnalysesAsync(int take, CancellationToken ct = default)
        {
            return await _analysisLegacyCompatibilityService.GetRecentAnalysesAsync(GetRequiredCurrentUserId(), take, ct);
        }

        public async Task<SimulationResultViewModel> RunSimulationAsync(SimulationRequestViewModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.InvestmentAmount <= 0m)
            {
                throw new ArgumentException("Le montant d investissement doit etre strictement positif.", nameof(request.InvestmentAmount));
            }

            var normalizedPattern = (request.Pattern ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalizedPattern))
            {
                normalizedPattern = "DOUBLE_TOP";
            }

            if (normalizedPattern != "DOUBLE_TOP")
            {
                throw new ArgumentException("Le pattern supporte est uniquement DOUBLE_TOP.", nameof(request.Pattern));
            }

            var symbol = NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request.Symbol));
            }

            await EnsureAssetAsync(symbol, null, ct);

            var analysisRequest = await _analysisRequestCompatibilityResolver.ResolveAsync(
                new AnalysisRunRequestViewModel
                {
                    Symbol = symbol,
                    RequestedPattern = normalizedPattern
                },
                GetRequiredCurrentUserId(),
                ct);

            var analysisResponse = await _analysisOrchestrator.RunAnalysisAsync(analysisRequest, ct);
            var mainPattern = analysisResponse.MainPattern;
            var targetPrice = mainPattern?.RiskHints.SuggestedTakeProfit;
            var invalidationPrice = mainPattern?.Invalidation.InvalidationLevel;
            var currentPrice = mainPattern?.Detection.CurrentPrice ?? 0m;
            var probability = mainPattern?.Scoring.ConfidenceScore ?? 0m;
            var horizonDays = Math.Clamp(request.HorizonDays, 1, 365);

            var recommendation = _serviceProvider
                .GetRequiredService<ITradingRecommendationService>()
                .EvaluateAnalysis(
                    MapPatternForSimulation(mainPattern?.PatternId),
                    mainPattern?.Detection.CurrentPhaseCode ?? string.Empty,
                    probability,
                    targetPrice,
                    invalidationPrice);

            var estimatedReturnPct = BuildSimulationReturnPct(currentPrice, targetPrice);
            var estimatedReturnAmount = decimal.Round(request.InvestmentAmount * estimatedReturnPct, 2);
            var estimatedFinalAmount = decimal.Round(request.InvestmentAmount + estimatedReturnAmount, 2);

            return new SimulationResultViewModel
            {
                Symbol = symbol,
                Pattern = MapPatternForSimulation(mainPattern?.PatternId),
                Phase = mainPattern?.Detection.CurrentPhaseCode ?? string.Empty,
                InvestmentAmount = decimal.Round(request.InvestmentAmount, 2),
                HorizonDays = horizonDays,
                EstimatedReturnAmount = estimatedReturnAmount,
                EstimatedReturnPct = decimal.Round(estimatedReturnPct, 4),
                EstimatedFinalAmount = estimatedFinalAmount,
                Assumption = BuildSimulationAssumption(symbol, currentPrice, targetPrice, horizonDays),
                CurrentPrice = currentPrice,
                Probability = probability,
                RecommendationAction = recommendation.Action,
                RecommendationReason = recommendation.Reason,
                RiskLevel = recommendation.RiskLevel,
                IsActionable = recommendation.IsActionable,
                TargetPrice = targetPrice,
                InvalidationPrice = invalidationPrice
            };
        }

        private static TradingPatternEnum MapPatternForSimulation(string? patternId)
        {
            return (patternId ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "HEAD_AND_SHOULDERS" => TradingPatternEnum.HeadAndShoulders,
                "DOUBLE_TOP" => TradingPatternEnum.DoubleTop,
                "DOUBLE_BOTTOM" => TradingPatternEnum.DoubleBottom,
                "CUP_AND_HANDLE" => TradingPatternEnum.CupAndHandle,
                "TRIANGLE" => TradingPatternEnum.Triangle,
                _ => TradingPatternEnum.DoubleTop
            };
        }

        private static decimal BuildSimulationReturnPct(decimal currentPrice, decimal? targetPrice)
        {
            if (currentPrice <= 0m || !targetPrice.HasValue)
            {
                return 0m;
            }

            return decimal.Round((targetPrice.Value - currentPrice) / currentPrice, 6);
        }

        private static string BuildSimulationAssumption(string symbol, decimal currentPrice, decimal? targetPrice, int horizonDays)
        {
            if (currentPrice <= 0m || !targetPrice.HasValue)
            {
                return $"Simulation pedagogique API-owned sur {symbol} sans objectif technique exploitable a ce stade.";
            }

            return $"Simulation pedagogique API-owned sur {symbol} en projetant un passage du prix courant {currentPrice:0.####} vers l'objectif technique {targetPrice.Value:0.####} sur un horizon indicatif de {horizonDays} jours.";
        }

        private async Task<(decimal LastPrice, decimal DayVariationPct)> BuildQuoteAsync(string symbol, CancellationToken ct)
        {
            var quote = await _tickerService.GetQuoteAsync(symbol, ct);
            return (quote.LastPrice, quote.DayVariationPct);
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
            var marketProfile = await EnsureFrenchEquityEligibilityAsync(normalizedSymbol, ct);

            var existing = await _financeDbContext.Assets
                .FirstOrDefaultAsync(x => x.Symbol == normalizedSymbol, ct);

            if (existing != null)
            {
                ApplyMarketProfile(existing, marketProfile, companyName);
                await _financeDbContext.SaveChangesAsync(ct);
                return existing;
            }

            var asset = new Asset
            {
                Symbol = normalizedSymbol,
                ProviderSymbol = marketProfile.ProviderSymbol,
                Name = ResolveCompanyName(marketProfile.CompanyName, companyName, normalizedSymbol),
                Exchange = marketProfile.Exchange,
                Currency = marketProfile.Currency,
                Country = marketProfile.Country,
                Sector = marketProfile.Sector,
                Category = marketProfile.Category,
                Summary = marketProfile.Summary,
                LastProfileSyncUtc = DateTime.UtcNow,
                AssetType = marketProfile.AssetType
            };

            await _financeDbContext.Assets.AddAsync(asset, ct);
            await _financeDbContext.SaveChangesAsync(ct);
            return asset;
        }

        private static string NormalizeSymbol(string? symbol)
        {
            return (symbol ?? string.Empty).Trim().ToUpperInvariant();
        }

        private string GetRequiredCurrentUserId()
        {
            if (!string.IsNullOrWhiteSpace(_currentUserId))
            {
                return _currentUserId;
            }

            throw new InvalidOperationException("Aucun utilisateur courant n'est disponible.");
        }

        private async Task<MarketAssetProfileData> EnsureFrenchEquityEligibilityAsync(string symbol, CancellationToken ct)
        {
            return await _tickerService.GetAssetProfileAsync(symbol, ct);
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

        private static DateTime ComputeNextMarketOpenUtc()
        {
            var parisTimeZone = ResolveParisTimeZone();
            var nowUtc = DateTime.UtcNow;
            var nowParis = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, parisTimeZone);
            var nextParisOpen = new DateTime(nowParis.Year, nowParis.Month, nowParis.Day, 9, 0, 0, DateTimeKind.Unspecified);

            if (nowParis >= nextParisOpen)
            {
                nextParisOpen = nextParisOpen.AddDays(1);
            }

            while (nextParisOpen.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                nextParisOpen = nextParisOpen.AddDays(1);
            }

            return TimeZoneInfo.ConvertTimeToUtc(nextParisOpen, parisTimeZone);
        }

        private static TimeZoneInfo ResolveParisTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
            }
        }

        private static string MapAssetType(AssetTypeEnum assetType)
            => assetType.ToString().ToUpperInvariant();

        private static void ApplyMarketProfile(Asset asset, MarketAssetProfileData marketProfile, string? fallbackCompanyName)
        {
            asset.ProviderSymbol = marketProfile.ProviderSymbol;
            asset.Name = ResolveCompanyName(marketProfile.CompanyName, fallbackCompanyName, asset.Symbol);
            asset.Exchange = marketProfile.Exchange;
            asset.Currency = marketProfile.Currency;
            asset.Country = marketProfile.Country;
            asset.Sector = marketProfile.Sector;
            asset.Category = marketProfile.Category;
            asset.Summary = marketProfile.Summary;
            asset.LastProfileSyncUtc = DateTime.UtcNow;
            asset.AssetType = marketProfile.AssetType;
        }

        private static string ResolveCompanyName(string? providerCompanyName, string? fallbackCompanyName, string symbol)
        {
            if (!string.IsNullOrWhiteSpace(providerCompanyName))
            {
                return providerCompanyName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(fallbackCompanyName))
            {
                return fallbackCompanyName.Trim();
            }

            return symbol;
        }
    }
}
