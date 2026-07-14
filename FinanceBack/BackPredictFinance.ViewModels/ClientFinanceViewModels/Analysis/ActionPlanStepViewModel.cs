namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis
{
    public sealed class ActionPlanStepViewModel
    {
        // Token attendu par le front : NOTE_LEVEL | REVIEW_AT | SET_ALERT | HOLDING_REMINDER | WAIT_FOR_DATA.
        public string Kind { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string? Value { get; set; }
        // Token attendu par le front si Kind = SET_ALERT : PATTERN_STATE_CHANGE | LEVEL_CROSSED | DATA_STALE.
        public string? AlertTrigger { get; set; }
    }
}
