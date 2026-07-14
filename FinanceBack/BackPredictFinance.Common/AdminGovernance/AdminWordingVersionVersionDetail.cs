namespace BackPredictFinance.Common.AdminGovernance
{
    /// <summary>
    /// Détail d'une version de wording gouverné (état de publication + scénarios),
    /// projeté par la couche de service et exposé via le ViewModel admin.
    /// </summary>
    public sealed class AdminWordingVersionVersionDetail
    {
        public string WordingVersionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public WordingPublicationState PublicationState { get; set; } = new();
        public List<WordingScenarioTemplate> Scenarios { get; set; } = [];
    }
}
