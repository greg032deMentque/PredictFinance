namespace BackPredictFinance.Services.TwelveDataServices
{
    public interface ITickerService
    {
        Task<IReadOnlyList<string>> GetExchangesAsync();
        Task<IReadOnlyList<string>> GetSymbolsByExchangeAsync(string exchange);
        Task<IReadOnlyList<string>> GetAllSymbolsAsync();
        Task<TickerTimeSeriesResponse> GetTimeSeriesAsync(string symbol, string interval, int outputSize);
    }

    public class TickerService : ITickerService
    {
        private static readonly Dictionary<string, List<string>> ExchangeToSymbols = new(StringComparer.OrdinalIgnoreCase)
        {
            ["NASDAQ"] = ["AAPL", "MSFT", "NVDA", "AMZN", "GOOGL", "META"],
            ["NYSE"] = ["JPM", "XOM"]
        };

        public Task<IReadOnlyList<string>> GetExchangesAsync()
        {
            IReadOnlyList<string> exchanges = ExchangeToSymbols.Keys.OrderBy(x => x).ToList();
            return Task.FromResult(exchanges);
        }

        public Task<IReadOnlyList<string>> GetSymbolsByExchangeAsync(string exchange)
        {
            if (string.IsNullOrWhiteSpace(exchange))
            {
                return Task.FromResult<IReadOnlyList<string>>([]);
            }

            if (!ExchangeToSymbols.TryGetValue(exchange.Trim(), out var symbols))
            {
                return Task.FromResult<IReadOnlyList<string>>([]);
            }

            IReadOnlyList<string> response = symbols.OrderBy(x => x).ToList();
            return Task.FromResult(response);
        }

        public Task<IReadOnlyList<string>> GetAllSymbolsAsync()
        {
            IReadOnlyList<string> symbols = ExchangeToSymbols
                .SelectMany(x => x.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            return Task.FromResult(symbols);
        }

        public Task<TickerTimeSeriesResponse> GetTimeSeriesAsync(string symbol, string interval, int outputSize)
        {
            var normalizedSymbol = string.IsNullOrWhiteSpace(symbol) ? "AAPL" : symbol.Trim().ToUpperInvariant();
            var normalizedInterval = string.IsNullOrWhiteSpace(interval) ? "1day" : interval.Trim().ToLowerInvariant();
            var normalizedOutputSize = Math.Clamp(outputSize, 1, 500);

            var points = new List<TickerCandle>(normalizedOutputSize);
            var start = DateTime.UtcNow.Date.AddDays(-normalizedOutputSize + 1);
            var price = 100m;

            for (var i = 0; i < normalizedOutputSize; i++)
            {
                var day = start.AddDays(i);
                var drift = (decimal)Math.Sin(i / 5.0) * 0.7m;
                var open = price;
                var close = open + drift;
                var high = Math.Max(open, close) + 0.5m;
                var low = Math.Min(open, close) - 0.5m;

                points.Add(new TickerCandle
                {
                    Date = day,
                    Open = decimal.Round(open, 2),
                    High = decimal.Round(high, 2),
                    Low = decimal.Round(low, 2),
                    Close = decimal.Round(close, 2),
                    Volume = 1_000_000 + (i * 1_000)
                });

                price = close;
            }

            var response = new TickerTimeSeriesResponse
            {
                Symbol = normalizedSymbol,
                Interval = normalizedInterval,
                OutputSize = normalizedOutputSize,
                Candles = points
            };

            return Task.FromResult(response);
        }
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
