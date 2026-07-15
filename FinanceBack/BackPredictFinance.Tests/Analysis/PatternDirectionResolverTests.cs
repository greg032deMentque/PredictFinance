using BackPredictFinance.Common.enums;
using BackPredictFinance.Patterns.Common;

namespace BackPredictFinance.Tests.Analysis;

public sealed class PatternDirectionResolverTests
{
    [Fact]
    public void Resolve_TargetAboveInvalidation_ReturnsBullish()
    {
        var direction = PatternDirectionResolver.Resolve(targetPrice: 120m, invalidationPrice: 90m);

        Assert.Equal(PatternDirectionEnum.Bullish, direction);
    }

    [Fact]
    public void Resolve_TargetBelowInvalidation_ReturnsBearish()
    {
        var direction = PatternDirectionResolver.Resolve(targetPrice: 90m, invalidationPrice: 120m);

        Assert.Equal(PatternDirectionEnum.Bearish, direction);
    }

    [Fact]
    public void Resolve_MissingTargetOrInvalidation_ReturnsUnknown()
    {
        Assert.Equal(PatternDirectionEnum.Unknown, PatternDirectionResolver.Resolve(null, 90m));
        Assert.Equal(PatternDirectionEnum.Unknown, PatternDirectionResolver.Resolve(120m, null));
        Assert.Equal(PatternDirectionEnum.Unknown, PatternDirectionResolver.Resolve(null, null));
    }

    [Fact]
    public void Resolve_TargetEqualsInvalidation_ReturnsUnknown()
    {
        var direction = PatternDirectionResolver.Resolve(targetPrice: 100m, invalidationPrice: 100m);

        Assert.Equal(PatternDirectionEnum.Unknown, direction);
    }
}
