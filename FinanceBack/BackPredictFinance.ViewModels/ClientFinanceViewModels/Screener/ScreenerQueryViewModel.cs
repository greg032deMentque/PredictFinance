namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Screener
{
    public sealed class ScreenerQueryViewModel
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
        public List<string>? Sectors { get; set; }
        public List<string>? Countries { get; set; }
        public bool PeaOnly { get; set; } = false;
        public int? AssetType { get; set; }
        public string? Search { get; set; }
        public decimal? MinPE { get; set; }
        public decimal? MaxPE { get; set; }
        public decimal? MinDividendYield { get; set; }
        public decimal? MinMarketCap { get; set; }
        public decimal? MinScore { get; set; }
    }
}
