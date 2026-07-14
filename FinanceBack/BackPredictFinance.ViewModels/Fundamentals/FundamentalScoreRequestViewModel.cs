namespace BackPredictFinance.ViewModels.Fundamentals
{
    public sealed class FundamentalScoreRequestViewModel
    {
        public string UniverseId { get; set; } = string.Empty;
        public List<string> Symbols { get; set; } = [];
        public int MinCategoriesRequired { get; set; } = 3;
        public bool CoveragePenaltyEnabled { get; set; } = true;
        public bool IncludeRankPosition { get; set; }
    }
}
