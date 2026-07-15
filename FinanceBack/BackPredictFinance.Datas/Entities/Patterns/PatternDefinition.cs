namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Métadonnée d'affichage d'un pattern chartiste (libellé, famille, description, direction).
    /// L'identifiant doit correspondre à un pattern supporté par le moteur d'analyse.
    /// </summary>
    public sealed class PatternDefinition
    {
        public string PatternId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;

        /// <summary>Libellé FR de la famille (« Continuation de tendance », « Retournement »).</summary>
        public string FamilyLabel { get; set; } = string.Empty;

        /// <summary>Libellé FR de la direction attendue (« Haussière », « Baissière », « Suit la tendance »).</summary>
        public string DirectionLabel { get; set; } = string.Empty;

        /// <summary>Explication pédagogique de la lecture de l'analyse pour ce pattern.</summary>
        public string AnalysisNarrative { get; set; } = string.Empty;

        /// <summary>Fiabilité historique (Bulkowski) du type de figure, entre 0 et 1.</summary>
        public decimal Reliability { get; set; }

        /// <summary>Libellé FR de la fiabilité (« Fiable », « Modérée », « Faible »).</summary>
        public string ReliabilityLabel { get; set; } = string.Empty;
    }
}
