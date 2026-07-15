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
                },
                new PatternDescriptor
                {
                    PatternId = PatternIds.DoubleBottom,
                    DisplayName = "Double Bottom",
                    Family = "reversal",
                    Description = "Two equivalent troughs separated by an intermediate rebound, bullish reversal confirmed by neckline breakout.",
                    Direction = "Bullish"
                },
                new PatternDescriptor
                {
                    PatternId = PatternIds.DoubleTop,
                    DisplayName = "Double Top",
                    Family = "reversal",
                    Description = "Two equivalent peaks separated by an intermediate trough, bearish reversal confirmed by neckline breakdown.",
                    Direction = "Bearish"
                },
                new PatternDescriptor
                {
                    PatternId = PatternIds.InverseHeadAndShoulders,
                    DisplayName = "Tête-Épaules Inversé",
                    Family = "reversal",
                    Description = "Three-trough structure (head deeper than shoulders) signaling bullish reversal on neckline breakout.",
                    Direction = "Bullish"
                },
                new PatternDescriptor
                {
                    PatternId = PatternIds.HeadAndShoulders,
                    DisplayName = "Tête-Épaules",
                    Family = "reversal",
                    Description = "Three-peak structure (head higher than shoulders) signaling bearish reversal on neckline breakdown.",
                    Direction = "Bearish"
                }
            ];
        }
    }
}
