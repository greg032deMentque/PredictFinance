namespace BackPredictFinance.Patterns
{
    public sealed class PatternDescriptor
    {
        public required string PatternId { get; init; }
        public required string DisplayName { get; init; }
        public required string Family { get; init; }
        public required string Description { get; init; }
        public required string Direction { get; init; }
    }
}
