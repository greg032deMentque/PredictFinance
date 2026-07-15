using System;
using System.Collections.Generic;
using System.Text;
using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Assets;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Watchlist
{
    public class WatchlistItemViewModel
    {
        public string UserAssetId { get; set; } = string.Empty;
        public InstrumentIdentityViewModel Instrument { get; set; } = new();
        public string Symbol { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Market { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public decimal LastPrice { get; set; }
        public decimal LastPriceEur { get; set; }
        public decimal ForexRateUsed { get; set; } = 1m;
        public decimal DayVariationPct { get; set; }
        public decimal HeldQuantity { get; set; }
        public decimal AverageBuyPrice { get; set; }
        public decimal InvestedAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public HoldingStatusEnum HoldingStatus { get; set; } = HoldingStatusEnum.NotHeld;
        public MarketReadingSummaryViewModel MarketReading { get; set; } = new();
        public SupportReadingSummaryViewModel SupportReading { get; set; } = new();
        public RecommendationSummaryViewModel Recommendation { get; set; } = new();
        public bool HasPersistedAnalysis { get; set; }
        public DateTime? LastAnalysisAtUtc { get; set; }
        public FreshnessViewModel Freshness { get; set; } = new();
        public DateTime? NextEarningsDateUtc { get; set; }
        public bool EarningsWithinHorizonWarning { get; set; }
    }
}
