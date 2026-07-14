namespace BackPredictFinance.ViewModels.UserViewModels
{
    public sealed class UserConsentsViewModel
    {
        public bool AnalyticsConsent { get; set; }
        public bool MarketingEmailConsent { get; set; }
        public bool ProductImprovementConsent { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
    }
}
