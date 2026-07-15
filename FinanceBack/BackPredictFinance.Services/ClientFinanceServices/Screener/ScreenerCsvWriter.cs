using System.Globalization;
using System.Text;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Screener;

namespace BackPredictFinance.Services.ClientFinanceServices.Screener
{
    /// <summary>
    /// Sérialise les lignes du screener en CSV (séparateur ';', UTF-8 BOM) pour compatibilité Excel FR.
    /// </summary>
    public static class ScreenerCsvWriter
    {
        private const char Separator = ';';

        public static byte[] BuildCsv(IReadOnlyList<ScreenerItemViewModel> items)
        {
            var builder = new StringBuilder();

            AppendRow(builder, "Symbol", "Name", "Exchange", "Sector", "Country", "Type", "PEA", "Prix", "Variation", "PER", "Rendement", "Capitalisation");

            foreach (var item in items)
            {
                AppendRow(
                    builder,
                    item.Symbol,
                    item.Name,
                    item.Exchange,
                    item.Sector ?? string.Empty,
                    item.Country ?? string.Empty,
                    item.AssetType.ToString(CultureInfo.InvariantCulture),
                    item.IsPeaEligible ? "Oui" : "Non",
                    item.LastPrice.HasValue ? item.LastPrice.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                    item.DayVariationPct.HasValue ? item.DayVariationPct.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                    item.TrailingPE.HasValue ? item.TrailingPE.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                    item.DividendYield.HasValue ? item.DividendYield.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                    item.MarketCap.HasValue ? item.MarketCap.Value.ToString(CultureInfo.InvariantCulture) : string.Empty);
            }

            return Encode(builder);
        }

        private static void AppendRow(StringBuilder builder, params string[] cells) =>
            builder.AppendLine(string.Join(Separator, cells.Select(Escape)));

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var mustQuote = value.IndexOf(Separator) >= 0
                || value.IndexOf('"') >= 0
                || value.IndexOf('\n') >= 0
                || value.IndexOf('\r') >= 0;

            if (!mustQuote)
                return value;

            return string.Concat("\"", value.Replace("\"", "\"\""), "\"");
        }

        private static byte[] Encode(StringBuilder builder)
        {
            var preamble = Encoding.UTF8.GetPreamble();
            var content = Encoding.UTF8.GetBytes(builder.ToString());
            var result = new byte[preamble.Length + content.Length];
            Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
            Buffer.BlockCopy(content, 0, result, preamble.Length, content.Length);
            return result;
        }
    }
}
