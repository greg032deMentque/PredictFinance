namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Learning
{
    public sealed class LearnOverviewViewModel
    {
        public RuntimeScopeViewModel RuntimeScope { get; set; } = new();
        public List<LearnTopicViewModel> Topics { get; set; } = [];
        public OnboardingGuidanceViewModel? Onboarding { get; set; }
    }
}
