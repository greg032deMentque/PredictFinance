using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class AnalysisRiskContext
    {
        public decimal AtrValue { get; set; }
        public decimal? StopLossPrice { get; set; }
        public decimal? Target1Price { get; set; }
        public decimal? Target2Price { get; set; }
        public decimal? RiskRewardRatio { get; set; }
        public decimal VolumeAvg20 { get; set; }
        public decimal VolumeRatio { get; set; }
        public VolumeConfirmation VolumeConfirmation { get; set; }
        public decimal? PositionSizePercent { get; set; }
        public bool EarningsWithinHorizonWarning { get; set; }
        public DateTime? NextEarningsDateUtc { get; set; }
    }

    public sealed class AnalysisTechnicalContext
    {
        public decimal Rsi14 { get; set; }
        public RsiZone RsiZone { get; set; }
        public decimal MacdValue { get; set; }
        public decimal MacdSignal { get; set; }
        public decimal MacdHistogram { get; set; }
        public MacdCross MacdCross { get; set; }
        public MarketRegime MarketRegime { get; set; }
        public bool RegimeWarning { get; set; }
    }
}
