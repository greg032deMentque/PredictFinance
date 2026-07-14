namespace BackPredictFinance.Common.AdminGovernance
{
    public sealed class WordingPublicationState
    {
        public bool IsActive { get; set; }
        public DateTime? ActivatedAtUtc { get; set; }
        public string RecommendationPolicyVersion { get; set; } = string.Empty;
        public string ExplanationPolicyVersion { get; set; } = string.Empty;
        public List<string> AffectedDomains { get; set; } = [];
    }
}
