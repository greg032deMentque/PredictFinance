namespace BackPredictFinance.ViewModels.UserViewModels
{
    public sealed class AlertPreferencesViewModel
    {
        public bool AlertPatternStateChangeEnabled { get; set; }
        public bool AlertLevelCrossedEnabled { get; set; }
        public bool AlertDataStaleEnabled { get; set; }
    }
}
