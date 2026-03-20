namespace BackPredictFinance.Common.enums
{
    public enum TradingPatternEnum
    {
        HeadAndShoulders = 0,
        DoubleTop = 1,
        DoubleBottom = 2,
        CupAndHandle = 3,
        Triangle = 4
    }

    public enum RecommendationActionEnum
    {
        Buy = 0,
        Sell = 1,
        Hold = 2,
        NonActionable = 3
    }

    public enum RiskLevelEnum
    {
        Information = 0,
        Low = 1,
        Moderate = 2,
        High = 3
    }
}
