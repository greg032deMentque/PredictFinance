namespace BackPredictFinance.Common.enums
{
    public enum TechnicalAnalysisOutcomeTypeEnum
    {
        CrediblePatternFound = 0,
        MultipleCompatiblePatterns = 1,
        NoCrediblePattern = 2,
        InsufficientData = 3,
        UnsupportedInstrument = 4,
        UnsupportedContext = 5
    }

    public enum PatternProgressStatusEnum
    {
        Forming = 0,
        Monitoring = 1,
        Confirmed = 2,
        Invalidated = 3,
        Completed = 4,
        Absent = 5
    }

    public enum ValidationStateEnum
    {
        NotValidated = 0,
        Validated = 1,
        NotApplicable = 2
    }

    public enum SupportAvailabilityStatusEnum
    {
        Full = 0,
        Partial = 1,
        Unavailable = 2
    }

    public enum FreshnessStatusEnum
    {
        Fresh = 0,
        Aging = 1,
        Stale = 2,
        Missing = 3
    }

    public enum HoldingStatusEnum
    {
        NotHeld = 0,
        Held = 1
    }

    public enum RecommendationStrengthEnum
    {
        Low = 0,
        Medium = 1,
        High = 2
    }
}
