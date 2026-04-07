using BackPredictFinance.Contracts.MarketData;
using BackPredictFinance.Contracts.Analysis;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
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

        public async Task<AnalysisRequest> ResolveAsync(AnalysisRunRequestViewModel request, string userId, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            var symbol = NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request.Symbol));
            }

            var resolvedPatterns = ResolvePatterns(request.RequestedPattern);
            var resolvedPatternIds = resolvedPatterns
                .Select(x => x.PatternId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var primaryPattern = resolvedPatterns[0];
            var asOfDate = await ResolveAsOfDateAsync(symbol, ct);
            var asset = await EnsureAssetAsync(symbol, ct);
            var instrument = MapInstrument(asset);
            var portfolioContext = await _portfolioContextLoader.TryLoadAsync(userId, asset.Id, asOfDate, ct)
                ?? new PortfolioContext
                {
                    UserId = userId,
                    InstrumentId = asset.Id,
                    CurrencyCode = asset.Currency
                };

            return new AnalysisRequest
            {
                InstrumentId = asset.Id,
                RequestedPatternIds = resolvedPatternIds.ToList(),
                AsOfDate = asOfDate,
                UserId = userId,
                Instrument = instrument,
                PortfolioContext = portfolioContext,
                CandleInterval = "1d",
                AnalysisMode = "on_demand",
                ResolvedPatternIds = resolvedPatternIds.ToList(),
                HistoryEndDate = asOfDate,
                HistoryStartDate = BuildHistoryStartDate(asOfDate, primaryPattern)
            };
        }

        private static string NormalizeSymbol(string? symbol)
        {
            return (symbol ?? string.Empty).Trim().ToUpperInvariant();
        }

        private IReadOnlyList<ResolvedAnalysisPattern> ResolvePatterns(string? requestedPattern)
        {
            var normalized = (requestedPattern ?? string.Empty).Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return [_patternRegistry.ResolveRequestedPattern(normalized)];
            }

            var enabledPatterns = _patternRegistry.GetEnabledPatterns()
                .GroupBy(x => x.PatternId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            if (enabledPatterns.Count > 1)
            {
                throw new InvalidOperationException("Le runtime V1 actif requiert un pattern explicite tant que plusieurs definitions de patterns sont actives.");
            }

            return enabledPatterns;
        }

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
                asset.Currency = marketProfile.Currency ?? "EUR";
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
            asset.AssetType = marketProfile.AssetType;
        }

        private static Instrument MapInstrument(Asset asset)
        {
            return new Instrument
            {
                InstrumentId = asset.Id,
                Symbol = asset.Symbol,
                ProviderSymbol = string.IsNullOrWhiteSpace(asset.ProviderSymbol) ? asset.Symbol : asset.ProviderSymbol,
                DisplayName = asset.Name ?? asset.Symbol,
                MarketCode = asset.Exchange,
                CountryCode = asset.Country ?? string.Empty,
                CurrencyCode = asset.Currency,
                AssetType = asset.AssetType == AssetTypeEnum.Stock ? "EQUITY" : asset.AssetType.ToString().ToUpperInvariant(),
                IsActive = true,
                LastProfileSyncUtc = asset.LastProfileSyncUtc,
                Summary = asset.Summary
            };
        }

        private static DateOnly BuildHistoryStartDate(DateOnly historyEndDate, ResolvedAnalysisPattern pattern)
        {
            ArgumentNullException.ThrowIfNull(pattern);

            if (pattern.HistoryLookbackMonths <= 0)
            {
                throw new InvalidOperationException($"Le runtime V1 actif ne fournit pas de profondeur historique valide pour le pattern {pattern.PatternId}.");
            }

            return historyEndDate.AddMonths(-pattern.HistoryLookbackMonths);
        }
    }
}
