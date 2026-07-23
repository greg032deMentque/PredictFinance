using BackPredictFinance.Common.MarketData;

namespace BackPredictFinance.Tests.MarketData;

public sealed class CountryCodeNormalizerTests
{
    [Theory]
    [InlineData("France", "FR")]
    [InlineData("United States", "US")]
    [InlineData("Germany", "DE")]
    public void NormalizeToIso2_FullCountryName_ReturnsIso2Code(string raw, string expected)
    {
        Assert.Equal(expected, CountryCodeNormalizer.NormalizeToIso2(raw));
    }

    [Theory]
    [InlineData("FR")]
    [InlineData("us")]
    [InlineData("de")]
    public void NormalizeToIso2_AlreadyIso2Code_ReturnsItUppercased(string raw)
    {
        Assert.Equal(raw.ToUpperInvariant(), CountryCodeNormalizer.NormalizeToIso2(raw));
    }

    [Fact]
    public void NormalizeToIso2_UnknownCountry_ReturnsRawValueUnchanged()
    {
        Assert.Equal("Atlantis", CountryCodeNormalizer.NormalizeToIso2("Atlantis"));
    }

    [Fact]
    public void NormalizeToIso2_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, CountryCodeNormalizer.NormalizeToIso2(null));
        Assert.Equal(string.Empty, CountryCodeNormalizer.NormalizeToIso2("  "));
    }
}
