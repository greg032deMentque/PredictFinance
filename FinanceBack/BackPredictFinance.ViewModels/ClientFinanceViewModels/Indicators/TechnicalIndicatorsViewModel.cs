namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Indicators
{
    public class TechnicalIndicatorsViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime ComputedAtUtc { get; set; }
        public int DataPointsUsed { get; set; }
        public RsiIndicatorViewModel? Rsi { get; set; }
        public MacdIndicatorViewModel? Macd { get; set; }
        public BollingerBandsViewModel? BollingerBands { get; set; }
        public MovingAveragesViewModel MovingAverages { get; set; } = new();
        public decimal? Obv { get; set; }
        public IndicatorSynthesisViewModel? Synthesis { get; set; }
    }
}
