using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Patterns;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Assets;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Simulation;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface IClientFinanceService
    {
        Task<List<AssetSearchItemViewModel>> SearchAssetsAsync(string query, bool peaEligibleOnly = false, CancellationToken ct = default);
        Task<AnalysisResultViewModel> RunAnalysisAsync(AnalysisRunRequestViewModel request, CancellationToken ct = default);
        Task<SimulationResultViewModel> RunSimulationAsync(SimulationRequestViewModel request, CancellationToken ct = default);
    }

    public sealed class ClientFinanceService : IClientFinanceService
    {
        private const int MaxSearchResults = 20;

        private readonly FinanceDbContext _context;
        private readonly IAnalysisRequestCompatibilityResolver _analysisRequestCompatibilityResolver;
        private readonly IAnalysisOrchestrator _analysisOrchestrator;
        private readonly IAnalysisPatternRegistry _analysisPatternRegistry;
        private readonly IClientFinanceAssetSupportService _assetSupportService;
        private readonly IAnalysisResultProjectionService _analysisResultProjectionService;

        public ClientFinanceService(
            FinanceDbContext context,
            IAnalysisRequestCompatibilityResolver analysisRequestCompatibilityResolver,
            IAnalysisOrchestrator analysisOrchestrator,
            IAnalysisPatternRegistry analysisPatternRegistry,
            IClientFinanceAssetSupportService assetSupportService,
            IAnalysisResultProjectionService analysisResultProjectionService)
        {
            _context = context;
            _analysisRequestCompatibilityResolver = analysisRequestCompatibilityResolver;
            _analysisOrchestrator = analysisOrchestrator;
            _analysisPatternRegistry = analysisPatternRegistry;
            _assetSupportService = assetSupportService;
            _analysisResultProjectionService = analysisResultProjectionService;
        }

        public async Task<List<AssetSearchItemViewModel>> SearchAssetsAsync(string query, bool peaEligibleOnly = false, CancellationToken ct = default)
        {
            var normalizedQuery = (query ?? string.Empty).Trim();
            if (normalizedQuery.Length < 1)
            {
                return [];
            }

            var assetsQuery = _context.Assets
                .AsNoTracking()
                .Include(a => a.PeaEligibilities)
                .Where(a => EF.Functions.Like(a.Symbol, $"%{normalizedQuery}%")
                         || EF.Functions.Like(a.Name ?? string.Empty, $"%{normalizedQuery}%"));

            if (peaEligibleOnly)
            {
                assetsQuery = assetsQuery.Where(a =>
                    a.PeaEligibilities.Any(e => e.EligibilityStatus == PeaEligibilityStatusEnum.ConfirmedEligible));
            }

            var assets = await assetsQuery
                .OrderBy(a => a.Symbol)
                .Take(MaxSearchResults)
                .ToListAsync(ct);

            var result = new List<AssetSearchItemViewModel>(assets.Count);
            foreach (var asset in assets)
            {
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
                    IsPeaEligible = isPeaEligible
                });
            }

            return result;
        }

        public async Task<AnalysisResultViewModel> RunAnalysisAsync(AnalysisRunRequestViewModel request, CancellationToken ct = default)
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
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request.Symbol));
            }

            await _assetSupportService.EnsureAssetAsync(normalizedRequest.Symbol, null, ct);
            var resolvedRequest = await _analysisRequestCompatibilityResolver.ResolveAsync(
                normalizedRequest,
                _assetSupportService.GetRequiredCurrentUserId(),
                ct);
            var analysisResponse = await _analysisOrchestrator.RunAnalysisAsync(resolvedRequest, ct);
            return _analysisResultProjectionService.MapRunResult(analysisResponse);
        }

        public async Task<SimulationResultViewModel> RunSimulationAsync(SimulationRequestViewModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.InvestmentAmount <= 0m)
            {
                throw new ArgumentException("Le montant d investissement doit etre strictement positif.", nameof(request.InvestmentAmount));
            }

            var normalizedPattern = PatternIds.Normalize(request.Pattern);
            if (string.IsNullOrWhiteSpace(normalizedPattern))
            {
                throw new ArgumentException("Le pattern est obligatoire.", nameof(request.Pattern));
            }

            _analysisPatternRegistry.ResolveDefinition(normalizedPattern);

            var symbol = _assetSupportService.NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request.Symbol));
            }

            await _assetSupportService.EnsureAssetAsync(symbol, null, ct);

            var analysisResult = await RunAnalysisAsync(
                new AnalysisRunRequestViewModel
                {
                    Symbol = symbol,
                    RequestedPatternIds = [PatternIds.RequireActivePatternId(normalizedPattern)]
                },
                ct);

            var targetPrice = analysisResult.TargetPrice;
            var currentPrice = analysisResult.CurrentPrice;
            var horizonDays = Math.Clamp(request.HorizonDays, 1, 365);
            var estimatedReturnPct = BuildSimulationReturnPct(currentPrice, targetPrice);
            var estimatedReturnAmount = decimal.Round(request.InvestmentAmount * estimatedReturnPct, 2);
            var estimatedFinalAmount = decimal.Round(request.InvestmentAmount + estimatedReturnAmount, 2);

            return new SimulationResultViewModel
            {
                Symbol = symbol,
                Pattern = analysisResult.Pattern,
                Phase = analysisResult.Phase,
                InvestmentAmount = decimal.Round(request.InvestmentAmount, 2),
                HorizonDays = horizonDays,
                EstimatedReturnAmount = estimatedReturnAmount,
                EstimatedReturnPct = decimal.Round(estimatedReturnPct, 4),
                EstimatedFinalAmount = estimatedFinalAmount,
                Assumption = BuildSimulationAssumption(symbol, currentPrice, targetPrice, horizonDays),
                CurrentPrice = currentPrice,
                Probability = analysisResult.Probability,
                RecommendationAction = analysisResult.RecommendationAction,
                RecommendationReason = analysisResult.RecommendationReason,
                RiskLevel = analysisResult.RiskLevel,
                IsActionable = analysisResult.IsActionable,
                TargetPrice = targetPrice,
                InvalidationPrice = analysisResult.InvalidationPrice
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
    }
}
