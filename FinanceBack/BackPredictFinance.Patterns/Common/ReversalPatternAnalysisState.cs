namespace BackPredictFinance.Patterns.Common
{
    public sealed class ReversalPatternAnalysisState : PatternAnalysisStateBase
    {
        public decimal? NecklinePrice { get; set; }
        public int? LeftShoulderIndex { get; set; }
        public int? HeadIndex { get; set; }
        public int? RightShoulderIndex { get; set; }
        public int? FirstPeakIndex { get; set; }
        public int? SecondPeakIndex { get; set; }
    }
}
