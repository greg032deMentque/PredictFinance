namespace BackPredictFinance.ViewModels.ClientFinanceViewModels
{
    public class AssetChartViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string Interval { get; set; } = "1d";
        public string Range { get; set; } = "6mo";
        public List<AssetChartPointViewModel> Points { get; set; } = [];
    }
}
