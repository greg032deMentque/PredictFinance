namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings
{
    public sealed class SupportReadingViewModel : SupportReadingSummaryViewModel
    {
        public string? ScoringVersion { get; set; }
        public string? ActiveUniverseId { get; set; }
        public decimal? CoverageRatio { get; set; }
        public decimal? CompositeScore { get; set; }
        public List<string> MissingCategorySummaries { get; set; } = [];
        public List<string> Notes { get; set; } = [];
    }
}
