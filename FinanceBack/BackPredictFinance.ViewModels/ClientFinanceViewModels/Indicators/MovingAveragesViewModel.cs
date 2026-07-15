namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Indicators
{
    public class MovingAveragesViewModel
    {
        public decimal? Ma20 { get; set; }
        public decimal? Ma50 { get; set; }
        public decimal? Ma200 { get; set; }
        public decimal CurrentPrice { get; set; }
    }
}
