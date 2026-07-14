using System.Net;
using BackPredictFinance.Common;
using Microsoft.Extensions.Caching.Memory;
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
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(8);

        private readonly IMemoryCache _cache;
        private readonly MarketDataOptions _options;
        private readonly CookieContainer _cookieContainer;
        private readonly HttpClient _httpClient;
        private bool _disposed;

        public YahooCrumbService(IMemoryCache cache, IOptions<MarketDataOptions> options)
        {
            _cache = cache;
            _options = options.Value;
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
            using (var homeRequest = new HttpRequestMessage(HttpMethod.Get, _options.YahooSessionBootstrapUrl))
            {
                using var homeResponse = await _httpClient.SendAsync(homeRequest, ct);
            }

            using var crumbRequest = new HttpRequestMessage(HttpMethod.Get, _options.YahooCrumbUrl);
            using var crumbResponse = await _httpClient.SendAsync(crumbRequest, ct);
            crumbResponse.EnsureSuccessStatusCode();

            var crumb = (await crumbResponse.Content.ReadAsStringAsync(ct)).Trim();
            if (string.IsNullOrWhiteSpace(crumb))
            {
                throw new InvalidOperationException("Yahoo crumb acquisition returned an empty value.");
            }

            var cookieUri = new Uri(_options.YahooCrumbUrl);
            var cookieHeader = _cookieContainer.GetCookieHeader(cookieUri);
            if (string.IsNullOrWhiteSpace(cookieHeader))
            {
                throw new InvalidOperationException("Yahoo session cookies were not captured.");
            }

            return new YahooCredentials(crumb, cookieHeader);
        }

        private async Task<YahooCredentials> AcquireAndCacheAsync(CancellationToken ct)
        {
            var credentials = await AcquireCredentialsAsync(ct);
            _cache.Set(CacheKey, credentials, CacheDuration);
            return credentials;
        }
    }
}
