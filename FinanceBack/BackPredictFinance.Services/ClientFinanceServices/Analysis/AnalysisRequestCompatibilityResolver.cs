using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Patterns;
using BackPredictFinance.Patterns.Contracts;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.TwelveDataServices;
using Microsoft.EntityFrameworkCore;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    /// <summary>
    /// Résout la requête frontend en contrat d'analyse backend compatible avec le runtime V1.
    /// </summary>
    public interface IAnalysisRequestCompatibilityResolver
    {
        /// <summary>
        /// Convertit une requête ClientFinance en contrat d'analyse enrichi et résolu.
        /// </summary>
        Task<AnalysisRequest> ResolveAsync(AnalysisRunRequestViewModel request, string userId, CancellationToken ct = default);
    }

    /// <summary>
    /// Implémente la normalisation et l'enrichissement des requêtes d'analyse.
    /// </summary>
    public sealed class AnalysisRequestCompatibilityResolver : IAnalysisRequestCompatibilityResolver
    {
        private readonly FinanceDbContext _financeDbContext;
        private readonly IAnalysisPatternRegistry _patternRegistry;
        private readonly IPortfolioContextLoader _portfolioContextLoader;
        private readonly ITickerService _tickerService;

        public AnalysisRequestCompatibilityResolver(
            FinanceDbContext financeDbContext,
            IAnalysisPatternRegistry patternRegistry,
            IPortfolioContextLoader portfolioContextLoader,
            ITickerService tickerService)
        {
            _financeDbContext = financeDbContext;
            _patternRegistry = patternRegistry;
            _portfolioContextLoader = portfolioContextLoader;
            _tickerService = tickerService;
        }

        /// <summary>
        /// Normalise une requete frontend en <see cref="AnalysisRequest"/> exploitable par le runtime :
        /// resout les patterns demandes vers des patterns actifs compatibles, fige la date d'analyse
        /// (AsOfDate/HistoryEndDate) sur le dernier cours connu du fournisseur, et calcule la fenetre
        /// d'historique necessaire a partir du besoin le plus large parmi les patterns resolus.
        /// </summary>
        public async Task<AnalysisRequest> ResolveAsync(AnalysisRunRequestViewModel request, string userId, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            var symbol = NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request));
            }

            var requestedPatternIds = BuildRequestedPatternIds(request);
            var resolvedPatterns = _patternRegistry.ResolveRequestedPatterns(requestedPatternIds);
            var resolvedPatternIds = resolvedPatterns
                .Select(pattern => pattern.PatternId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var asOfDate = await ResolveAsOfDateAsync(symbol, ct);
            var asset = await EnsureAssetAsync(symbol, ct);
            var instrument = MapInstrument(asset);
            var portfolioContext = await _portfolioContextLoader.TryLoadAsync(userId, asset.Id, ct)
                ?? new PortfolioContext
                {
                    UserId = userId,
                    InstrumentId = asset.Id,
                    CurrencyCode = asset.Currency
                };

            return new AnalysisRequest
            {
                InstrumentId = asset.Id,
                RequestedPatternIds = requestedPatternIds,
                AsOfDate = asOfDate,
                UserId = userId,
                Instrument = instrument,
                PortfolioContext = portfolioContext,
                CandleInterval = "1d",
                AnalysisMode = "on_demand",
                ResolvedPatternIds = resolvedPatternIds,
                HistoryEndDate = asOfDate,
                HistoryStartDate = BuildHistoryStartDate(asOfDate, resolvedPatterns)
            };
        }

        private static string NormalizeSymbol(string? symbol)
        {
            return (symbol ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static List<string> BuildRequestedPatternIds(AnalysisRunRequestViewModel request)
        {
            return request.RequestedPatternIds
                .Where(patternId => !string.IsNullOrWhiteSpace(patternId))
                .Select(PatternIds.RequireActivePatternId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // AsOfDate (qui devient HistoryEndDate) est fige sur la date du dernier cours connu du
        // fournisseur, pas sur DateTime.UtcNow : cela borne explicitement la fenetre d'analyse et
        // evite qu'un calcul en aval n'aille chercher des donnees plus recentes que ce cours.
        private async Task<DateOnly> ResolveAsOfDateAsync(string symbol, CancellationToken ct)
        {
            var quote = await _tickerService.GetQuoteAsync(symbol, ct);
            return DateOnly.FromDateTime(quote.AsOfUtc);
        }

        private async Task<Asset> EnsureAssetAsync(string symbol, CancellationToken ct)
        {
            var existing = await _financeDbContext.Assets
                .FirstOrDefaultAsync(x => x.Symbol == symbol, ct);

            MarketAssetProfileData? marketProfile = null;
            if (existing == null || string.IsNullOrWhiteSpace(existing.ProviderSymbol) || string.IsNullOrWhiteSpace(existing.Exchange) || string.IsNullOrWhiteSpace(existing.Country))
            {
                marketProfile = await _tickerService.GetAssetProfileAsync(symbol, ct);
            }

            if (existing != null)
            {
                ApplyMarketProfile(existing, marketProfile);
                await _financeDbContext.SaveChangesAsync(ct);
                return existing;
            }

            var asset = new Asset
            {
                Symbol = symbol,
                ProviderSymbol = marketProfile?.ProviderSymbol ?? symbol,
                Name = string.IsNullOrWhiteSpace(marketProfile?.CompanyName) ? symbol : marketProfile.CompanyName.Trim(),
                Exchange = marketProfile?.Exchange ?? string.Empty,
                Currency = marketProfile?.Currency ?? "EUR",
                Country = marketProfile?.Country,
                Sector = marketProfile?.Sector,
                Category = marketProfile?.Category,
                Summary = marketProfile?.Summary,
                LastProfileSyncUtc = marketProfile == null ? null : DateTime.UtcNow,
                AssetType = marketProfile?.AssetType ?? AssetTypeEnum.Stock
            };

            await _financeDbContext.Assets.AddAsync(asset, ct);
            await _financeDbContext.SaveChangesAsync(ct);
            return asset;
        }

        // Regle d'enrichissement "non destructive" : chaque champ n'est ecrase que s'il est vide en
        // base ; une donnee deja renseignee (potentiellement corrigee manuellement) n'est jamais
        // ecrasee par le profil fournisseur, sauf LastProfileSyncUtc qui trace toujours le dernier appel.
        private static void ApplyMarketProfile(Asset asset, MarketAssetProfileData? marketProfile)
        {
            if (marketProfile == null)
            {
                if (string.IsNullOrWhiteSpace(asset.ProviderSymbol))
                {
                    asset.ProviderSymbol = asset.Symbol;
                }

                if (string.IsNullOrWhiteSpace(asset.Currency))
                {
                    asset.Currency = "EUR";
                }

                return;
            }

            asset.ProviderSymbol = string.IsNullOrWhiteSpace(marketProfile.ProviderSymbol) ? asset.Symbol : marketProfile.ProviderSymbol.Trim();
            if (string.IsNullOrWhiteSpace(asset.Name))
            {
                asset.Name = string.IsNullOrWhiteSpace(marketProfile.CompanyName) ? asset.Symbol : marketProfile.CompanyName.Trim();
            }

            if (string.IsNullOrWhiteSpace(asset.Exchange))
            {
                asset.Exchange = marketProfile.Exchange ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(asset.Currency))
            {
                asset.Currency = string.IsNullOrWhiteSpace(marketProfile.Currency) ? "EUR" : marketProfile.Currency.Trim();
            }

            if (string.IsNullOrWhiteSpace(asset.Country))
            {
                asset.Country = marketProfile.Country;
            }

            if (string.IsNullOrWhiteSpace(asset.Sector))
            {
                asset.Sector = marketProfile.Sector;
            }

            if (string.IsNullOrWhiteSpace(asset.Category))
            {
                asset.Category = marketProfile.Category;
            }

            if (string.IsNullOrWhiteSpace(asset.Summary))
            {
                asset.Summary = marketProfile.Summary;
            }

            asset.LastProfileSyncUtc = DateTime.UtcNow;
        }

        private static Instrument MapInstrument(Asset asset)
        {
            return new Instrument
            {
                InstrumentId = asset.Id,
                Symbol = asset.Symbol,
                ProviderSymbol = string.IsNullOrWhiteSpace(asset.ProviderSymbol) ? asset.Symbol : asset.ProviderSymbol,
                DisplayName = string.IsNullOrWhiteSpace(asset.Name) ? asset.Symbol : asset.Name,
                MarketCode = asset.Exchange,
                CountryCode = asset.Country ?? string.Empty,
                CurrencyCode = string.IsNullOrWhiteSpace(asset.Currency) ? "EUR" : asset.Currency,
                AssetType = asset.AssetType == AssetTypeEnum.Stock ? "EQUITY" : asset.AssetType.ToString().ToUpperInvariant(),
                IsActive = true,
                LastProfileSyncUtc = asset.LastProfileSyncUtc,
                Summary = asset.Summary
            };
        }

        // La fenetre d'historique demandee au fournisseur doit couvrir le besoin le PLUS LARGE parmi
        // tous les patterns resolus (Max des lookbacks), sinon le pattern le plus gourmand en donnees
        // se retrouverait avec un historique tronque et un calcul potentiellement errone.
        private static DateOnly BuildHistoryStartDate(DateOnly historyEndDate, IReadOnlyList<ResolvedAnalysisPattern> resolvedPatterns)
        {
            ArgumentNullException.ThrowIfNull(resolvedPatterns);

            if (resolvedPatterns.Count == 0)
            {
                throw new InvalidOperationException("Au moins un pattern resolu est obligatoire pour calculer la fenetre d'analyse.");
            }

            var requiredLookbackMonths = resolvedPatterns
                .Select(pattern => pattern.HistoryLookbackMonths)
                .DefaultIfEmpty(0)
                .Max();

            if (requiredLookbackMonths <= 0)
            {
                throw new InvalidOperationException("Le runtime V1 actif ne fournit pas de profondeur historique valide pour les patterns resolves.");
            }

            return historyEndDate.AddMonths(-requiredLookbackMonths);
        }
    }
}
