using AutoMapper;
using BackPredictFinance.Common.Fundamentals;
using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.Fundamentals;
using Microsoft.Extensions.Logging.Abstractions;

namespace BackPredictFinance.Tests.Api;

public sealed class FundamentalsApiContractTests
{
    private static readonly IMapper Mapper = CreateMapper();

    [Fact]
    public void FundamentalScoreResponse_Mapping_PreservesLockedPeaEnums()
    {
        var response = new FundamentalScoreResponse
        {
            UniverseId = "PEA_FR_EQUITIES",
            ScoringVersion = "FUNDAMENTAL_PERCENTILE_V1",
            EligibilityPolicyVersion = "PEA_REGISTRY_V1",
            ProviderId = "YAHOO_FINANCE",
            AsOfUtcSemantics = "LIVE_BEST_EFFORT",
            Results =
            [
                new FundamentalScoreResult
                {
                    Symbol = "AIRP",
                    DisplayName = "Air Test",
                    UsableScore = true,
                    TotalScore = 0.81m,
                    PeaEligibility = new PeaEligibilityInfo
                    {
                        Status = PeaEligibilityStatusEnum.ConfirmedEligible,
                        SourceType = PeaEligibilitySourceTypeEnum.ManualRegistry,
                        SourceReference = "REG-001",
                        CheckedUtc = new DateTime(2026, 4, 13, 8, 0, 0, DateTimeKind.Utc),
                        PolicyVersion = "PEA_REGISTRY_V1",
                        ReviewerNote = "Validated"
                    }
                }
            ]
        };

        var payload = Mapper.Map<FundamentalScoreResponseViewModel>(response);

        var item = Assert.Single(payload.Results);
        Assert.Equal(PeaEligibilityStatusEnum.ConfirmedEligible, item.PeaEligibilityStatus);
        Assert.Equal(PeaEligibilitySourceTypeEnum.ManualRegistry, item.PeaEligibilitySourceType);
        Assert.Equal("REG-001", item.PeaEligibilitySourceReference);
    }

    private static IMapper CreateMapper()
    {
        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(FundamentalScoreItemViewModelProfile).Assembly);
        }, NullLoggerFactory.Instance);

        return mapperConfiguration.CreateMapper();
    }
}
