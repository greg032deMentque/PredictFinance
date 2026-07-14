using System.Text.Json;
using System.Net;
using BackPredictFinance.Common;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BackPredictFinance.Services.TwelveDataServices
{
    /// <summary>
    /// Fournit la recherche et les profils de marché des instruments.
    /// </summary>
    public interface IMarketCatalogProvider
    {
        /// <summary>
        /// Recherche des instruments à partir d'un texte libre.
        /// </summary>
        Task<IReadOnlyList<MarketAssetDescriptor>> SearchAssetsAsync(string query, CancellationToken ct = default);
        /// <summary>
        /// Retourne le profil de marché d'un instrument.
        /// </summary>
        Task<MarketAssetProfileData> GetAssetProfileAsync(string symbol, CancellationToken ct = default);
    }

    /// <summary>
    /// Fournit les cotations et séries temporelles de marché.
    /// </summary>
    public interface IMarketPriceProvider
    {
        /// <summary>
        /// Retourne la cotation courante d'un instrument.
        /// </summary>
        Task<MarketQuoteData> GetQuoteAsync(string symbol, CancellationToken ct = default);
        /// <summary>
        /// Retourne une série temporelle de chandeliers pour un instrument.
        /// </summary>
        Task<IReadOnlyList<TickerCandle>> GetChartAsync(string symbol, string interval, string range, CancellationToken ct = default);
    }

    /// <summary>
    /// Fournit les données fondamentales utilisées par le scoring.
    /// </summary>
    public interface IFundamentalsProvider
    {
        /// <summary>
        /// Retourne le profil de marché d'un instrument pour les besoins fondamentaux.
        /// </summary>
        Task<MarketAssetProfileData> GetAssetProfileAsync(string symbol, CancellationToken ct = default);
        /// <summary>
        /// Retourne les fondamentaux d'un instrument.
        /// </summary>
        Task<MarketFundamentalData> GetFundamentalsAsync(string symbol, CancellationToken ct = default);
    }

    /// <summary>
    /// Fournit la lecture descriptive d'un ETF (TER, encours, famille, indice).
    /// </summary>
    public interface IEtfProfileProvider
    {
        /// <summary>
        /// Retourne le profil ETF d'un instrument.
        /// </summary>
        Task<MarketEtfProfileData> GetEtfProfileAsync(string symbol, CancellationToken ct = default);
    }

    /// <summary>
    /// Implémente l'accès au fournisseur de données de marché distant.
    /// </summary>
    public sealed class YahooFinanceMarketDataProvider : IMarketCatalogProvider, IMarketPriceProvider, IFundamentalsProvider, IEtfProfileProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly MarketDataOptions _options;
        private readonly IYahooCrumbService _crumbService;

        public YahooFinanceMarketDataProvider(HttpClient httpClient, IMemoryCache cache, IOptions<MarketDataOptions> options, IYahooCrumbService crumbService)
        {
            _httpClient = httpClient;
            _cache = cache;
            _options = options.Value;
            _crumbService = crumbService;

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
            using var response = await SendV10Async(url, ct);
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

        public async Task<MarketFundamentalData> GetFundamentalsAsync(string symbol, CancellationToken ct = default)
        {
            var normalized = NormalizeSymbol(symbol);
            var cacheKey = $"market_fundamentals::{normalized}";
            if (_cache.TryGetValue(cacheKey, out MarketFundamentalData? cached) && cached is not null)
            {
                return cached;
            }

            var quote = await GetQuoteAsync(normalized, ct);
            var url = $"{_options.YahooQuoteSummaryUrl}/{Uri.EscapeDataString(normalized)}?modules=price,summaryProfile,assetProfile,fundProfile,financialData,defaultKeyStatistics,summaryDetail";
            using var response = await SendV10Async(url, ct);
            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = document.RootElement;
            var result = root.GetProperty("quoteSummary").GetProperty("result")[0];
            var price = result.TryGetProperty("price", out var priceNode) ? priceNode : default;
            var summaryProfile = result.TryGetProperty("summaryProfile", out var summaryNode) ? summaryNode : default;
            var assetProfile = result.TryGetProperty("assetProfile", out var assetNode) ? assetNode : default;
            var fundProfile = result.TryGetProperty("fundProfile", out var fundNode) ? fundNode : default;
            var financialData = result.TryGetProperty("financialData", out var financialDataNode) ? financialDataNode : default;
            var defaultKeyStatistics = result.TryGetProperty("defaultKeyStatistics", out var defaultKeyStatisticsNode) ? defaultKeyStatisticsNode : default;
            var summaryDetail = result.TryGetProperty("summaryDetail", out var summaryDetailNode) ? summaryDetailNode : default;

            var fundamentals = new MarketFundamentalData
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
                AsOfUtc = quote.AsOfUtc,
                ProviderId = "YAHOO_FINANCE",
                ReturnOnEquity = ReadDecimal(financialData, "returnOnEquity"),
                OperatingMargin = ReadDecimal(financialData, "operatingMargins"),
                CurrentRatio = ReadDecimal(financialData, "currentRatio"),
                DebtToEquity = ReadDecimal(financialData, "debtToEquity"),
                TrailingPe = ReadDecimal(summaryDetail, "trailingPE") ?? ReadDecimal(defaultKeyStatistics, "trailingPE"),
                DividendYield = ReadDecimal(summaryDetail, "dividendYield")
            };

            _cache.Set(cacheKey, fundamentals, TimeSpan.FromMinutes(_options.ProfileCacheMinutes));
            return fundamentals;
        }

        public async Task<MarketEtfProfileData> GetEtfProfileAsync(string symbol, CancellationToken ct = default)
        {
            var normalized = NormalizeSymbol(symbol);
            var cacheKey = $"market_etf_profile::{normalized}";
            if (_cache.TryGetValue(cacheKey, out MarketEtfProfileData? cached) && cached is not null)
            {
                return cached;
            }

            var url = $"{_options.YahooQuoteSummaryUrl}/{Uri.EscapeDataString(normalized)}?modules=fundProfile,topHoldings,defaultKeyStatistics,summaryDetail";
            using var response = await SendV10Async(url, ct);
            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = document.RootElement;
            var result = root.GetProperty("quoteSummary").GetProperty("result")[0];
            var fundProfile = result.TryGetProperty("fundProfile", out var fundProfileNode) ? fundProfileNode : default;
            var defaultKeyStatistics = result.TryGetProperty("defaultKeyStatistics", out var defaultKeyStatisticsNode) ? defaultKeyStatisticsNode : default;
            var summaryDetail = result.TryGetProperty("summaryDetail", out var summaryDetailNode) ? summaryDetailNode : default;
            var topHoldings = result.TryGetProperty("topHoldings", out var topHoldingsNode) ? topHoldingsNode : default;

            var profile = new MarketEtfProfileData
            {
                Symbol = normalized,
                FundFamily = ReadString(fundProfile, "family"),
                Category = ReadString(fundProfile, "categoryName"),
                LegalType = ReadString(fundProfile, "legalType"),
                IndexTracked = ReadString(topHoldings, "equityHoldings"),
                TotalExpenseRatio = ReadDecimal(fundProfile, "annualReportExpenseRatio")
                    ?? ReadDecimal(defaultKeyStatistics, "annualHoldingsTurnover"),
                TotalAssets = ReadDecimal(summaryDetail, "totalAssets"),
                ReplicationMethod = null,
                YtdReturn = ReadDecimal(fundProfile, "ytdReturn"),
                ThreeYearAverageReturn = ReadDecimal(fundProfile, "threeYearAverageReturn"),
                FiveYearAverageReturn = ReadDecimal(fundProfile, "fiveYearAverageReturn")
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

        private async Task<HttpResponseMessage> SendV10Async(string url, CancellationToken ct)
        {
            var credentials = await _crumbService.GetCredentialsAsync(ct);
            var response = await SendV10WithCredentialsAsync(url, credentials, ct);
            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return response;
            }

            response.Dispose();
            _crumbService.Invalidate();

            var refreshedCredentials = await _crumbService.GetCredentialsAsync(ct);
            return await SendV10WithCredentialsAsync(url, refreshedCredentials, ct);
        }

        private async Task<HttpResponseMessage> SendV10WithCredentialsAsync(string url, YahooCredentials credentials, CancellationToken ct)
        {
            var requestUrl = AppendCrumbToUrl(url, credentials.Crumb);
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.TryAddWithoutValidation("Cookie", credentials.CookieHeader);

            return await _httpClient.SendAsync(request, ct);
        }

        private static string AppendCrumbToUrl(string url, string crumb)
        {
            var separator = url.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            return $"{url}{separator}crumb={Uri.EscapeDataString(crumb)}";
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
}
