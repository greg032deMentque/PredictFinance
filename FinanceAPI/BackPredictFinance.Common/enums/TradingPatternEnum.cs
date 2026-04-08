namespace BackPredictFinance.Common.enums
{
    public enum TradingPatternEnum
    {
        HeadAndShoulders = 0,
        DoubleTop = 1,
        DoubleBottom = 2,
        CupAndHandle = 3,
        Triangle = 4,
        RectangleContinuation = 5,
        SymmetricalTriangleContinuation = 6,
        BullFlagContinuation = 7,
        BearFlagContinuation = 8
    }

    public enum RecommendationActionEnum
    {
        Monitor = 0,
        Buy = 1,
        Wait = 2,
        Hold = 3,
        Reinforce = 4,
        Lighten = 5,
        Sell = 6
    }

    public enum RiskLevelEnum
    {
        Information = 0,
        Low = 1,
        Moderate = 2,
        High = 3
    }
}
