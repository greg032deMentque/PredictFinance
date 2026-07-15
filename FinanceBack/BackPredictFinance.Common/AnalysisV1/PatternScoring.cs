namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class PatternScoring
    {
        /// <summary>Qualité géométrique de la détection [0-1]. Gouverne Buy/Sell/Hold.</summary>
        public decimal ConfidenceScore { get; set; }

        public string ConfidenceLabel { get; set; } = string.Empty;

        /// <summary>Fiabilité historique de la figure (stats Bulkowski). Nullable : null sur anciens snapshots.</summary>
        public decimal? ProbabilityScore { get; set; }

        public string? ProbabilityLabel { get; set; }

        public bool IsCredible { get; set; }
        public List<string> ScoreReasons { get; set; } = [];
        public string? ScoreVersion { get; set; }
    }
}
