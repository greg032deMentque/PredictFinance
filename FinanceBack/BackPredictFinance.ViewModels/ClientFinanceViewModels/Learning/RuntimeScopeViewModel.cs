namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Learning
{
    public sealed class RuntimeScopeViewModel
    {
        public string RuntimePerimeterId { get; set; } = string.Empty;
        public string MarketScopeLabel { get; set; } = string.Empty;
        public string TimeGranularity { get; set; } = string.Empty;
        public bool EtfSupportEnabled { get; set; }
        public bool BrokerExecutionEnabled { get; set; }
        public List<RuntimeScopePatternViewModel> SupportedPatterns { get; set; } = [];
    }
}
