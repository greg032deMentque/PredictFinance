using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels
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
