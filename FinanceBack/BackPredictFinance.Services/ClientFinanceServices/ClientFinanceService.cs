using System.Globalization;
using System.Text;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Patterns;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Assets;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface IClientFinanceService
    {
        Task<List<AssetSearchItemViewModel>> SearchAssetsAsync(string query, bool peaEligibleOnly = false, CancellationToken ct = default);
        Task<AnalysisDossierViewModel> RunAnalysisAsync(AnalysisRunRequestViewModel request, CancellationToken ct = default);
        Task<SimulationResultViewModel> RunSimulationAsync(SimulationRequestViewModel request, CancellationToken ct = default);
        Task<MultiSimulationResultViewModel> RunMultiSimulationAsync(SimulationRequestViewModel request, CancellationToken ct = default);
    }

    public sealed class ClientFinanceService : IClientFinanceService
    {
        private const int MaxSearchResults = 20;
        private const int SearchSqlCandidates = 60;

        private readonly FinanceDbContext _context;
        private readonly IAnalysisRequestCompatibilityResolver _analysisRequestCompatibilityResolver;
        private readonly IAnalysisOrchestrator _analysisOrchestrator;
        private readonly IAnalysisPatternRegistry _analysisPatternRegistry;
        private readonly IClientFinanceAssetSupportService _assetSupportService;
        private readonly IAnalysisResultProjectionService _analysisResultProjectionService;
        private readonly IMarketCatalogProvider _marketCatalogProvider;
        private readonly ILogger<ClientFinanceService> _logger;

        public ClientFinanceService(
            FinanceDbContext context,
            IAnalysisRequestCompatibilityResolver analysisRequestCompatibilityResolver,
            IAnalysisOrchestrator analysisOrchestrator,
            IAnalysisPatternRegistry analysisPatternRegistry,
            IClientFinanceAssetSupportService assetSupportService,
            IAnalysisResultProjectionService analysisResultProjectionService,
            IMarketCatalogProvider marketCatalogProvider,
            ILogger<ClientFinanceService> logger)
        {
            _context = context;
            _analysisRequestCompatibilityResolver = analysisRequestCompatibilityResolver;
            _analysisOrchestrator = analysisOrchestrator;
            _analysisPatternRegistry = analysisPatternRegistry;
            _assetSupportService = assetSupportService;
            _analysisResultProjectionService = analysisResultProjectionService;
            _marketCatalogProvider = marketCatalogProvider;
            _logger = logger;
        }

        public async Task<List<AssetSearchItemViewModel>> SearchAssetsAsync(string query, bool peaEligibleOnly = false, CancellationToken ct = default)
        {
            var rawQuery = (query ?? string.Empty).Trim();
            if (rawQuery.Length < 1)
            {
                return [];
            }

            var normalizedQuery = NormalizeForSearch(rawQuery);

            var assetsQuery = _context.Assets
                .AsNoTracking()
                .Include(a => a.PeaEligibilities)
                .Where(a => EF.Functions.Like(a.Symbol, $"%{rawQuery}%")
                         || EF.Functions.Like(a.Name ?? string.Empty, $"%{rawQuery}%")
                         || (a.Isin != null && EF.Functions.Like(a.Isin, $"%{rawQuery}%")));

            if (peaEligibleOnly)
            {
                assetsQuery = assetsQuery.Where(a =>
                    a.PeaEligibilities.Any(e => e.EligibilityStatus == PeaEligibilityStatusEnum.ConfirmedEligible));
            }

            var candidates = await assetsQuery
                .OrderBy(a => a.Symbol)
                .Take(SearchSqlCandidates)
                .ToListAsync(ct);

            var result = new List<AssetSearchItemViewModel>(MaxSearchResults);
            var localSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var asset in candidates)
            {
                if (result.Count >= MaxSearchResults)
                {
                    break;
                }

                if (!MatchesAccentInsensitive(asset.Symbol, normalizedQuery)
                    && !MatchesAccentInsensitive(asset.Name, normalizedQuery)
                    && !MatchesAccentInsensitive(asset.Isin, normalizedQuery))
                {
                    continue;
                }

                var isPeaEligible = asset.PeaEligibilities
                    .Any(e => e.EligibilityStatus == PeaEligibilityStatusEnum.ConfirmedEligible);

                result.Add(new AssetSearchItemViewModel
                {
                    Symbol = asset.Symbol,
                    AssetType = _assetSupportService.MapAssetType(asset.AssetType),
                    CompanyName = asset.Name ?? string.Empty,
                    Market = asset.Exchange,
                    Currency = asset.Currency,
                    LastPrice = 0m,
                    DayVariationPct = 0m,
                    Isin = asset.Isin,
                    Sector = string.IsNullOrWhiteSpace(asset.Sector) ? null : asset.Sector,
                    Country = string.IsNullOrWhiteSpace(asset.Country) ? null : asset.Country,
                    Summary = string.IsNullOrWhiteSpace(asset.Summary) ? null : asset.Summary,
                    IsPeaEligible = isPeaEligible
                });

                localSymbols.Add(asset.Symbol);
            }

            if (!peaEligibleOnly && result.Count < MaxSearchResults)
            {
                var yahooDescriptors = await FetchYahooSearchSafeAsync(rawQuery, ct);
                foreach (var descriptor in yahooDescriptors)
                {
                    if (result.Count >= MaxSearchResults)
                    {
                        break;
                    }

                    if (localSymbols.Contains(descriptor.Symbol))
                    {
                        continue;
                    }

                    result.Add(new AssetSearchItemViewModel
                    {
                        Symbol = descriptor.Symbol,
                        AssetType = _assetSupportService.MapAssetType(descriptor.AssetType),
                        CompanyName = descriptor.CompanyName,
                        Market = descriptor.Exchange,
                        Currency = descriptor.Currency,
                        LastPrice = 0m,
                        DayVariationPct = 0m,
                        Isin = null,
                        Sector = null,
                        Country = null,
                        Summary = null,
                        IsPeaEligible = false
                    });
                }
            }

            return result;
        }

        private async Task<IReadOnlyList<BackPredictFinance.Common.MarketData.MarketAssetDescriptor>> FetchYahooSearchSafeAsync(string query, CancellationToken ct)
        {
            try
            {
                return await _marketCatalogProvider.SearchAssetsAsync(query, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Yahoo symbol search failed for query '{Query}', falling back to local only", query);
                return [];
            }
        }

        private static string NormalizeForSearch(string input)
        {
            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().ToUpperInvariant();
        }

        private static bool MatchesAccentInsensitive(string? value, string normalizedQuery)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return NormalizeForSearch(value).Contains(normalizedQuery, StringComparison.Ordinal);
        }

        public async Task<AnalysisDossierViewModel> RunAnalysisAsync(AnalysisRunRequestViewModel request, CancellationToken ct = default)
        {
            var normalizedRequest = new AnalysisRunRequestViewModel
            {
                Symbol = _assetSupportService.NormalizeSymbol(request.Symbol),
                RequestedPatternIds = request.RequestedPatternIds
                    .Where(patternId => !string.IsNullOrWhiteSpace(patternId))
                    .Select(PatternIds.RequireActivePatternId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };

            if (string.IsNullOrWhiteSpace(normalizedRequest.Symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request));
            }

            await _assetSupportService.EnsureAssetAsync(normalizedRequest.Symbol, null, ct);
            var resolvedRequest = await _analysisRequestCompatibilityResolver.ResolveAsync(
                normalizedRequest,
                _assetSupportService.GetRequiredCurrentUserId(),
                ct);
            var analysisResponse = await _analysisOrchestrator.RunAnalysisAsync(resolvedRequest, ct);
            return _analysisResultProjectionService.MapDossier(analysisResponse);
        }

        public async Task<SimulationResultViewModel> RunSimulationAsync(SimulationRequestViewModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.InvestmentAmount <= 0m)
            {
                throw new ArgumentException("Le montant d'investissement doit être strictement positif.", nameof(request));
            }

            var normalizedPattern = PatternIds.Normalize(request.Pattern);
            if (string.IsNullOrWhiteSpace(normalizedPattern))
            {
                throw new ArgumentException("Le pattern est obligatoire.", nameof(request));
            }

            _analysisPatternRegistry.ResolveDefinition(normalizedPattern);

            var symbol = _assetSupportService.NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request));
            }

            await _assetSupportService.EnsureAssetAsync(symbol, null, ct);

            var dossier = await RunAnalysisAsync(
                new AnalysisRunRequestViewModel
                {
                    Symbol = symbol,
                    RequestedPatternIds = [PatternIds.RequireActivePatternId(normalizedPattern)]
                },
                ct);

            var mainPattern = dossier.MainPattern;

            if (mainPattern is null)
            {
                return BuildDegradedSimulation(symbol, dossier, request.InvestmentAmount);
            }

            return BuildPatternSimulation(symbol, dossier, request.InvestmentAmount, request.HorizonDays, mainPattern);
        }

        public async Task<MultiSimulationResultViewModel> RunMultiSimulationAsync(SimulationRequestViewModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.InvestmentAmount <= 0m)
            {
                throw new ArgumentException("Le montant d'investissement doit être strictement positif.", nameof(request));
            }

            var symbol = _assetSupportService.NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request));
            }

            var requestedPatterns = (request.Patterns ?? [])
                .Concat(string.IsNullOrWhiteSpace(request.Pattern) ? [] : [request.Pattern])
                .Select(PatternIds.Normalize)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (requestedPatterns.Count == 0)
            {
                throw new ArgumentException("Au moins un pattern est obligatoire.", nameof(request));
            }

            foreach (var patternId in requestedPatterns)
            {
                _analysisPatternRegistry.ResolveDefinition(patternId);
            }

            await _assetSupportService.EnsureAssetAsync(symbol, null, ct);

            var dossier = await RunAnalysisAsync(
                new AnalysisRunRequestViewModel
                {
                    Symbol = symbol,
                    RequestedPatternIds = []
                },
                ct);

            var patternPool = new List<AnalysisPatternViewModel>();
            if (dossier.MainPattern is not null)
            {
                patternPool.Add(dossier.MainPattern);
            }
            patternPool.AddRange(dossier.AlternativePatterns);

            var horizonDays = Math.Clamp(request.HorizonDays, 1, 365);
            var patternResults = new List<SimulationResultViewModel>(requestedPatterns.Count);

            foreach (var patternId in requestedPatterns)
            {
                var match = patternPool.FirstOrDefault(p =>
                    string.Equals(p.PatternId, patternId, StringComparison.OrdinalIgnoreCase));

                patternResults.Add(match is not null
                    ? BuildPatternSimulation(symbol, dossier, request.InvestmentAmount, horizonDays, match)
                    : BuildDegradedSimulationForPattern(symbol, dossier, request.InvestmentAmount, patternId));
            }

            // Le cours courant est une donnée de l'instrument, indépendante du fait qu'un pattern
            // demandé soit identifié : le sourcer du dossier (patterns analysés, sinon dernière bougie).
            var commonCurrentPrice = patternPool
                .Select(pattern => pattern.CurrentPrice)
                .FirstOrDefault(price => price > 0m);
            if (commonCurrentPrice == 0m && dossier.PriceSeries.Count > 0)
            {
                commonCurrentPrice = dossier.PriceSeries[^1].Close;
            }

            var currency = await ResolveAssetCurrencyAsync(symbol, ct);
            foreach (var patternResult in patternResults)
            {
                patternResult.Currency = currency;
            }

            var globalMessage = patternResults.All(r => !r.IsActionable)
                ? dossier.OutcomeMessage
                : string.Empty;

            return new MultiSimulationResultViewModel
            {
                Symbol = symbol,
                InvestmentAmount = decimal.Round(request.InvestmentAmount, 2),
                HorizonDays = horizonDays,
                CurrentPrice = commonCurrentPrice,
                Currency = currency,
                GlobalMessage = globalMessage,
                PatternResults = patternResults
            };
        }

        private async Task<string> ResolveAssetCurrencyAsync(string symbol, CancellationToken ct)
        {
            var currency = await _context.Assets
                .AsNoTracking()
                .Where(asset => asset.Symbol == symbol)
                .Select(asset => asset.Currency)
                .FirstOrDefaultAsync(ct);

            return string.IsNullOrWhiteSpace(currency) ? "EUR" : currency.Trim();
        }

        private SimulationResultViewModel BuildDegradedSimulation(
            string symbol,
            AnalysisDossierViewModel dossier,
            decimal investmentAmount)
        {
            return new SimulationResultViewModel
            {
                Symbol = symbol,
                Pattern = string.Empty,
                Phase = string.Empty,
                InvestmentAmount = decimal.Round(investmentAmount, 2),
                HorizonDays = 0,
                EstimatedReturnAmount = 0m,
                EstimatedReturnPct = 0m,
                EstimatedFinalAmount = decimal.Round(investmentAmount, 2),
                Assumption = $"Simulation non disponible : {dossier.OutcomeMessage}",
                CurrentPrice = 0m,
                Probability = 0m,
                RecommendationAction = RecommendationActionEnum.Hold,
                RecommendationReason = dossier.OutcomeMessage,
                RiskLevel = RiskLevelEnum.Information,
                IsActionable = false,
                TargetPrice = null,
                InvalidationPrice = null,
                Scenarios = [],
                PriceSeries = dossier.PriceSeries,
                StructuralPoints = []
            };
        }

        private static SimulationResultViewModel BuildDegradedSimulationForPattern(
            string symbol,
            AnalysisDossierViewModel dossier,
            decimal investmentAmount,
            string patternId)
        {
            return new SimulationResultViewModel
            {
                Symbol = symbol,
                Pattern = patternId,
                Phase = string.Empty,
                InvestmentAmount = decimal.Round(investmentAmount, 2),
                HorizonDays = 0,
                EstimatedReturnAmount = 0m,
                EstimatedReturnPct = 0m,
                EstimatedFinalAmount = decimal.Round(investmentAmount, 2),
                Assumption = "Pattern non identifié sur cette valeur.",
                CurrentPrice = 0m,
                Probability = 0m,
                RecommendationAction = RecommendationActionEnum.Hold,
                RecommendationReason = $"Le pattern {patternId} n'a pas été identifié sur {symbol}.",
                RiskLevel = RiskLevelEnum.Information,
                IsActionable = false,
                TargetPrice = null,
                InvalidationPrice = null,
                Scenarios = [],
                PriceSeries = dossier.PriceSeries,
                StructuralPoints = []
            };
        }

        private SimulationResultViewModel BuildPatternSimulation(
            string symbol,
            AnalysisDossierViewModel dossier,
            decimal investmentAmount,
            int horizonDays,
            AnalysisPatternViewModel pattern)
        {
            var currentPrice = pattern.CurrentPrice;
            var targetPrice = pattern.SuggestedTakeProfit;
            var invalidationPrice = pattern.InvalidationLevel;
            var clampedHorizon = Math.Clamp(horizonDays, 1, 365);

            var returnPct = BuildSimulationReturnPct(currentPrice, targetPrice);
            var returnAmount = decimal.Round(investmentAmount * returnPct, 2);
            var finalAmount = decimal.Round(investmentAmount + returnAmount, 2);

            return new SimulationResultViewModel
            {
                Symbol = symbol,
                Pattern = pattern.PatternId,
                Phase = pattern.PhaseCode,
                InvestmentAmount = decimal.Round(investmentAmount, 2),
                HorizonDays = clampedHorizon,
                EstimatedReturnAmount = returnAmount,
                EstimatedReturnPct = decimal.Round(returnPct, 4),
                EstimatedFinalAmount = finalAmount,
                Assumption = BuildSimulationAssumption(symbol, currentPrice, targetPrice, clampedHorizon),
                CurrentPrice = currentPrice,
                Probability = pattern.ConfidenceScore,
                RecommendationAction = Enum.TryParse<RecommendationActionEnum>(pattern.RecommendationAction, out var recAction)
                    ? recAction
                    : RecommendationActionEnum.Hold,
                RecommendationReason = pattern.RecommendationReason,
                RiskLevel = Enum.TryParse<RiskLevelEnum>(pattern.RiskLevel, out var riskLevel)
                    ? riskLevel
                    : RiskLevelEnum.Information,
                IsActionable = pattern.IsActionable,
                TargetPrice = targetPrice,
                InvalidationPrice = invalidationPrice,
                Scenarios = BuildScenarios(investmentAmount, currentPrice, targetPrice, invalidationPrice),
                PriceSeries = dossier.PriceSeries,
                StructuralPoints = pattern.StructuralPoints
            };
        }

        private List<SimulationScenarioViewModel> BuildScenarios(
            decimal investmentAmount,
            decimal currentPrice,
            decimal? targetPrice,
            decimal? invalidationPrice)
        {
            var scenarios = new List<SimulationScenarioViewModel>();

            if (targetPrice.HasValue && currentPrice > 0m)
            {
                var returnPct = BuildSimulationReturnPct(currentPrice, targetPrice);
                var returnAmount = decimal.Round(investmentAmount * returnPct, 2);
                scenarios.Add(new SimulationScenarioViewModel
                {
                    Label = "Cible",
                    TargetPrice = targetPrice,
                    EstimatedReturnPct = decimal.Round(returnPct, 4),
                    EstimatedReturnAmount = returnAmount,
                    EstimatedFinalAmount = decimal.Round(investmentAmount + returnAmount, 2),
                    Probability = null
                });
            }

            scenarios.Add(new SimulationScenarioViewModel
            {
                Label = "Neutre",
                TargetPrice = currentPrice > 0m ? currentPrice : null,
                EstimatedReturnPct = 0m,
                EstimatedReturnAmount = 0m,
                EstimatedFinalAmount = decimal.Round(investmentAmount, 2),
                Probability = null
            });

            if (invalidationPrice.HasValue && currentPrice > 0m)
            {
                var returnPct = BuildSimulationReturnPct(currentPrice, invalidationPrice);
                var returnAmount = decimal.Round(investmentAmount * returnPct, 2);
                scenarios.Add(new SimulationScenarioViewModel
                {
                    Label = "Invalidation",
                    TargetPrice = invalidationPrice,
                    EstimatedReturnPct = decimal.Round(returnPct, 4),
                    EstimatedReturnAmount = returnAmount,
                    EstimatedFinalAmount = decimal.Round(investmentAmount + returnAmount, 2),
                    Probability = null
                });
            }

            return scenarios;
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
                return $"Simulation pédagogique sur {symbol} sans objectif technique exploitable à ce stade.";
            }

            return $"Simulation pédagogique sur {symbol} en projetant un passage du prix courant {currentPrice:0.####} vers l'objectif technique {targetPrice.Value:0.####} sur un horizon indicatif de {horizonDays} jours.";
        }
    }
}
