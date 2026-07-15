using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.TwelveDataServices;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    /// <summary>
    /// Fournit les helpers communs de support actif pour les cas d'usage ClientFinance.
    /// </summary>
    public interface IClientFinanceAssetSupportService
    {
        /// <summary>
        /// Construit une cotation synthétique à partir du fournisseur de marché.
        /// </summary>
        Task<(decimal LastPrice, decimal DayVariationPct)> BuildQuoteAsync(string symbol, CancellationToken ct = default);
        /// <summary>
        /// Construit les cotations de plusieurs symboles en un seul appel groupé.
        /// Les symboles introuvables ou en erreur sont absents du dictionnaire retourné.
        /// </summary>
        Task<IReadOnlyDictionary<string, (decimal LastPrice, decimal DayVariationPct)>> BuildQuotesAsync(IEnumerable<string> symbols, CancellationToken ct = default);
        /// <summary>
        /// Retourne le taux de conversion d'une devise vers EUR (1 pour EUR, 1/EURUSD pour USD, etc.).
        /// Lève une exception si le taux est indisponible pour une devise non-EUR.
        /// </summary>
        Task<decimal> GetForexRateToEurAsync(string currency, CancellationToken ct = default);
        /// <summary>
        /// Persiste un point d'historique de prix pour un actif.
        /// </summary>
        Task SavePriceHistoryAsync(string assetId, decimal price, CancellationToken ct = default);
        /// <summary>
        /// Garantit l'existence persistée d'un actif compatible avec le périmètre marché.
        /// </summary>
        Task<Asset> EnsureAssetAsync(string symbol, string? companyName, CancellationToken ct = default);
        /// <summary>
        /// Normalise un symbole instrument pour le runtime backend.
        /// </summary>
        string NormalizeSymbol(string? symbol);
        /// <summary>
        /// Retourne l'identifiant de l'utilisateur courant ou lève une erreur si absent.
        /// </summary>
        string GetRequiredCurrentUserId();
        /// <summary>
        /// Projette le type d'actif backend vers sa représentation client.
        /// </summary>
        string MapAssetType(AssetTypeEnum assetType);
    }

    /// <summary>
    /// Implémente les aides communes de normalisation, éligibilité et persistance d'actifs.
    /// </summary>
    public sealed class ClientFinanceAssetSupportService : BaseService, IClientFinanceAssetSupportService
    {
        private readonly ITickerService _tickerService;
        private readonly IMarketPriceProvider _priceProvider;

        public ClientFinanceAssetSupportService(IServiceProvider serviceProvider, ITickerService tickerService, IMarketPriceProvider priceProvider)
            : base(serviceProvider)
        {
            _tickerService = tickerService;
            _priceProvider = priceProvider;
        }

        public async Task<(decimal LastPrice, decimal DayVariationPct)> BuildQuoteAsync(string symbol, CancellationToken ct = default)
        {
            var quote = await _tickerService.GetQuoteAsync(symbol, ct);
            return (quote.LastPrice, quote.DayVariationPct);
        }

        public async Task<IReadOnlyDictionary<string, (decimal LastPrice, decimal DayVariationPct)>> BuildQuotesAsync(IEnumerable<string> symbols, CancellationToken ct = default)
        {
            var quotes = await _priceProvider.GetQuotesAsync(symbols, ct);
            return quotes.ToDictionary(
                kvp => kvp.Key,
                kvp => (kvp.Value.LastPrice, kvp.Value.DayVariationPct),
                StringComparer.OrdinalIgnoreCase);
        }

        public async Task<decimal> GetForexRateToEurAsync(string currency, CancellationToken ct = default)
        {
            var normalized = (currency ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalized) || normalized == "EUR")
            {
                return 1m;
            }

            var quote = await _priceProvider.GetQuoteAsync($"EUR{normalized}=X", ct);
            if (quote.LastPrice <= 0m)
            {
                throw new InvalidOperationException($"Taux forex invalide pour la devise {normalized}: LastPrice={quote.LastPrice}");
            }

            return decimal.Round(1m / quote.LastPrice, 6);
        }

        public async Task SavePriceHistoryAsync(string assetId, decimal price, CancellationToken ct = default)
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

        public async Task<Asset> EnsureAssetAsync(string symbol, string? companyName, CancellationToken ct = default)
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

        public string NormalizeSymbol(string? symbol)
            => (symbol ?? string.Empty).Trim().ToUpperInvariant();

        public string GetRequiredCurrentUserId()
        {
            if (!string.IsNullOrWhiteSpace(_currentUserId))
            {
                return _currentUserId;
            }

            throw new InvalidOperationException("Aucun utilisateur courant n'est disponible.");
        }

        public string MapAssetType(AssetTypeEnum assetType)
            => assetType.ToString().ToUpperInvariant();

        private Task<MarketAssetProfileData> EnsureFrenchEquityEligibilityAsync(string symbol, CancellationToken ct)
            => _tickerService.GetAssetProfileAsync(symbol, ct);

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
