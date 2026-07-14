using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Assets
{
    public sealed class FreshnessViewModel
    {
        public FreshnessStatusEnum Status { get; set; } = FreshnessStatusEnum.Missing;
        public DateTime? CheckedAtUtc { get; set; }
        public string DisplayLabel { get; set; } = string.Empty;
    }
}
