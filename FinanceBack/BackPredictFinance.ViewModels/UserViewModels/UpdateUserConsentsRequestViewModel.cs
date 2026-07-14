namespace BackPredictFinance.ViewModels.UserViewModels
{
    public sealed class UpdateUserConsentsRequestViewModel
    {
        public bool AnalyticsConsent { get; set; }
        public bool MarketingEmailConsent { get; set; }
        public bool ProductImprovementConsent { get; set; }
    }
}
