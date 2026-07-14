using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis
{
    public sealed class TradingPredictionModelCheckCompatibilityViewModel
        {
            public ModelCheckEnum Check { get; set; }
            public ModelCheckStatusEnum Status { get; set; }
            public decimal? Value { get; set; }
            public decimal? Threshold { get; set; }
            public string Detail { get; set; } = string.Empty;
        }
}
