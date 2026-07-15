namespace BackPredictFinance.Common.AnalysisV1
{
    public static class EarningsHorizonEvaluator
    {
        public static bool IsWithinHorizon(DateTime? earningsDateUtc, int horizonDays, DateTime emissionDateUtc)
        {
            if (!earningsDateUtc.HasValue || horizonDays <= 0)
            {
                return false;
            }

            return earningsDateUtc.Value >= emissionDateUtc
                && earningsDateUtc.Value <= emissionDateUtc.AddDays(horizonDays);
        }
    }
}
