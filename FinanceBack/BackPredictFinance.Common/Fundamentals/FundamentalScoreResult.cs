namespace BackPredictFinance.Common.Fundamentals
{
    public sealed class FundamentalScoreResult
    {
        public string Symbol { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool UsableScore { get; set; }
        public decimal? TotalScore { get; set; }
        public int CategoriesPresent { get; set; }
        public decimal CategoryCoverage { get; set; }
        public decimal? ProfitabilityScore { get; set; }
        public decimal? LiquidityScore { get; set; }
        public decimal? DebtScore { get; set; }
        public decimal? ValuationScore { get; set; }
        public decimal? DividendScore { get; set; }
        public List<string> MissingMetrics { get; set; } = [];
        public int? RankPosition { get; set; }
        public int? UniverseSize { get; set; }
        public List<string> Notes { get; set; } = [];
        public PeaEligibilityInfo PeaEligibility { get; set; } = new();
    }
}
