namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class PatternStructuralPoint
    {
        public string PointType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public decimal Price { get; set; }
    }
}
