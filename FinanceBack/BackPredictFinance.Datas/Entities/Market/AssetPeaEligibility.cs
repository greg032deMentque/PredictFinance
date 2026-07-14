using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Conserve l'état d'éligibilité PEA d'un actif avec sa source et sa traçabilité.
    /// </summary>
    public class AssetPeaEligibility : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AssetId { get; set; } = string.Empty;
        public Asset Asset { get; set; } = null!;
        public string UniverseId { get; set; } = string.Empty;
        public PeaEligibilityStatusEnum EligibilityStatus { get; set; } = PeaEligibilityStatusEnum.Unknown;
        public PeaEligibilitySourceTypeEnum SourceType { get; set; } = PeaEligibilitySourceTypeEnum.Unknown;
        public string SourceReference { get; set; } = string.Empty;
        public DateTime? CheckedUtc { get; set; }
        public string PolicyVersion { get; set; } = string.Empty;
        public string ReviewerNote { get; set; } = string.Empty;
    }
}
