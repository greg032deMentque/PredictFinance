using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Common.AnalysisV1
{
    /// <summary>
    /// Projection de la dernière éligibilité PEA connue pour un instrument.
    /// </summary>
    public sealed class LatestPeaEligibilityProjection
    {
        public string UniverseId { get; set; } = string.Empty;
        public PeaEligibilityStatusEnum EligibilityStatus { get; set; } = PeaEligibilityStatusEnum.Unknown;
        public string SourceReference { get; set; } = string.Empty;
        public DateTime? CheckedUtc { get; set; }
    }
}
