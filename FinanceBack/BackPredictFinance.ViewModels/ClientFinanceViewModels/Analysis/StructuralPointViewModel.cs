namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis
{
    public sealed class StructuralPointViewModel
    {
        public string PointType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public decimal Price { get; set; }
    }
}
