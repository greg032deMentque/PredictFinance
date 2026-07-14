namespace BackPredictFinance.Patterns
{
    public static class PatternCatalog
    {
        public static IReadOnlyList<PatternDescriptor> GetTargetPatterns()
        {
            return
            [
                new PatternDescriptor
                {
                    PatternId = PatternIds.RectangleContinuation,
                    DisplayName = "Rectangle continuation",
                    Family = "continuation",
                    Description = "Horizontal consolidation that validates only if the breakout continues the prior trend.",
                    Direction = "TrendFollowing"
                },
                new PatternDescriptor
                {
                    PatternId = PatternIds.SymmetricalTriangleContinuation,
                    DisplayName = "Symmetrical triangle continuation",
                    Family = "continuation",
                    Description = "Converging range that validates when price breaks in the direction of the established trend.",
                    Direction = "TrendFollowing"
                },
                new PatternDescriptor
                {
                    PatternId = PatternIds.BullFlagContinuation,
                    DisplayName = "Bull flag continuation",
                    Family = "continuation",
                    Description = "Short pullback after a bullish impulse that validates on an upside breakout.",
                    Direction = "Bullish"
                },
                new PatternDescriptor
                {
                    PatternId = PatternIds.BearFlagContinuation,
                    DisplayName = "Bear flag continuation",
                    Family = "continuation",
                    Description = "Short rebound after a bearish impulse that validates on a downside breakout.",
                    Direction = "Bearish"
                }
            ];
        }
    }
}
