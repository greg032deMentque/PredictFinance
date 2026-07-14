using BackPredictFinance.Common.MarketData;

namespace BackPredictFinance.Patterns.Abstractions
{
    public interface IPatternMarketDataProvider
    {
        Task<TickerTimeSeriesResponse> GetTimeSeriesAsync(string symbol, string interval, int outputSize, CancellationToken ct = default);
    }
}
