namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Patterns
{
    public sealed class PatternCatalogViewModel
    {
        public required string Id { get; init; }
        public required string Label { get; init; }
        public required string Family { get; init; }
        public required string Description { get; init; }
        public required string Direction { get; init; }
    }
}
