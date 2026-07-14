using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis
{
    public sealed class ActionPlanViewModel
    {
        // Contexte de détention réel du snapshot auquel ce plan s'applique (RM-10/RM-26).
        public HoldingStatusEnum HoldingStatus { get; set; } = HoldingStatusEnum.NotHeld;
        public string PolicyVersion { get; set; } = string.Empty;
        public List<ActionPlanStepViewModel> Steps { get; set; } = [];
    }
}
