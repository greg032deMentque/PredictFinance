namespace BackPredictFinance.Common.Fundamentals
{
    /// <summary>
    /// Valeurs de politique par défaut du scoring fondamental (univers, versions, métriques),
    /// partagées entre le service de scoring et la gouvernance admin.
    /// </summary>
    public static class FundamentalScoringPolicyDefaults
    {
        public const string SupportedUniverseId = "PEA_FR_EQUITIES";
        public const string ScoringVersion = "FUNDAMENTAL_PERCENTILE_V2";
        public const string EligibilityPolicyVersion = "PEA_REGISTRY_V1";
        public const string ProviderId = "YAHOO_FINANCE";
        public const string AsOfUtcSemantics = "LIVE_BEST_EFFORT";
        public static readonly string[] CategoryCodes = ["profitability", "liquidity", "debt", "valuation", "dividend", "growth"];
        public static readonly string[] HigherIsBetterMetricCodes = ["returnOnEquity", "operatingMargin", "currentRatio", "dividendYield", "revenueGrowth", "earningsGrowth"];
        public static readonly string[] LowerIsBetterMetricCodes = ["debtToEquity", "trailingPe", "pegRatio", "priceToBook"];
        public const int MinimumCategoriesRequiredFloor = 1;
        public const int MinimumCategoriesRequiredCeiling = 6;
    }
}
