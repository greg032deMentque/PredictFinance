namespace BackPredictFinance.Patterns
{
    public static class PatternIds
    {
        public const string RectangleContinuation = "RECTANGLE_CONTINUATION";
        public const string SymmetricalTriangleContinuation = "SYMMETRICAL_TRIANGLE_CONTINUATION";
        public const string BullFlagContinuation = "BULL_FLAG_CONTINUATION";
        public const string BearFlagContinuation = "BEAR_FLAG_CONTINUATION";
        public const string DoubleBottom = "DOUBLE_BOTTOM";
        public const string DoubleTop = "DOUBLE_TOP";
        public const string InverseHeadAndShoulders = "INVERSE_HEAD_AND_SHOULDERS";
        public const string HeadAndShoulders = "HEAD_AND_SHOULDERS";

        public static string Normalize(string? patternId)
        {
            return (patternId ?? string.Empty).Trim().ToUpperInvariant();
        }

        public static string RequireActivePatternId(string? patternId)
        {
            var normalizedPatternId = Normalize(patternId);

            return normalizedPatternId switch
            {
                RectangleContinuation => normalizedPatternId,
                SymmetricalTriangleContinuation => normalizedPatternId,
                BullFlagContinuation => normalizedPatternId,
                BearFlagContinuation => normalizedPatternId,
                DoubleBottom => normalizedPatternId,
                DoubleTop => normalizedPatternId,
                InverseHeadAndShoulders => normalizedPatternId,
                HeadAndShoulders => normalizedPatternId,
                _ => throw new InvalidOperationException($"Le pattern {normalizedPatternId} n'est pas pris en charge par le catalogue API.")
            };
        }
    }
}
