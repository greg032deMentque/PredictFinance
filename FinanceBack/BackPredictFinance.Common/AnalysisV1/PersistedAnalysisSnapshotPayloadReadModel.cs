using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Common.AnalysisV1
{
    /// <summary>
    /// Modèle de lecture d'un snapshot d'analyse persisté (côté projection/restitution).
    /// </summary>
    public sealed class PersistedAnalysisSnapshotPayloadReadModel
    {
        public string SchemaVersion { get; set; } = string.Empty;
        public string SnapshotId { get; set; } = string.Empty;
        public AnalysisOutcome Outcome { get; set; }
        public Instrument InstrumentSnapshot { get; set; } = new();
        public List<string> RequestedPatternIds { get; set; } = [];
        public List<string> ExecutedPatternIds { get; set; } = [];
        public DateTime CompletedAtUtc { get; set; }
        public string PedagogicalSummary { get; set; } = string.Empty;
        public SnapshotPortfolioContextSummary PortfolioContextSnapshot { get; set; } = new();
        public string AnalysisEngineVersion { get; set; } = string.Empty;
        public string? RecommendationPolicyVersion { get; set; }
        public string? ExplanationPolicyVersion { get; set; }
        public List<AnalysisSnapshotPatternRow> PatternRows { get; set; } = [];
        public AnalysisSnapshotRecommendation? Recommendation { get; set; }
        public PersistedModelSnapshotReadModel ModelSnapshot { get; set; } = new();
        public DateTime? EarningsDateUtc { get; set; }
        public PatternAssessmentContract? PrimaryPattern => PatternRows
            .OrderByDescending(x => x.IsPrimaryDisplayCandidate)
            .ThenBy(x => x.DisplayRank)
            .Select(x => x.PatternAssessmentPayload)
            .FirstOrDefault();
    }
}
