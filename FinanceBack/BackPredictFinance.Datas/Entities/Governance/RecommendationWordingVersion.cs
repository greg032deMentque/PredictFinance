namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Stocke une version gouvernee de wording backend avec son etat d'activation et ses domaines couverts.
    /// </summary>
    public sealed class RecommendationWordingVersion : AuditableEntityBase
    {
        public string WordingVersionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? ActivatedAtUtc { get; set; }
        public string RecommendationPolicyVersion { get; set; } = string.Empty;
        public string ExplanationPolicyVersion { get; set; } = string.Empty;
        public string AffectedDomains { get; set; } = string.Empty;
        public List<RecommendationWordingScenario> Scenarios { get; set; } = [];
    }
}
