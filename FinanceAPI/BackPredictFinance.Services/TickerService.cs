using System.Net.Http.Json;
using System.Text.Json;
using BackPredictFinance.Common;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Contracts.MarketData;
using BackPredictFinance.Datas.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BackPredictFinance.Services.TwelveDataServices
{



    public interface IMarketCatalogProvider
    {
        Task<IReadOnlyList<MarketAssetDescriptor>> SearchAssetsAsync(string query, CancellationToken ct = default);
        Task<MarketAssetProfileData> GetAssetProfileAsync(string symbol, CancellationToken ct = default);
    }

    public interface IMarketPriceProvider
    {
        Task<MarketQuoteData> GetQuoteAsync(string symbol, CancellationToken ct = default);
        Task<IReadOnlyList<TickerCandle>> GetChartAsync(string symbol, string interval, string range, CancellationToken ct = default);
    }

    public interface IFundamentalsProvider
    {
        Task<MarketAssetProfileData> GetAssetProfileAsync(string symbol, CancellationToken ct = default);
    }

    public interface ITickerService
    {
        Task<IReadOnlyList<string>> GetExchangesAsync(CancellationToken ct = default);
        Task<IReadOnlyList<string>> GetSymbolsByExchangeAsync(string exchange, CancellationToken ct = default);
        Task<IReadOnlyList<string>> GetAllSymbolsAsync(CancellationToken ct = default);
        Task<TickerTimeSeriesResponse> GetTimeSeriesAsync(string symbol, string interval, int outputSize, CancellationToken ct = default);
        Task<IReadOnlyList<MarketAssetDescriptor>> SearchAssetsAsync(string query, CancellationToken ct = default);
        Task<MarketQuoteData> GetQuoteAsync(string symbol, CancellationToken ct = default);
        Task<MarketAssetProfileData> GetAssetProfileAsync(string symbol, CancellationToken ct = default);
    }

    public sealed class YahooFinanceMarketDataProvider : IMarketCatalogProvider, IMarketPriceProvider, IFundamentalsProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly MarketDataOptions _options;

        public YahooFinanceMarketDataProvider(HttpClient httpClient, IMemoryCache cache, IOptions<MarketDataOptions> options)
        {
            _httpClient = httpClient;
            _cache = cache;
            _options = options.Value;

            if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_options.UserAgent);
            }
        }

        public async Task<IReadOnlyList<MarketAssetDescriptor>> SearchAssetsAsync(string query, CancellationToken ct = default)
        {
            var normalized = (query ?? string.Empty).Trim();
            if (normalized.Length < 1)
            {
                return [];
            }

            var cacheKey = $"market_search::{normalized.ToUpperInvariant()}";
            if (_cache.TryGetValue(cacheKey, out IReadOnlyList<MarketAssetDescriptor>? cached) && cached is not null)
            {
                return cached;
            }

            var url = $"{_options.YahooSearchUrl}?q={Uri.EscapeDataString(normalized)}&quotesCount=20&newsCount=0";
            using var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var items = new List<MarketAssetDescriptor>();
            if (document.RootElement.TryGetProperty("quotes", out var quotes) && quotes.ValueKind == JsonValueKind.Array)
            {
                foreach (var quote in quotes.EnumerateArray())
                {
                    var symbol = ReadString(quote, "symbol");
                    if (string.IsNullOrWhiteSpace(symbol))
                    {
                        continue;
                    }

                    items.Add(new MarketAssetDescriptor
                    {
                        Symbol = symbol.ToUpperInvariant(),
                        ProviderSymbol = ReadString(quote, "symbol") ?? symbol,
                        CompanyName = ReadString(quote, "longname", "shortname") ?? symbol,
                        Exchange = ReadString(quote, "exchDisp", "exchange") ?? string.Empty,
                        Currency = ReadString(quote, "currency") ?? string.Empty,
                        AssetType = ParseAssetType(ReadString(quote, "quoteType")),
                        LastPrice = ReadDecimal(quote, "regularMarketPrice") ?? 0m,
                        DayVariationPct = ReadDecimal(quote, "regularMarketChangePercent") ?? 0m
                    });
                }
            }

            _cache.Set(cacheKey, items, TimeSpan.FromMinutes(_options.SearchCacheMinutes));
            return items;
        }

        public async Task<MarketQuoteData> GetQuoteAsync(string symbol, CancellationToken ct = default)
        {
            var normalized = NormalizeSymbol(symbol);
            var cacheKey = $"market_quote::{normalized}";
            if (_cache.TryGetValue(cacheKey, out MarketQuoteData? cached) && cached is not null)
            {
                return cached;
            }

            var chart = await GetChartInternalAsync(normalized, "1d", "5d", ct);
            if (chart.Candles.Count == 0)
            {
                throw new InvalidOperationException($"No market data returned for {normalized}");
            }

            var last = chart.Candles[^1];
            var previousClose = chart.Candles.Count > 1 ? chart.Candles[^2].Close : last.Close;
            var pct = previousClose == 0m ? 0m : decimal.Round(((last.Close - previousClose) / previousClose) * 100m, 4);

            var quote = new MarketQuoteData
            {
                Symbol = normalized,
                AssetType = chart.AssetType,
                LastPrice = decimal.Round(last.Close, 4),
                DayVariationPct = pct,
                AsOfUtc = last.Date
            };

            _cache.Set(cacheKey, quote, TimeSpan.FromMinutes(_options.QuoteCacheMinutes));
            return quote;
        }

        public async Task<IReadOnlyList<TickerCandle>> GetChartAsync(string symbol, string interval, string range, CancellationToken ct = default)
        {
            var chart = await GetChartInternalAsync(NormalizeSymbol(symbol), interval, range, ct);
            return chart.Candles;
        }

        public async Task<MarketAssetProfileData> GetAssetProfileAsync(string symbol, CancellationToken ct = default)
        {
            var normalized = NormalizeSymbol(symbol);
            var cacheKey = $"market_profile::{normalized}";
            if (_cache.TryGetValue(cacheKey, out MarketAssetProfileData? cached) && cached is not null)
            {
                return cached;
            }

            var quote = await GetQuoteAsync(normalized, ct);
            var url = $"{_options.YahooQuoteSummaryUrl}/{Uri.EscapeDataString(normalized)}?modules=price,summaryProfile,assetProfile,fundProfile";
            using var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = document.RootElement;
            var result = root.GetProperty("quoteSummary").GetProperty("result")[0];
            var price = result.TryGetProperty("price", out var priceNode) ? priceNode : default;
            var summaryProfile = result.TryGetProperty("summaryProfile", out var summaryNode) ? summaryNode : default;
            var assetProfile = result.TryGetProperty("assetProfile", out var assetNode) ? assetNode : default;
            var fundProfile = result.TryGetProperty("fundProfile", out var fundNode) ? fundNode : default;

            var profile = new MarketAssetProfileData
            {
                Symbol = normalized,
                ProviderSymbol = ReadString(price, "symbol") ?? normalized,
                CompanyName = ReadString(price, "longName", "shortName") ?? normalized,
                AssetType = ParseAssetType(ReadString(price, "quoteType")),
                Exchange = ReadString(price, "exchangeName") ?? string.Empty,
                Currency = ReadString(price, "currency") ?? string.Empty,
                Country = ReadString(summaryProfile, "country", "domicile") ?? string.Empty,
                Sector = ReadString(summaryProfile, "sector") ?? string.Empty,
                Category = ReadString(fundProfile, "categoryName", "legalType", "family") ?? string.Empty,
                Summary = ReadString(summaryProfile, "longBusinessSummary") ?? ReadString(assetProfile, "longBusinessSummary") ?? string.Empty,
                LastPrice = quote.LastPrice,
                DayVariationPct = quote.DayVariationPct,
                AsOfUtc = quote.AsOfUtc
            };

            _cache.Set(cacheKey, profile, TimeSpan.FromMinutes(_options.ProfileCacheMinutes));
            return profile;
        }

        private async Task<YahooChartEnvelope> GetChartInternalAsync(string symbol, string interval, string range, CancellationToken ct)
        {
            var normalizedInterval = string.IsNullOrWhiteSpace(interval) ? "1d" : interval.Trim().ToLowerInvariant();
            var normalizedRange = string.IsNullOrWhiteSpace(range) ? "6mo" : range.Trim().ToLowerInvariant();
            var cacheKey = $"market_chart::{symbol}::{normalizedInterval}::{normalizedRange}";
            if (_cache.TryGetValue(cacheKey, out YahooChartEnvelope? cached) && cached is not null)
            {
                return cached;
            }

            var url = $"{_options.YahooChartUrl}/{Uri.EscapeDataString(symbol)}?interval={normalizedInterval}&range={normalizedRange}&includePrePost=false";
            using var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var result = document.RootElement.GetProperty("chart").GetProperty("result")[0];
            var meta = result.GetProperty("meta");
            var timestamps = result.TryGetProperty("timestamp", out var timestampNode) ? timestampNode : default;
            var quote = result.GetProperty("indicators").GetProperty("quote")[0];

            var opens = quote.GetProperty("open").EnumerateArray().ToArray();
            var highs = quote.GetProperty("high").EnumerateArray().ToArray();
            var lows = quote.GetProperty("low").EnumerateArray().ToArray();
            var closes = quote.GetProperty("close").EnumerateArray().ToArray();
            var volumes = quote.GetProperty("volume").EnumerateArray().ToArray();
            var timestampValues = timestamps.ValueKind == JsonValueKind.Array ? timestamps.EnumerateArray().ToArray() : [];

            var candles = new List<TickerCandle>(timestampValues.Length);
            for (var index = 0; index < timestampValues.Length; index++)
            {
                if (opens[index].ValueKind is JsonValueKind.Null ||
                    highs[index].ValueKind is JsonValueKind.Null ||
                    lows[index].ValueKind is JsonValueKind.Null ||
                    closes[index].ValueKind is JsonValueKind.Null)
                {
                    continue;
                }

                var timestampUtc = DateTimeOffset.FromUnixTimeSeconds(timestampValues[index].GetInt64()).UtcDateTime;
                candles.Add(new TickerCandle
                {
                    Date = timestampUtc,
                    Open = Convert.ToDecimal(opens[index].GetDouble()),
                    High = Convert.ToDecimal(highs[index].GetDouble()),
                    Low = Convert.ToDecimal(lows[index].GetDouble()),
                    Close = Convert.ToDecimal(closes[index].GetDouble()),
                    Volume = volumes[index].ValueKind == JsonValueKind.Null ? 0m : Convert.ToDecimal(volumes[index].GetDouble())
                });
            }

            var envelope = new YahooChartEnvelope
            {
                AssetType = ParseAssetType(ReadString(meta, "instrumentType")),
                Candles = candles
            };

            _cache.Set(cacheKey, envelope, TimeSpan.FromMinutes(_options.ChartCacheMinutes));
            return envelope;
        }

        private static string NormalizeSymbol(string symbol)
        {
            var normalized = (symbol ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ArgumentException("Symbol is required.", nameof(symbol));
            }

            return normalized;
        }

        private static string? ReadString(JsonElement node, params string[] propertyNames)
        {
            if (node.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var propertyName in propertyNames)
            {
                if (node.TryGetProperty(propertyName, out var property))
                {
                    if (property.ValueKind == JsonValueKind.String)
                    {
                        return property.GetString();
                    }

                    if (property.ValueKind == JsonValueKind.Object && property.TryGetProperty("raw", out var rawString) && rawString.ValueKind == JsonValueKind.String)
                    {
                        return rawString.GetString();
                    }
                }
            }

            return null;
        }

        private static decimal? ReadDecimal(JsonElement node, params string[] propertyNames)
        {
            if (node.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var propertyName in propertyNames)
            {
                if (!node.TryGetProperty(propertyName, out var property))
                {
                    continue;
                }

                if (property.ValueKind == JsonValueKind.Number)
                {
                    return Convert.ToDecimal(property.GetDouble());
                }

                if (property.ValueKind == JsonValueKind.Object && property.TryGetProperty("raw", out var rawValue) && rawValue.ValueKind == JsonValueKind.Number)
                {
                    return Convert.ToDecimal(rawValue.GetDouble());
                }
            }

            return null;
        }

        private static AssetTypeEnum ParseAssetType(string? quoteType)
        {
            return (quoteType ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "ETF" => AssetTypeEnum.Etf,
                "CRYPTOCURRENCY" => AssetTypeEnum.Crypto,
                _ => AssetTypeEnum.Stock
            };
        }

        private sealed class YahooChartEnvelope
        {
            public AssetTypeEnum AssetType { get; set; } = AssetTypeEnum.Stock;
            public List<TickerCandle> Candles { get; set; } = [];
        }
    }

    public class TickerService : ITickerService
    {
        private readonly IMarketCatalogProvider _catalogProvider;
        private readonly IMarketPriceProvider _priceProvider;
        private readonly FinanceDbContext _context;

        public TickerService(IMarketCatalogProvider catalogProvider, IMarketPriceProvider priceProvider, FinanceDbContext context)
        {
            _catalogProvider = catalogProvider;
            _priceProvider = priceProvider;
            _context = context;
        }

        public async Task<IReadOnlyList<string>> GetExchangesAsync(CancellationToken ct = default)
        {
            var known = await _context.Assets
                .AsNoTracking()
                .Select(x => new
                {
                    x.Exchange,
                    x.Country,
                    x.AssetType
                })
                .ToListAsync(ct);

            return known
                .Where(x => x.AssetType == AssetTypeEnum.Stock && IsFrenchCountry(x.Country))
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
                .Where(x => x.AssetType == AssetTypeEnum.Stock && IsFrenchCountry(x.Country))
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
                .Where(x => x.AssetType == AssetTypeEnum.Stock && IsFrenchCountry(x.Country))
                .Select(x => NormalizeSymbol(x.Symbol))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<TickerTimeSeriesResponse> GetTimeSeriesAsync(string symbol, string interval, int outputSize, CancellationToken ct = default)
        {
            var profile = await GetEligibleFrenchEquityProfileAsync(symbol, ct);
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
                    var profile = await GetEligibleFrenchEquityProfileAsync(descriptor.Symbol, ct);
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
            var profile = await GetEligibleFrenchEquityProfileAsync(symbol, ct);
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
            => GetEligibleFrenchEquityProfileAsync(symbol, ct);

        private async Task<MarketAssetProfileData> GetEligibleFrenchEquityProfileAsync(string symbol, CancellationToken ct)
        {
            var normalizedSymbol = NormalizeSymbol(symbol);
            var profile = await _catalogProvider.GetAssetProfileAsync(normalizedSymbol, ct);
            return EnsureFrenchEquityEligibility(profile, normalizedSymbol);
        }

        private static MarketAssetProfileData EnsureFrenchEquityEligibility(MarketAssetProfileData profile, string symbol)
        {
            ArgumentNullException.ThrowIfNull(profile);

            var normalizedSymbol = NormalizeSymbol(string.IsNullOrWhiteSpace(profile.Symbol) ? symbol : profile.Symbol);
            var normalizedProviderSymbol = NormalizeSymbol(string.IsNullOrWhiteSpace(profile.ProviderSymbol) ? normalizedSymbol : profile.ProviderSymbol);
            var exchange = NormalizeExchange(profile.Exchange);
            var currency = (profile.Currency ?? string.Empty).Trim().ToUpperInvariant();
            var normalizedCountry = NormalizeCountryCode(profile.Country);
            var companyName = string.IsNullOrWhiteSpace(profile.CompanyName) ? normalizedSymbol : profile.CompanyName.Trim();
            var sector = (profile.Sector ?? string.Empty).Trim();
            var category = (profile.Category ?? string.Empty).Trim();
            var summary = (profile.Summary ?? string.Empty).Trim();

            if (profile.AssetType != AssetTypeEnum.Stock ||
                normalizedCountry != "FR" ||
                string.IsNullOrWhiteSpace(exchange) ||
                string.IsNullOrWhiteSpace(currency) ||
                profile.LastPrice <= 0m)
            {
                throw new InvalidOperationException($"L'instrument {normalizedSymbol} n'entre pas dans le perimetre V1. Seules les actions francaises cotees avec des donnees de marche disponibles sont prises en charge.");
            }

            return new MarketAssetProfileData
            {
                Symbol = normalizedSymbol,
                ProviderSymbol = normalizedProviderSymbol,
                CompanyName = companyName,
                AssetType = profile.AssetType,
                Exchange = exchange,
                Currency = currency,
                Country = normalizedCountry,
                Sector = sector,
                Category = category,
                Summary = summary,
                LastPrice = profile.LastPrice,
                DayVariationPct = profile.DayVariationPct,
                AsOfUtc = profile.AsOfUtc
            };
        }

        private static string NormalizeCountryCode(string? country)
        {
            return (country ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "FR" => "FR",
                "FRANCE" => "FR",
                _ => string.Empty
            };
        }

        private static bool IsFrenchCountry(string? country)
            => NormalizeCountryCode(country) == "FR";

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
