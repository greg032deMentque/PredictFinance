using System.Net.Http.Json;
using System.Text.Json;
using BackPredictFinance.Common;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BackPredictFinance.Services.TwelveDataServices
{
    public sealed class MarketAssetDescriptor
    {
        public string Symbol { get; set; } = string.Empty;
        public string ProviderSymbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public AssetTypeEnum AssetType { get; set; } = AssetTypeEnum.Stock;
        public decimal LastPrice { get; set; }
        public decimal DayVariationPct { get; set; }
    }

    public sealed class MarketQuoteData
    {
        public string Symbol { get; set; } = string.Empty;
        public AssetTypeEnum AssetType { get; set; } = AssetTypeEnum.Stock;
        public decimal LastPrice { get; set; }
        public decimal DayVariationPct { get; set; }
        public DateTime AsOfUtc { get; set; }
    }

    public sealed class MarketAssetProfileData
    {
        public string Symbol { get; set; } = string.Empty;
        public string ProviderSymbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public AssetTypeEnum AssetType { get; set; } = AssetTypeEnum.Stock;
        public string Exchange { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public string Country { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public decimal LastPrice { get; set; }
        public decimal DayVariationPct { get; set; }
        public DateTime AsOfUtc { get; set; }
    }

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
                        Currency = ReadString(quote, "currency") ?? "USD",
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
                Currency = ReadString(price, "currency") ?? "USD",
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
                .Where(x => !string.IsNullOrWhiteSpace(x.Exchange))
                .Select(x => x.Exchange)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(ct);

            if (known.Count > 0)
            {
                return known;
            }

            return ["AMEX", "ARCA", "NASDAQ", "NYSE"];
        }

        public async Task<IReadOnlyList<string>> GetSymbolsByExchangeAsync(string exchange, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(exchange))
            {
                return [];
            }

            return await _context.Assets
                .AsNoTracking()
                .Where(x => x.Exchange == exchange)
                .Select(x => x.Symbol)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<string>> GetAllSymbolsAsync(CancellationToken ct = default)
        {
            return await _context.Assets
                .AsNoTracking()
                .Select(x => x.Symbol)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(ct);
        }

        public async Task<TickerTimeSeriesResponse> GetTimeSeriesAsync(string symbol, string interval, int outputSize, CancellationToken ct = default)
        {
            var normalizedOutputSize = Math.Clamp(outputSize, 5, 500);
            var range = normalizedOutputSize switch
            {
                <= 30 => "1mo",
                <= 90 => "3mo",
                <= 180 => "6mo",
                _ => "1y"
            };

            var candles = await _priceProvider.GetChartAsync(symbol, interval, range, ct);
            var response = new TickerTimeSeriesResponse
            {
                Symbol = symbol.Trim().ToUpperInvariant(),
                Interval = interval,
                OutputSize = normalizedOutputSize,
                Candles = candles.TakeLast(normalizedOutputSize).ToList()
            };
            return response;
        }

        public Task<IReadOnlyList<MarketAssetDescriptor>> SearchAssetsAsync(string query, CancellationToken ct = default)
            => _catalogProvider.SearchAssetsAsync(query, ct);

        public Task<MarketQuoteData> GetQuoteAsync(string symbol, CancellationToken ct = default)
            => _priceProvider.GetQuoteAsync(symbol, ct);

        public Task<MarketAssetProfileData> GetAssetProfileAsync(string symbol, CancellationToken ct = default)
            => _catalogProvider.GetAssetProfileAsync(symbol, ct);
    }

    public class TickerTimeSeriesResponse
    {
        public string Symbol { get; set; } = string.Empty;
        public string Interval { get; set; } = string.Empty;
        public int OutputSize { get; set; }
        public List<TickerCandle> Candles { get; set; } = [];
    }

    public class TickerCandle
    {
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }
}
