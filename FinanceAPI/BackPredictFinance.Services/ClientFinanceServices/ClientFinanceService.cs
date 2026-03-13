using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.PythonServices;
using BackPredictFinance.Services.PythonServices.Models;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Data;
using System.Text.Json;

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
        private static readonly HashSet<string> DefaultTrainingSymbols = new(StringComparer.OrdinalIgnoreCase)
        {
            "AAPL",
            "MSFT",
            "NVDA",
            "AMZN",
            "GOOGL",
            "META",
            "JPM",
            "XOM",
            "TSLA"
        };

        private readonly ITickerService _tickerService;
        private readonly IPythonApiService _pythonApiService;
        private readonly IPatternCatalogService _patternCatalogService;
        private readonly PythonCliOptions _pythonOptions;
        private readonly IHostEnvironment _environment;
        private bool? _analysisHistorySchemaAvailable;

        public ClientFinanceService(
            IServiceProvider serviceProvider,
            ITickerService tickerService,
            IPythonApiService pythonApiService,
            IPatternCatalogService patternCatalogService,
            IOptions<PythonCliOptions> pythonOptions,
            IHostEnvironment environment)
            : base(serviceProvider)
        {
            _tickerService = tickerService;
            _pythonApiService = pythonApiService;
            _patternCatalogService = patternCatalogService;
            _pythonOptions = pythonOptions.Value;
            _environment = environment;
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

            var trainingSymbols = GetTrainingSymbols();
            var matches = trainingSymbols
                .Where(symbol => symbol.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                .OrderBy(symbol => symbol, StringComparer.OrdinalIgnoreCase)
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

        public async Task<WatchlistItemViewModel> AddToWatchlistAsync(WatchlistUpsertRequestViewModel request, CancellationToken ct = default)
        {
            var symbol = NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request.Symbol));
            }
            EnsureSymbolIsAllowed(symbol);

            var asset = await EnsureAssetAsync(symbol, request.CompanyName, ct);
            var userAsset = await _financeDbContext.UserAssets
                .FirstOrDefaultAsync(x => x.UserId == _currentUserId && x.AssetId == asset.Id, ct);

            if (userAsset == null)
            {
                userAsset = new UserAsset
                {
                    UserId = _currentUserId,
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
            EnsureSymbolIsAllowed(normalizedSymbol);

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
            EnsureSymbolIsAllowed(symbol);

            var transactionType = ParseTransactionType(request.TransactionType);
            var asset = await EnsureAssetAsync(symbol, symbol, ct);

            var userAsset = await _financeDbContext.UserAssets
                .FirstOrDefaultAsync(x => x.UserId == _currentUserId && x.AssetId == asset.Id, ct);

            if (userAsset == null)
            {
                userAsset = new UserAsset
                {
                    UserId = _currentUserId,
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
            var startedAtUtc = DateTime.UtcNow;
            var symbol = NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request.Symbol));
            }
            EnsureSymbolIsAllowed(symbol);
            var requestedPattern = NormalizeRequestedAnalysisPattern(request.RequestedPattern);

            var prediction = await _pythonApiService.PredictAsync(new AssetIn
            {
                Symbol = symbol,
                Pattern = requestedPattern
            });
            var completedAtUtc = DateTime.UtcNow;
            var asset = await EnsureAssetAsync(symbol, symbol, ct);
            var userAsset = await EnsureUserAssetAsync(asset.Id, ct);
            var analysisRun = await TryPersistAnalysisRunAsync(asset, requestedPattern, prediction, startedAtUtc, completedAtUtc, ct);

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

            return MapAnalysisResult(analysisRun?.Id ?? recommendation.Id, asset, prediction, reason);
        }

        public async Task<List<AnalysisResultViewModel>> GetRecentAnalysesAsync(int take, CancellationToken ct = default)
        {
            var size = Math.Clamp(take, 1, 100);
            if (await CanUseAnalysisHistoryAsync(ct))
            {
                var analysisRuns = await _financeDbContext.AnalysisRuns
                    .AsNoTracking()
                    .Include(x => x.Asset)
                    .Include(x => x.PatternAssessments)
                    .Include(x => x.DecisionSignal)
                    .Include(x => x.ModelSnapshot)
                    .Where(x => x.UserId == _currentUserId)
                    .OrderByDescending(x => x.CompletedAtUtc ?? x.StartedAtUtc)
                    .Take(size)
                    .ToListAsync(ct);

                return _mapper.Map<List<AnalysisResultViewModel>>(analysisRuns);
            }

            var recommendations = await _financeDbContext.Set<Recommendation>()
                .AsNoTracking()
                .Include(x => x.UserAsset)
                .ThenInclude(x => x.Asset)
                .Where(x => x.UserAsset.UserId == _currentUserId)
                .OrderByDescending(x => x.RecommendedAtUtc)
                .Take(size)
                .ToListAsync(ct);

            return _mapper.Map<List<AnalysisResultViewModel>>(recommendations);
        }

        private async Task<AnalysisRun?> TryPersistAnalysisRunAsync(
            Asset asset,
            string requestedPattern,
            PredictOut prediction,
            DateTime startedAtUtc,
            DateTime completedAtUtc,
            CancellationToken ct)
        {
            if (!await CanUseAnalysisHistoryAsync(ct))
            {
                return null;
            }

            var analysisRun = new AnalysisRun
            {
                UserId = _currentUserId,
                AssetId = asset.Id,
                RequestedPattern = ParseTradingPattern(requestedPattern),
                Status = "Completed",
                StartedAtUtc = startedAtUtc,
                CompletedAtUtc = completedAtUtc,
                RawPayload = JsonSerializer.Serialize(prediction),
                PatternAssessments = BuildPatternAssessments(prediction),
                DecisionSignal = new DecisionSignal
                {
                    Action = ParseRecommendationAction(prediction.SuggestedAction),
                    IsActionable = prediction.IsActionable,
                    Confidence = prediction.ActionConfidence,
                    HorizonDays = prediction.HorizonDays,
                    Reason = string.IsNullOrWhiteSpace(prediction.ActionReason)
                        ? "Aucune justification"
                        : prediction.ActionReason.Trim()
                },
                ModelSnapshot = new ModelSnapshot
                {
                    ModelStatus = prediction.ModelStatus,
                    ModelMessage = string.IsNullOrWhiteSpace(prediction.ModelMessage)
                        ? string.Empty
                        : prediction.ModelMessage.Trim(),
                    ModelVersion = string.IsNullOrWhiteSpace(prediction.ModelVersion)
                        ? _patternCatalogService.Resolve(requestedPattern).ModelVersion
                        : prediction.ModelVersion.Trim(),
                    Precision = prediction.Precision,
                    F1 = prediction.F1,
                    RocAuc = prediction.RocAuc,
                    PositiveSamples = prediction.PositiveSamples,
                    SelectedThreshold = prediction.SelectedThreshold
                }
            };

            await _financeDbContext.AnalysisRuns.AddAsync(analysisRun, ct);
            await _financeDbContext.SaveChangesAsync(ct);
            return analysisRun;
        }

        private static List<PatternAssessment> BuildPatternAssessments(PredictOut prediction)
        {
            if (prediction.Patterns.Count > 0)
            {
                return prediction.Patterns
                    .Select(pattern => new PatternAssessment
                    {
                        Pattern = ParseTradingPattern(pattern.Pattern),
                        Phase = string.IsNullOrWhiteSpace(pattern.Phase) ? prediction.Phase : pattern.Phase.Trim(),
                        Probability = pattern.Probability,
                        Confidence = pattern.Confidence,
                        CurrentPrice = pattern.CurrentPrice,
                        NecklinePrice = pattern.NecklinePrice,
                        TargetPrice = pattern.TargetPrice,
                        InvalidationPrice = pattern.InvalidationPrice,
                        FirstPeakAtUtc = pattern.FirstPeakAtUtc,
                        SecondPeakAtUtc = pattern.SecondPeakAtUtc,
                        IsPrimary = pattern.IsPrimary
                    })
                    .ToList();
            }

            return
            [
                new PatternAssessment
                {
                    Pattern = ParseTradingPattern(prediction.Pattern),
                    Phase = prediction.Phase,
                    Probability = prediction.LastProbability,
                    Confidence = prediction.ActionConfidence,
                    CurrentPrice = prediction.CurrentPrice,
                    NecklinePrice = prediction.NecklinePrice,
                    TargetPrice = prediction.TargetPrice,
                    InvalidationPrice = prediction.InvalidationPrice,
                    IsPrimary = true
                }
            ];
        }

        private async Task<bool> CanUseAnalysisHistoryAsync(CancellationToken ct)
        {
            if (_analysisHistorySchemaAvailable.HasValue)
            {
                return _analysisHistorySchemaAvailable.Value;
            }

            const string sql = """
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_NAME IN ('AnalysisRuns', 'PatternAssessments', 'DecisionSignals', 'ModelSnapshots')
                """;

            var connection = _financeDbContext.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;
            if (shouldClose)
            {
                await connection.OpenAsync(ct);
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                var scalar = await command.ExecuteScalarAsync(ct);
                _analysisHistorySchemaAvailable = Convert.ToInt32(scalar) == 4;
                return _analysisHistorySchemaAvailable.Value;
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        public async Task<SimulationResultViewModel> RunSimulationAsync(SimulationRequestViewModel request, CancellationToken ct = default)
        {
            if (request.InvestmentAmount <= 0m)
            {
                throw new ArgumentException("Le montant d investissement doit etre strictement positif.", nameof(request.InvestmentAmount));
            }

            var normalizedPattern = (request.Pattern ?? string.Empty).Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(normalizedPattern) && normalizedPattern != "DOUBLE_TOP")
            {
                throw new ArgumentException("Le pattern supporte est uniquement DOUBLE_TOP.", nameof(request.Pattern));
            }

            var symbol = NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request.Symbol));
            }
            EnsureSymbolIsAllowed(symbol);

            var horizonDays = Math.Clamp(request.HorizonDays, 1, 365);
            var simulation = await _pythonApiService.SimulateAsync(
                new PythonSimulationRequest
                {
                    Symbol = symbol,
                    Pattern = normalizedPattern,
                    ModelDir = _patternCatalogService.Resolve(normalizedPattern).ModelDir,
                    Period = _pythonOptions.Period,
                    InvestmentAmount = request.InvestmentAmount,
                    HorizonDays = horizonDays,
                    SellThreshold = _pythonOptions.SellThreshold,
                    BuyThreshold = _pythonOptions.BuyThreshold
                });

            return new SimulationResultViewModel
            {
                Symbol = symbol,
                InvestmentAmount = decimal.Round(simulation.InvestmentAmount, 2),
                HorizonDays = simulation.HorizonDays,
                EstimatedReturnAmount = decimal.Round(simulation.EstimatedReturnAmount, 2),
                EstimatedReturnPct = decimal.Round(simulation.EstimatedReturnPct, 4),
                EstimatedFinalAmount = decimal.Round(simulation.EstimatedFinalAmount, 2),
                Recommendation = simulation.Recommendation,
                Assumption = simulation.Assumption
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

        private async Task<UserAsset> EnsureUserAssetAsync(string assetId, CancellationToken ct)
        {
            var userAsset = await _financeDbContext.UserAssets
                .FirstOrDefaultAsync(x => x.UserId == _currentUserId && x.AssetId == assetId, ct);

            if (userAsset != null)
            {
                return userAsset;
            }

            var created = new UserAsset
            {
                UserId = _currentUserId,
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

        private void EnsureSymbolIsAllowed(string symbol)
        {
            var trainingSymbols = GetTrainingSymbols();
            if (trainingSymbols.Contains(symbol))
            {
                return;
            }

            throw new InvalidOperationException($"Le symbole {symbol} n'est pas disponible pour ce modele.");
        }

        private HashSet<string> GetTrainingSymbols()
        {
            var trainConfigPath = ResolveTrainConfigPath();
            if (!File.Exists(trainConfigPath))
            {
                return new HashSet<string>(DefaultTrainingSymbols, StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(trainConfigPath));
                if (!document.RootElement.TryGetProperty("tickers", out var tickersElement) ||
                    tickersElement.ValueKind != JsonValueKind.Array)
                {
                    return new HashSet<string>(DefaultTrainingSymbols, StringComparer.OrdinalIgnoreCase);
                }

                var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var tickerElement in tickersElement.EnumerateArray())
                {
                    if (tickerElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var symbol = NormalizeSymbol(tickerElement.GetString());
                    if (string.IsNullOrWhiteSpace(symbol))
                    {
                        continue;
                    }

                    symbols.Add(symbol);
                }

                return symbols.Count > 0
                    ? symbols
                    : new HashSet<string>(DefaultTrainingSymbols, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new HashSet<string>(DefaultTrainingSymbols, StringComparer.OrdinalIgnoreCase);
            }
        }

        private string ResolveTrainConfigPath()
        {
            var workingDirectory = ResolvePath(_pythonOptions.WorkingDirectory);
            var modelDirectory = _patternCatalogService.Resolve().ModelDir;
            var resolvedModelDirectory = Path.IsPathRooted(modelDirectory)
                ? modelDirectory
                : Path.Combine(workingDirectory, modelDirectory);

            return Path.Combine(resolvedModelDirectory, "train_config.json");
        }

        private string ResolvePath(string pathValue)
        {
            if (Path.IsPathRooted(pathValue))
            {
                return pathValue;
            }

            return Path.GetFullPath(Path.Combine(_environment.ContentRootPath, pathValue));
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
                Phase = prediction.Phase,
                Confidence = prediction.ActionConfidence,
                Recommendation = prediction.SuggestedAction,
                Reason = reason,
                RiskLevel = InferRiskLevel(prediction.ActionConfidence, prediction.IsActionable),
                HorizonDays = prediction.HorizonDays,
                PredictedAt = prediction.PredictedAt,
                IsActionable = prediction.IsActionable,
                ModelStatus = prediction.ModelStatus.ToString(),
                ModelMessage = prediction.ModelMessage,
                CurrentPrice = prediction.CurrentPrice,
                TargetPrice = prediction.TargetPrice,
                InvalidationPrice = prediction.InvalidationPrice
            };
        }

        private static string BuildAnalysisReason(string pattern, string? reason)
        {
            var safePattern = string.IsNullOrWhiteSpace(pattern) ? "UNKNOWN" : pattern.Trim();
            var safeReason = string.IsNullOrWhiteSpace(reason) ? "Aucune justification" : reason.Trim();
            return $"Pattern={safePattern};Reason={safeReason}";
        }

        private static string NormalizeRequestedAnalysisPattern(string? requestedPattern)
        {
            var normalized = (requestedPattern ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return "DOUBLE_TOP";
            }

            if (normalized == "DOUBLE_TOP")
            {
                return normalized;
            }

            throw new ArgumentException("Le pattern supporte est uniquement DOUBLE_TOP.", nameof(requestedPattern));
        }

        private static TradingPatternEnum ParseTradingPattern(string? rawPattern)
        {
            var normalized = (rawPattern ?? string.Empty).Trim().ToUpperInvariant();
            return normalized switch
            {
                "HEAD_AND_SHOULDERS" => TradingPatternEnum.HeadAndShoulders,
                "DOUBLE_TOP" => TradingPatternEnum.DoubleTop,
                "DOUBLE_BOTTOM" => TradingPatternEnum.DoubleBottom,
                "CUP_AND_HANDLE" => TradingPatternEnum.CupAndHandle,
                "TRIANGLE" => TradingPatternEnum.Triangle,
                _ => TradingPatternEnum.DoubleTop
            };
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

        private static string InferRiskLevel(decimal confidence, bool actionable)
        {
            if (!actionable)
            {
                return "Information";
            }

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
