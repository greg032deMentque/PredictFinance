namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Learning
{
    public sealed class OnboardingStepViewModel
    {
        public int Order { get; set; }
        public string StepCode { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string RoutePath { get; set; } = string.Empty;
    }
}
