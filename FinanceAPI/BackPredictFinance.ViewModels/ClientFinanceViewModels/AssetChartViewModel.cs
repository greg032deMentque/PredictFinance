namespace BackPredictFinance.ViewModels.ClientFinanceViewModels
{
    public class AssetChartViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string Interval { get; set; } = "1d";
        public string Range { get; set; } = "6mo";
        public List<AssetChartPointViewModel> Points { get; set; } = [];
    }

    public class AssetChartPointViewModel
    {
        public DateTime TimestampUtc { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }
}
