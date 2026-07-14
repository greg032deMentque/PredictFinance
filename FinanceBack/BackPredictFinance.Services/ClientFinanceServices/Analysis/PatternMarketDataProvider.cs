using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Services.TwelveDataServices;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    /// <summary>
    /// Adapte le service de marché applicatif au contrat attendu par le moteur de patterns.
    /// </summary>
    public sealed class PatternMarketDataProvider : IPatternMarketDataProvider
    {
        private readonly ITickerService _tickerService;

        public PatternMarketDataProvider(ITickerService tickerService)
        {
            _tickerService = tickerService;
        }

        public Task<TickerTimeSeriesResponse> GetTimeSeriesAsync(string symbol, string interval, int outputSize, CancellationToken ct = default)
        {
            return _tickerService.GetTimeSeriesAsync(symbol, interval, outputSize, ct);
        }
    }
}
