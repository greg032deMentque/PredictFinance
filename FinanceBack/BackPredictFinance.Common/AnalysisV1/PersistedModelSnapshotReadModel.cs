using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Common.AnalysisV1
{
    /// <summary>
    /// Modèle de lecture du statut de modèle associé à un snapshot d'analyse persisté.
    /// </summary>
    public sealed class PersistedModelSnapshotReadModel
    {
        public ModelStatusEnum ModelStatus { get; set; }
        public string ModelMessage { get; set; } = string.Empty;
    }
}
