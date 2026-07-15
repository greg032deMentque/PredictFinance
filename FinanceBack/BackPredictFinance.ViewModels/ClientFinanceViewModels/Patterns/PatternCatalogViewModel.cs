namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Patterns
{
    public sealed class PatternCatalogViewModel
    {
        public required string Id { get; init; }
        public required string Label { get; init; }
        public required string Family { get; init; }
        public required string Description { get; init; }
        public required string Direction { get; init; }
        public required string FamilyLabel { get; init; }
        public required string DirectionLabel { get; init; }
        public required string AnalysisNarrative { get; init; }
        public required decimal Reliability { get; init; }
        public required string ReliabilityLabel { get; init; }
    }
}
