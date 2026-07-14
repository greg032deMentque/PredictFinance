namespace BackPredictFinance.Common.MarketData
{
    public class TickerTimeSeriesResponse
    {
        public string Symbol { get; set; } = string.Empty;
        public string Interval { get; set; } = string.Empty;
        public int OutputSize { get; set; }
        public List<TickerCandle> Candles { get; set; } = [];
    }
}
