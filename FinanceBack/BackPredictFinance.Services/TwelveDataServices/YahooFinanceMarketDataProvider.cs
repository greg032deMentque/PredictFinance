using System.Text.Json;
using System.Net;
using BackPredictFinance.Common;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
        /// Retourne les cotations courantes d'une liste de symboles en un seul appel groupé.
        /// Les symboles introuvables ou en erreur sont silencieusement absents du dictionnaire retourné.
        /// </summary>
        Task<IReadOnlyDictionary<string, MarketQuoteData>> GetQuotesAsync(IEnumerable<string> symbols, CancellationToken ct = default);
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
    /// Implémente l'accès au fournisseur de données de marché distant.
    /// </summary>
    public sealed class YahooFinanceMarketDataProvider : IMarketCatalogProvider, IMarketPriceProvider, IFundamentalsProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly MarketDataOptions _options;
        private readonly IYahooCrumbService _crumbService;
        private readonly ILogger<YahooFinanceMarketDataProvider> _logger;

        public YahooFinanceMarketDataProvider(HttpClient httpClient, IMemoryCache cache, IOptions<MarketDataOptions> options, IYahooCrumbService crumbService, ILogger<YahooFinanceMarketDataProvider> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _options = options.Value;
            _crumbService = crumbService;
            _logger = logger;

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

            // Pas d'endpoint "quote" dedie cote Yahoo v10 pour ce cas d'usage : on derive la cotation courante
            // et sa variation journaliere des 2 dernieres bougies d'un chart 1 jour sur 5 jours (marge pour
            // couvrir les week-ends/jours feries sans donnee).
            var chart = await GetChartInternalAsync(normalized, "1d", "5d", ct);
            if (chart.Candles.Count == 0)
            {
                throw new InvalidOperationException($"No market data returned for {normalized}");
            }

            var last = chart.Candles[^1];
            // S'il n'existe qu'une seule bougie exploitable (ex. instrument recemment cote), on ne peut pas
            // calculer de variation : on retombe sur le close courant comme reference, ce qui neutralise le %
            // (voir garde division par zero juste apres) plutot que de lever une exception.
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

        public async Task<IReadOnlyDictionary<string, MarketQuoteData>> GetQuotesAsync(IEnumerable<string> symbols, CancellationToken ct = default)
        {
            var normalized = (symbols ?? [])
                .Select(s => (s ?? string.Empty).Trim().ToUpperInvariant())
                .Where(s => s.Length > 0)
                .Distinct()
                .ToList();

            var result = new Dictionary<string, MarketQuoteData>(StringComparer.OrdinalIgnoreCase);
            if (normalized.Count == 0)
            {
                return result;
            }

            // Concurrence volontairement limitee : Yahoo n'expose pas d'endpoint batch officiel pour les
            // cotations, donc on parallelise des appels individuels tout en restant sous le radar du
            // rate-limiting / anti-bot Yahoo cote query1.finance.yahoo.com.
            const int maxConcurrency = 4;
            using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

            var tasks = normalized.Select(async symbol =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    var quote = await GetQuoteAsync(symbol, ct);
                    return (Symbol: symbol, Quote: (MarketQuoteData?)quote);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Tolerance volontaire : un symbole en erreur (non cote, donnees Yahoo absentes, etc.)
                    // ne doit pas faire echouer le lot entier. Cf. doc de l'interface : les symboles en echec
                    // sont silencieusement absents du dictionnaire retourne.
                    _logger.LogWarning(ex, "GetQuotesAsync: cotation indisponible pour {Symbol}, ignoré", symbol);
                    return (Symbol: symbol, Quote: (MarketQuoteData?)null);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var entries = await Task.WhenAll(tasks);
            foreach (var entry in entries)
            {
                if (entry.Quote is not null)
                {
                    result[entry.Symbol] = entry.Quote;
                }
            }

            return result;
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
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Yahoo quoteSummary profile {Symbol} -> HTTP {StatusCode}. Body(300): {Body}",
                    normalized, (int)response.StatusCode,
                    errorBody.Length > 300 ? errorBody[..300] : errorBody);
                throw new InvalidOperationException($"Yahoo quoteSummary profile failed for {normalized}: HTTP {(int)response.StatusCode}");
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = document.RootElement;
            if (!root.TryGetProperty("quoteSummary", out var quoteSummaryNode)
                || !quoteSummaryNode.TryGetProperty("result", out var resultArrayNode)
                || resultArrayNode.ValueKind != JsonValueKind.Array
                || resultArrayNode.GetArrayLength() == 0)
            {
                _logger.LogWarning("Yahoo quoteSummary profile {Symbol}: réponse sans result exploitable", normalized);
                throw new InvalidOperationException($"Yahoo quoteSummary profile returned no result for {normalized}");
            }

            var result = resultArrayNode[0];
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
                Country = NormalizeCountry(ReadString(summaryProfile, "country", "domicile")),
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
            var url = $"{_options.YahooQuoteSummaryUrl}/{Uri.EscapeDataString(normalized)}?modules=price,summaryProfile,assetProfile,fundProfile,financialData,defaultKeyStatistics,summaryDetail,calendarEvents";
            using var response = await SendV10Async(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Yahoo quoteSummary fundamentals {Symbol} -> HTTP {StatusCode}. Body(300): {Body}",
                    normalized, (int)response.StatusCode,
                    errorBody.Length > 300 ? errorBody[..300] : errorBody);
                throw new InvalidOperationException($"Yahoo quoteSummary fundamentals failed for {normalized}: HTTP {(int)response.StatusCode}");
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = document.RootElement;
            if (!root.TryGetProperty("quoteSummary", out var quoteSummaryNode)
                || !quoteSummaryNode.TryGetProperty("result", out var resultArrayNode)
                || resultArrayNode.ValueKind != JsonValueKind.Array
                || resultArrayNode.GetArrayLength() == 0)
            {
                _logger.LogWarning("Yahoo quoteSummary fundamentals {Symbol}: réponse sans result exploitable", normalized);
                throw new InvalidOperationException($"Yahoo quoteSummary fundamentals returned no result for {normalized}");
            }

            var result = resultArrayNode[0];
            var price = result.TryGetProperty("price", out var priceNode) ? priceNode : default;
            var summaryProfile = result.TryGetProperty("summaryProfile", out var summaryNode) ? summaryNode : default;
            var assetProfile = result.TryGetProperty("assetProfile", out var assetNode) ? assetNode : default;
            var fundProfile = result.TryGetProperty("fundProfile", out var fundNode) ? fundNode : default;
            var financialData = result.TryGetProperty("financialData", out var financialDataNode) ? financialDataNode : default;
            var defaultKeyStatistics = result.TryGetProperty("defaultKeyStatistics", out var defaultKeyStatisticsNode) ? defaultKeyStatisticsNode : default;
            var summaryDetail = result.TryGetProperty("summaryDetail", out var summaryDetailNode) ? summaryDetailNode : default;
            var calendarEvents = result.TryGetProperty("calendarEvents", out var calendarEventsNode) ? calendarEventsNode : default;

            var fundamentals = new MarketFundamentalData
            {
                Symbol = normalized,
                ProviderSymbol = ReadString(price, "symbol") ?? normalized,
                CompanyName = ReadString(price, "longName", "shortName") ?? normalized,
                AssetType = ParseAssetType(ReadString(price, "quoteType")),
                Exchange = ReadString(price, "exchangeName") ?? string.Empty,
                Currency = ReadString(price, "currency") ?? string.Empty,
                Country = NormalizeCountry(ReadString(summaryProfile, "country", "domicile")),
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
                DividendYield = ReadDecimal(summaryDetail, "dividendYield"),
                MarketCap = ReadDecimal(summaryDetail, "marketCap") ?? ReadDecimal(price, "marketCap"),
                RevenueGrowth = ReadDecimal(financialData, "revenueGrowth"),
                EarningsGrowth = ReadDecimal(financialData, "earningsGrowth"),
                PegRatio = ReadDecimal(defaultKeyStatistics, "pegRatio"),
                PriceToBook = ReadDecimal(defaultKeyStatistics, "priceToBook"),
                RecommendationKey = ReadString(financialData, "recommendationKey"),
                RecommendationMean = ReadDecimal(financialData, "recommendationMean"),
                TargetMeanPrice = ReadDecimal(financialData, "targetMeanPrice"),
                EarningsDate = ReadEarningsDate(calendarEvents)
            };

            _cache.Set(cacheKey, fundamentals, TimeSpan.FromMinutes(_options.ProfileCacheMinutes));
            return fundamentals;
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
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Yahoo chart {Symbol} interval={Interval} range={Range} -> HTTP {StatusCode}. Body(300): {Body}",
                    symbol, normalizedInterval, normalizedRange, (int)response.StatusCode,
                    body.Length > 300 ? body[..300] : body);
                response.EnsureSuccessStatusCode();
            }

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
                // Yahoo renvoie un timestamp pour chaque jour de la plage demandee mais peut laisser les
                // champs OHLC a null (jours feries, suspensions de cotation, trous de donnees fournisseur).
                // Une bougie avec un seul champ OHLC null est inexploitable pour le moteur de patterns : on
                // l'exclut plutot que de la convertir en 0, ce qui fausserait les calculs en aval.
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
                    // Contrairement a l'OHLC, un volume null n'invalide pas la bougie (le prix reste
                    // exploitable) : on le neutralise a 0 plutot que d'exclure toute la bougie.
                    Volume = volumes[index].ValueKind == JsonValueKind.Null ? 0m : Convert.ToDecimal(volumes[index].GetDouble())
                });
            }

            if (candles.Count == 0)
            {
                _logger.LogWarning(
                    "Yahoo chart {Symbol} interval={Interval} range={Range} a renvoye 0 bougie exploitable (timestamps bruts={RawCount}).",
                    symbol, normalizedInterval, normalizedRange, timestampValues.Length);
            }

            var envelope = new YahooChartEnvelope
            {
                AssetType = ParseAssetType(ReadString(meta, "instrumentType")),
                Candles = candles
            };

            _cache.Set(cacheKey, envelope, TimeSpan.FromMinutes(_options.ChartCacheMinutes));
            return envelope;
        }

        // Les endpoints quoteSummary (v10) exigent un couple crumb/cookie valide. Ce crumb peut expirer ou
        // etre invalide independamment de son TTL de cache (rotation cote Yahoo, session revoquee...). Sur un
        // 401, on invalide le cache et on retente une seule fois avec des credentials fraichement acquis
        // plutot que de propager l'echec immediatement.
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

        // Lit la premiere propriete existante parmi plusieurs noms candidats (fallback de champs : Yahoo
        // n'utilise pas toujours le meme nom selon le module quoteSummary, ex. "longname"/"shortname").
        // Gere aussi les deux formes renvoyees par Yahoo pour une meme donnee : valeur JSON brute (string),
        // ou objet enveloppe { raw, fmt } pour les champs formatables ; seule la forme "raw" est fiable pour
        // un parsing programmatique, "fmt" etant une representation deja mise en forme pour affichage humain.
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

        // Meme logique de fallback de champs et de forme { raw, fmt } que ReadString, pour les valeurs
        // numeriques. Convertit systematiquement via double (JSON ne distingue pas int/decimal) avant de
        // reconvertir en decimal, seul type manipule cote metier pour ces montants/ratios.
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

        // Yahoo renvoie earningsDate soit comme une seule date, soit comme un tableau de 2 dates representant
        // une fourchette d'estimation (ex. debut/fin de fenetre probable de publication) quand la date exacte
        // n'est pas encore confirmee. On privilegie la premiere date future du tableau (l'estimation la plus
        // proche et donc la plus actionnable) ; si toutes les dates candidates sont passees (donnee Yahoo pas
        // encore rafraichie post-publication), on retombe sur la plus recente comme dernier repere connu
        // plutot que de retourner null.
        private static DateTime? ReadEarningsDate(JsonElement calendarEvents)
        {
            if (calendarEvents.ValueKind != JsonValueKind.Object || !calendarEvents.TryGetProperty("earningsDate", out var earningsDateNode))
            {
                return null;
            }

            if (earningsDateNode.ValueKind != JsonValueKind.Array)
            {
                return ReadEarningsDateEntry(earningsDateNode);
            }

            var candidateDates = earningsDateNode.EnumerateArray()
                .Select(ReadEarningsDateEntry)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

            if (candidateDates.Count == 0)
            {
                return null;
            }

            var nextFutureDate = candidateDates
                .Where(x => x >= DateTime.UtcNow)
                .OrderBy(x => x)
                .Select(x => (DateTime?)x)
                .FirstOrDefault();

            return nextFutureDate ?? candidateDates.OrderByDescending(x => x).First();
        }

        private static DateTime? ReadEarningsDateEntry(JsonElement node)
        {
            if (node.ValueKind == JsonValueKind.Object
                && node.TryGetProperty("raw", out var rawValue)
                && rawValue.ValueKind == JsonValueKind.Number)
            {
                return DateTimeOffset.FromUnixTimeSeconds(rawValue.GetInt64()).UtcDateTime;
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

        private static string NormalizeCountry(string? raw) => CountryCodeNormalizer.NormalizeToIso2(raw);

        private sealed class YahooChartEnvelope
        {
            public AssetTypeEnum AssetType { get; set; } = AssetTypeEnum.Stock;
            public List<TickerCandle> Candles { get; set; } = [];
        }
    }
}
