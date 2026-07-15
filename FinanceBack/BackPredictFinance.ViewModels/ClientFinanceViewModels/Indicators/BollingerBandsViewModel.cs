namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Indicators
{
    public class BollingerBandsViewModel
    {
        public decimal Upper { get; set; }
        public decimal Middle { get; set; }
        public decimal Lower { get; set; }
        public decimal CurrentPrice { get; set; }
        public string Position { get; set; } = string.Empty;
    }
}
