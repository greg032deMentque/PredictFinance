namespace BackPredictFinance.Common.MarketData
{
    public static class CountryCodeNormalizer
    {
        private static readonly Dictionary<string, string> CountryNameToCode = new(StringComparer.OrdinalIgnoreCase)
        {
            ["France"] = "FR",
            ["United States"] = "US",
            ["Netherlands"] = "NL",
            ["Germany"] = "DE",
            ["United Kingdom"] = "GB",
            ["Switzerland"] = "CH",
            ["Belgium"] = "BE",
            ["Spain"] = "ES",
            ["Italy"] = "IT",
            ["Sweden"] = "SE",
            ["Luxembourg"] = "LU",
            ["Ireland"] = "IE",
            ["Denmark"] = "DK",
            ["Finland"] = "FI",
            ["Norway"] = "NO",
            ["Portugal"] = "PT",
            ["Austria"] = "AT",
            ["Japan"] = "JP",
            ["China"] = "CN",
            ["Canada"] = "CA"
        };

        private static readonly HashSet<string> KnownIso2Codes = new(CountryNameToCode.Values, StringComparer.OrdinalIgnoreCase);

        public static string NormalizeToIso2(string? raw)
        {
            var trimmed = (raw ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                return string.Empty;
            }

            if (KnownIso2Codes.Contains(trimmed))
            {
                return trimmed.ToUpperInvariant();
            }

            return CountryNameToCode.TryGetValue(trimmed, out var code) ? code : trimmed;
        }
    }
}
