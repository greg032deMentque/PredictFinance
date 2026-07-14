using BackPredictFinance.Common.enums;
using Microsoft.Extensions.Logging;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public static class FreshnessClassifier
    {
        private const int AgingThresholdDays = 2;
        private const int StaleThresholdDays = 4;

        public static FreshnessStatusEnum Classify(DateTime? checkedAtUtc, DateTime referenceUtc, ILogger? logger = null)
        {
            if (!checkedAtUtc.HasValue)
            {
                return FreshnessStatusEnum.Missing;
            }

            var tradingDays = CountTradingDaysBetween(checkedAtUtc.Value.Date, referenceUtc.Date);

            if (logger != null)
            {
                logger.LogDebug(
                    "FreshnessClassifier: checkedAt={CheckedAt} reference={Reference} tradingDays={TradingDays} (feries Euronext ignores en V1)",
                    checkedAtUtc.Value.Date,
                    referenceUtc.Date,
                    tradingDays);
            }

            if (tradingDays <= 1)
            {
                return FreshnessStatusEnum.Fresh;
            }

            if (tradingDays < StaleThresholdDays)
            {
                return FreshnessStatusEnum.Aging;
            }

            return FreshnessStatusEnum.Stale;
        }

        private static int CountTradingDaysBetween(DateTime fromDate, DateTime toDate)
        {
            if (fromDate >= toDate)
            {
                return 0;
            }

            var count = 0;
            var current = fromDate.AddDays(1);
            while (current <= toDate)
            {
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    count++;
                }
                current = current.AddDays(1);
            }
            return count;
        }
    }
}
