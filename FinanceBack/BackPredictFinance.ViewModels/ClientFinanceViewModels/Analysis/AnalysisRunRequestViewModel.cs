using BackPredictFinance.Common.enums;
using System.Collections.Generic;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis
{
    public class AnalysisRunRequestViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public List<string> RequestedPatternIds { get; set; } = [];
        public HoldingStatusEnum? HoldingContext { get; set; }
    }
}
