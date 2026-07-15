using BackPredictFinance.Common.enums;
using Microsoft.Extensions.Logging;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public static class FreshnessClassifier
    {
        private const int StaleThresholdDays = 4;

        /// <summary>
        /// Classe la fraîcheur d'une donnée selon le nombre de jours OUVRÉS boursiers (hors week-end
        /// et hors jours fériés de fermeture Euronext) écoulés depuis sa dernière mise à jour.
        /// </summary>
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
                    "FreshnessClassifier: checkedAt={CheckedAt} reference={Reference} tradingDays={TradingDays}",
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
                if (current.DayOfWeek != DayOfWeek.Saturday
                    && current.DayOfWeek != DayOfWeek.Sunday
                    && !IsEuronextHoliday(current))
                {
                    count++;
                }
                current = current.AddDays(1);
            }
            return count;
        }

        private static bool IsEuronextHoliday(DateTime date)
        {
            var easterSunday = ComputeEasterSunday(date.Year);
            var goodFriday = easterSunday.AddDays(-2);
            var easterMonday = easterSunday.AddDays(1);

            if (date.Date == goodFriday.Date || date.Date == easterMonday.Date)
            {
                return true;
            }

            return (date.Month, date.Day) switch
            {
                (1, 1) => true,
                (5, 1) => true,
                (12, 25) => true,
                (12, 26) => true,
                _ => false
            };
        }

        private static DateTime ComputeEasterSunday(int year)
        {
            var a = year % 19;
            var b = year / 100;
            var c = year % 100;
            var d = b / 4;
            var e = b % 4;
            var f = (b + 8) / 25;
            var g = (b - f + 1) / 3;
            var h = ((19 * a) + b - d - g + 15) % 30;
            var i = c / 4;
            var k = c % 4;
            var l = (32 + (2 * e) + (2 * i) - h - k) % 7;
            var m = (a + (11 * h) + (22 * l)) / 451;
            var month = (h + l - (7 * m) + 114) / 31;
            var day = ((h + l - (7 * m) + 114) % 31) + 1;

            return new DateTime(year, month, day);
        }
    }
}
