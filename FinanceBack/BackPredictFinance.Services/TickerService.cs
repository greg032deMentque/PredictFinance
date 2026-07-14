using System.Text.Json;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Datas.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BackPredictFinance.Services.TwelveDataServices
{
    /// <summary>
    /// Orchestre l'accès applicatif aux données de marché externes.
    /// </summary>
    public interface ITickerService
    {
        /// <summary>
        /// Retourne la liste des places de marché disponibles.
        /// </summary>
        Task<IReadOnlyList<string>> GetExchangesAsync(CancellationToken ct = default);
        /// <summary>
        /// Retourne les symboles disponibles pour une place de marché donnée.
        /// </summary>
        Task<IReadOnlyList<string>> GetSymbolsByExchangeAsync(string exchange, CancellationToken ct = default);
        /// <summary>
        /// Retourne l'ensemble des symboles connus.
        /// </summary>
        Task<IReadOnlyList<string>> GetAllSymbolsAsync(CancellationToken ct = default);
        /// <summary>
        /// Retourne une série temporelle pour un symbole, un intervalle et une profondeur donnés.
        /// </summary>
        Task<TickerTimeSeriesResponse> GetTimeSeriesAsync(string symbol, string interval, int outputSize, CancellationToken ct = default);
        /// <summary>
        /// Recherche des instruments à partir d'un texte libre.
        /// </summary>
        Task<IReadOnlyList<MarketAssetDescriptor>> SearchAssetsAsync(string query, CancellationToken ct = default);
        /// <summary>
        /// Retourne la cotation courante d'un instrument.
        /// </summary>
        Task<MarketQuoteData> GetQuoteAsync(string symbol, CancellationToken ct = default);
        /// <summary>
        /// Retourne le profil de marché d'un instrument.
        /// </summary>
        Task<MarketAssetProfileData> GetAssetProfileAsync(string symbol, CancellationToken ct = default);
    }

    /// <summary>
    /// Expose une façade applicative pour les recherches et lectures de données de marché.
    /// </summary>
    public class TickerService : ITickerService
    {
        private readonly IMarketCatalogProvider _catalogProvider;
        private readonly IMarketPriceProvider _priceProvider;
        private readonly FinanceDbContext _context;
        private readonly ILogger<TickerService> _logger;

        public TickerService(
            IMarketCatalogProvider catalogProvider,
            IMarketPriceProvider priceProvider,
            FinanceDbContext context,
            ILogger<TickerService> logger)
        {
            _catalogProvider = catalogProvider;
            _priceProvider = priceProvider;
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<string>> GetExchangesAsync(CancellationToken ct = default)
        {
            var known = await _context.Assets
                .AsNoTracking()
                .Select(x => new
                {
                    x.Exchange,
                    x.AssetType
                })
                .ToListAsync(ct);

            return known
                .Where(x => x.AssetType == AssetTypeEnum.Stock || x.AssetType == AssetTypeEnum.Etf)
                .Select(x => NormalizeExchange(x.Exchange))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<IReadOnlyList<string>> GetSymbolsByExchangeAsync(string exchange, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(exchange))
            {
                return [];
            }

            var normalizedExchange = NormalizeExchange(exchange);
            var assets = await _context.Assets
                .AsNoTracking()
                .ToListAsync(ct);

            return assets
                .Where(x => x.AssetType == AssetTypeEnum.Stock || x.AssetType == AssetTypeEnum.Etf)
                .Where(x => string.Equals(NormalizeExchange(x.Exchange), normalizedExchange, StringComparison.OrdinalIgnoreCase))
                .Select(x => NormalizeSymbol(x.Symbol))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<IReadOnlyList<string>> GetAllSymbolsAsync(CancellationToken ct = default)
        {
            var assets = await _context.Assets
                .AsNoTracking()
                .ToListAsync(ct);

            return assets
                .Where(x => x.AssetType == AssetTypeEnum.Stock || x.AssetType == AssetTypeEnum.Etf)
                .Select(x => NormalizeSymbol(x.Symbol))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<TickerTimeSeriesResponse> GetTimeSeriesAsync(string symbol, string interval, int outputSize, CancellationToken ct = default)
        {
            var profile = await GetSupportedInstrumentProfileAsync(symbol, ct);
            var normalizedOutputSize = Math.Clamp(outputSize, 5, 500);
            var range = normalizedOutputSize switch
            {
                <= 30 => "1mo",
                <= 90 => "3mo",
                <= 180 => "6mo",
                _ => "1y"
            };

            var candles = await _priceProvider.GetChartAsync(profile.Symbol, interval, range, ct);
            var response = new TickerTimeSeriesResponse
            {
                Symbol = profile.Symbol,
                Interval = interval,
                OutputSize = normalizedOutputSize,
                Candles = candles.TakeLast(normalizedOutputSize).ToList()
            };
            return response;
        }

        public async Task<IReadOnlyList<MarketAssetDescriptor>> SearchAssetsAsync(string query, CancellationToken ct = default)
        {
            var descriptors = await _catalogProvider.SearchAssetsAsync(query, ct);
            if (descriptors.Count == 0)
            {
                return [];
            }

            var response = new List<MarketAssetDescriptor>();
            foreach (var descriptor in descriptors)
            {
                if (string.IsNullOrWhiteSpace(descriptor.Symbol))
                {
                    continue;
                }

                try
                {
                    var profile = await GetSupportedInstrumentProfileAsync(descriptor.Symbol, ct);
                    response.Add(new MarketAssetDescriptor
                    {
                        Symbol = profile.Symbol,
                        ProviderSymbol = profile.ProviderSymbol,
                        CompanyName = profile.CompanyName,
                        Exchange = profile.Exchange,
                        Currency = profile.Currency,
                        AssetType = profile.AssetType,
                        LastPrice = profile.LastPrice,
                        DayVariationPct = profile.DayVariationPct
                    });
                }
                catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or JsonException)
                {
                    _logger.LogWarning(ex, "TickerService.SearchAssetsAsync: symbole {Symbol} ignoré ({ExceptionType})", descriptor.Symbol, ex.GetType().Name);
                    continue;
                }
            }

            return response
                .GroupBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<MarketQuoteData> GetQuoteAsync(string symbol, CancellationToken ct = default)
        {
            var profile = await GetSupportedInstrumentProfileAsync(symbol, ct);
            return new MarketQuoteData
            {
                Symbol = profile.Symbol,
                AssetType = profile.AssetType,
                LastPrice = profile.LastPrice,
                DayVariationPct = profile.DayVariationPct,
                AsOfUtc = profile.AsOfUtc
            };
        }

        public Task<MarketAssetProfileData> GetAssetProfileAsync(string symbol, CancellationToken ct = default)
            => GetSupportedInstrumentProfileAsync(symbol, ct);

        private async Task<MarketAssetProfileData> GetSupportedInstrumentProfileAsync(string symbol, CancellationToken ct)
        {
            var normalizedSymbol = NormalizeSymbol(symbol);
            var profile = await _catalogProvider.GetAssetProfileAsync(normalizedSymbol, ct);
            return EnsureSupportedInstrumentEligibility(profile, normalizedSymbol);
        }

        private static MarketAssetProfileData EnsureSupportedInstrumentEligibility(MarketAssetProfileData profile, string symbol)
        {
            ArgumentNullException.ThrowIfNull(profile);

            var normalizedSymbol = NormalizeSymbol(string.IsNullOrWhiteSpace(profile.Symbol) ? symbol : profile.Symbol);
            var normalizedProviderSymbol = NormalizeSymbol(string.IsNullOrWhiteSpace(profile.ProviderSymbol) ? normalizedSymbol : profile.ProviderSymbol);
            var exchange = NormalizeExchange(profile.Exchange);
            var currency = (profile.Currency ?? string.Empty).Trim().ToUpperInvariant();
            var country = (profile.Country ?? string.Empty).Trim();
            var companyName = string.IsNullOrWhiteSpace(profile.CompanyName) ? normalizedSymbol : profile.CompanyName.Trim();
            var sector = (profile.Sector ?? string.Empty).Trim();
            var category = (profile.Category ?? string.Empty).Trim();
            var summary = (profile.Summary ?? string.Empty).Trim();

            if (profile.AssetType == AssetTypeEnum.Crypto)
            {
                throw new InvalidOperationException($"L'instrument {normalizedSymbol} est une crypto-monnaie et n'est pas pris en charge. Seules les actions et ETF cotes sont acceptes.");
            }

            if (profile.AssetType != AssetTypeEnum.Stock && profile.AssetType != AssetTypeEnum.Etf)
            {
                throw new InvalidOperationException($"L'instrument {normalizedSymbol} n'est pas une action ou un ETF cote. Seuls les instruments de type action ou ETF sont pris en charge.");
            }

            if (string.IsNullOrWhiteSpace(exchange) ||
                string.IsNullOrWhiteSpace(currency) ||
                profile.LastPrice <= 0m)
            {
                throw new InvalidOperationException($"L'instrument {normalizedSymbol} ne dispose pas de donnees de marche exploitables (exchange, devise ou prix manquants).");
            }

            return new MarketAssetProfileData
            {
                Symbol = normalizedSymbol,
                ProviderSymbol = normalizedProviderSymbol,
                CompanyName = companyName,
                AssetType = profile.AssetType,
                Exchange = exchange,
                Currency = currency,
                Country = country,
                Sector = sector,
                Category = category,
                Summary = summary,
                LastPrice = profile.LastPrice,
                DayVariationPct = profile.DayVariationPct,
                AsOfUtc = profile.AsOfUtc
            };
        }

        private static string NormalizeExchange(string? exchange)
            => (exchange ?? string.Empty).Trim().ToUpperInvariant();

        private static string NormalizeSymbol(string? symbol)
        {
            var normalized = (symbol ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ArgumentException("Symbol is required.", nameof(symbol));
            }

            return normalized;
        }
    }
}

