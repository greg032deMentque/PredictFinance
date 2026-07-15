using System.Net;
using BackPredictFinance.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BackPredictFinance.Services.TwelveDataServices
{
    /// <summary>
    /// Représente les identifiants de session Yahoo nécessaires aux appels quoteSummary.
    /// </summary>
    /// <param name="Crumb">Jeton anti-CSRF retourné par Yahoo.</param>
    /// <param name="CookieHeader">En-tête Cookie complet associé à la session.</param>
    public sealed record YahooCredentials(string Crumb, string CookieHeader);

    /// <summary>
    /// Fournit les informations de session Yahoo requises pour les appels protégés.
    /// </summary>
    public interface IYahooCrumbService
    {
        /// <summary>
        /// Retourne un couple crumb/cookies valide pour Yahoo Finance.
        /// </summary>
        Task<YahooCredentials> GetCredentialsAsync(CancellationToken ct = default);

        /// <summary>
        /// Invalide les informations en cache afin de forcer un renouvellement.
        /// </summary>
        void Invalidate();
    }

    /// <summary>
    /// Gère l'acquisition et la mise en cache de la session Yahoo Finance.
    /// </summary>
    public sealed class YahooCrumbService : IYahooCrumbService, IDisposable
    {
        private const string CacheKey = "yahoo::crumb_credentials";
        // Duree de cache volontairement courte par rapport a la duree de vie reelle observee du crumb Yahoo :
        // en cas de rotation cote Yahoo, ca limite la fenetre pendant laquelle des credentials perimes seraient
        // servis avant le prochain renouvellement (le retry sur 401 dans YahooFinanceMarketDataProvider.SendV10Async
        // couvre le reste des cas de peremption anticipee).
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(8);

        private readonly IMemoryCache _cache;
        private readonly MarketDataOptions _options;
        private readonly CookieContainer _cookieContainer;
        private readonly HttpClient _httpClient;
        private readonly ILogger<YahooCrumbService> _logger;
        private bool _disposed;

        public YahooCrumbService(IMemoryCache cache, IOptions<MarketDataOptions> options, ILogger<YahooCrumbService> logger)
        {
            _cache = cache;
            _options = options.Value;
            _logger = logger;
            _cookieContainer = new CookieContainer();

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                CookieContainer = _cookieContainer,
                UseCookies = true
            };

            _httpClient = new HttpClient(handler, disposeHandler: true)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_options.UserAgent);
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        }

        public Task<YahooCredentials> GetCredentialsAsync(CancellationToken ct = default)
        {
            if (_cache.TryGetValue(CacheKey, out YahooCredentials? cachedCredentials) && cachedCredentials is not null)
            {
                return Task.FromResult(cachedCredentials);
            }

            return AcquireAndCacheAsync(ct);
        }

        public void Invalidate()
        {
            _cache.Remove(CacheKey);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _httpClient.Dispose();
            _disposed = true;
        }

        private async Task<YahooCredentials> AcquireCredentialsAsync(CancellationToken ct)
        {
            // Le bootstrap ne sert qu'a capturer les cookies de session (Set-Cookie A1/A3).
            // Yahoo renvoie frequemment un statut non-2xx tout en posant ces cookies :
            // on tolere donc le statut ici, seul l'echec du crumb est bloquant.
            // L'ordre est important : le crumb retourne par YahooCrumbUrl est lie a la session dont les cookies
            // viennent d'etre poses par ce bootstrap ; l'appeler sans bootstrap prealable renverrait un crumb
            // orphelin, non associe a un cookie de session valide cote Yahoo.
            HttpStatusCode bootstrapStatus;
            using (var homeRequest = new HttpRequestMessage(HttpMethod.Get, _options.YahooSessionBootstrapUrl))
            {
                using var homeResponse = await _httpClient.SendAsync(homeRequest, ct);
                bootstrapStatus = homeResponse.StatusCode;
            }

            using var crumbRequest = new HttpRequestMessage(HttpMethod.Get, _options.YahooCrumbUrl);
            using var crumbResponse = await _httpClient.SendAsync(crumbRequest, ct);
            if (!crumbResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Yahoo crumb acquisition -> bootstrap={BootstrapStatus} crumb HTTP {CrumbStatus}.",
                    (int)bootstrapStatus, (int)crumbResponse.StatusCode);
                crumbResponse.EnsureSuccessStatusCode();
            }

            var crumb = (await crumbResponse.Content.ReadAsStringAsync(ct)).Trim();
            if (string.IsNullOrWhiteSpace(crumb))
            {
                throw new InvalidOperationException("Yahoo crumb acquisition returned an empty value.");
            }

            // Le CookieContainer .NET peut ne pas associer les cookies de fc.yahoo.com a query1.finance.yahoo.com
            // selon le domaine exact des Set-Cookie. On essaie plusieurs hotes plutot que d'echouer d'emblee :
            // un crumb valide suffit souvent meme avec un cookieHeader vide.
            var cookieHeader = ResolveCookieHeader();
            if (string.IsNullOrWhiteSpace(cookieHeader))
            {
                _logger.LogWarning(
                    "Yahoo session cookies non captures (bootstrap={BootstrapStatus}). Utilisation du crumb sans cookie.",
                    (int)bootstrapStatus);
            }

            return new YahooCredentials(crumb, cookieHeader);
        }

        private string ResolveCookieHeader()
        {
            // Le crumb provient de query1.finance.yahoo.com ; les cookies de session peuvent etre poses
            // sur le domaine parent .yahoo.com. On interroge les deux hotes connus.
            var candidateUris = new[]
            {
                _options.YahooCrumbUrl,
                _options.YahooChartUrl,
                "https://finance.yahoo.com/",
                "https://www.yahoo.com/"
            };

            foreach (var candidate in candidateUris)
            {
                if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
                {
                    continue;
                }

                var header = _cookieContainer.GetCookieHeader(uri);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    return header;
                }
            }

            return string.Empty;
        }

        private async Task<YahooCredentials> AcquireAndCacheAsync(CancellationToken ct)
        {
            var credentials = await AcquireCredentialsAsync(ct);
            _cache.Set(CacheKey, credentials, CacheDuration);
            return credentials;
        }
    }
}
