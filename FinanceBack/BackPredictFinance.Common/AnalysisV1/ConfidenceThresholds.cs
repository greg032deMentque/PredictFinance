namespace BackPredictFinance.Common.AnalysisV1
{
    public static class ConfidenceThresholds
    {
        // Low  : confidence < 0.45
        // Medium: 0.45 <= confidence < 0.75
        // High : confidence >= 0.75
        public const decimal MediumFloor = 0.45m;
        public const decimal HighFloor = 0.75m;
    }
}
