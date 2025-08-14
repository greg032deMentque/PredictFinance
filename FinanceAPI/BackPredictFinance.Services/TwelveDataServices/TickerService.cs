using BackPredictFinance.Common;
using BackPredictFinance.Services.TwelveDataServices.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BackPredictFinance.Services.TwelveDataServices
{
    public interface ITickerService
    {
        /// <summary>
        /// Récupère la liste de toutes les places disponibles (exchanges) chez Twelve Data.
        /// </summary>
        Task<List<string>> GetExchangesAsync();

        /// <summary>
        /// Récupère la liste des symbols pour une place donnée.
        /// </summary>
        Task<List<string>> GetSymbolsByExchangeAsync(string exchange);

        /// <summary>
        /// Récupère tous les symbols, en itérant sur chaque exchange.
        /// </summary>
        Task<List<string>> GetAllSymbolsAsync();

        /// <summary>
        /// Récupère la série temporelle pour un symbole donné.
        /// </summary>
        /// <param name="symbol">Ticker (ex. "AAPL")</param>
        /// <param name="interval">Intervalle (1min, 5min, 15min, 1h, 1day, ...)</param>
        /// <param name="outputSize">Nombre de points à récupérer (max 5000)</param>
        Task<List<TimeSeriesPoint>> GetTimeSeriesAsync(
            string symbol,
            string interval,
            int outputSize = 100
        );
    }

    public class TickerService : BaseService, ITickerService
    {

        private readonly HttpClient _http;
        private readonly TwelveDataOptions _opts;

        public TickerService(IServiceProvider serviceProvider, HttpClient http, IOptions<TwelveDataOptions> opts) : base(serviceProvider)
        {
            _http = http;
            _opts = opts.Value;
        }

        public async Task<List<string>> GetExchangesAsync()
        {
            var uri = $"{_opts.BaseUrl}/exchanges?apikey={_opts.ApiKey}";
            using var resp = await _http.GetAsync(uri);
            resp.EnsureSuccessStatusCode();

            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
            // réponse typique : { "data": [ { "exchange": "NYSE" }, { "exchange": "NASDAQ" }, ... ] }
            var list = doc.RootElement
                .GetProperty("data")
                .EnumerateArray()
                .Select(e => e.GetProperty("exchange").GetString()!)
                .ToList();

            return list;
        }

        public async Task<List<string>> GetSymbolsByExchangeAsync(string exchange)
        {
            // attention à l'encodage de l'exchange si nécessaire
            var uri = $"{_opts.BaseUrl}/symbols?exchange={Uri.EscapeDataString(exchange)}&apikey={_opts.ApiKey}";
            using var resp = await _http.GetAsync(uri);
            resp.EnsureSuccessStatusCode();

            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
            // réponse typique : { "data": [ { "symbol": "AAPL" }, { "symbol": "MSFT" }, ... ] }
            var list = doc.RootElement
                .GetProperty("data")
                .EnumerateArray()
                .Select(e => e.GetProperty("symbol").GetString()!)
                .ToList();

            return list;
        }

        public async Task<List<string>> GetAllSymbolsAsync()
        {
            var exchanges = await GetExchangesAsync();
            var allSymbols = new List<string>();

            foreach (var ex in exchanges)
            {
                var syms = await GetSymbolsByExchangeAsync(ex);
                allSymbols.AddRange(syms);
            }

            return allSymbols.Distinct().ToList();
        }

        public async Task<List<TimeSeriesPoint>> GetTimeSeriesAsync(
        string symbol,
        string interval,
        int outputSize = 100)
        {
            // Construire l’URL : time_series?symbol=AAPL&interval=1day&outputsize=100
            var uri = $"{_opts.BaseUrl}/time_series" +
                      $"?symbol={Uri.EscapeDataString(symbol)}" +
                      $"&interval={Uri.EscapeDataString(interval)}" +
                      $"&outputsize={outputSize}" +
                      $"&apikey={_opts.ApiKey}";

            using var resp = await _http.GetAsync(uri);
            resp.EnsureSuccessStatusCode();

            using var stream = await resp.Content.ReadAsStreamAsync();
            var tsResponse = await JsonSerializer.DeserializeAsync<TimeSeriesResponse>(stream);
            if (tsResponse?.Status != "ok")
                return new List<TimeSeriesPoint>();

            return tsResponse.Values;
        }
    }
}
