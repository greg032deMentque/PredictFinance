namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Learning
{
    public sealed class OnboardingGuidanceViewModel
    {
        public bool ShouldDisplay { get; set; }
        public string GuidanceCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<OnboardingStepViewModel> SuggestedSteps { get; set; } = [];
    }
}
