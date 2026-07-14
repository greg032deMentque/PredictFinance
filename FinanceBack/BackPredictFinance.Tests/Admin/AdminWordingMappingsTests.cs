using AutoMapper;
using BackPredictFinance.Common.AdminGovernance;
using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.AdminViewModels.Wording;
using Microsoft.Extensions.Logging.Abstractions;

namespace BackPredictFinance.Tests.Admin;

public sealed class AdminWordingMappingsTests
{
    private static readonly IMapper Mapper = CreateMapper();

    [Fact]
    public void AdminWordingVersionDetail_Mapping_PreservesGovernedFields()
    {
        var detail = new AdminWordingVersionDetail
        {
            WordingVersionId = "REC_WORDING_V1",
            DisplayName = "Recommendation wording V1",
            PublicationState = new WordingPublicationState
            {
                IsActive = true,
                ActivatedAtUtc = new DateTime(2026, 4, 13, 8, 0, 0, DateTimeKind.Utc),
                RecommendationPolicyVersion = "analysis-v1-policy@prompt3",
                ExplanationPolicyVersion = "analysis-v1-explanation@prompt5",
                AffectedDomains = ["analysis", "history"]
            },
            Scenario = new WordingScenarioTemplate
            {
                ScenarioCode = "HELD_REINFORCE",
                RecommendationKind = RecommendationKind.Reinforce,
                HoldingStatus = HoldingStatusEnum.Held,
                ActionVerbFamilyCode = "REINFORCE",
                SupportedStrengths = [RecommendationStrengthEnum.Low, RecommendationStrengthEnum.Medium],
                TemplateSummary = "Template"
            }
        };

        var payload = Mapper.Map<AdminWordingVersionDetailViewModel>(detail);

        Assert.Equal("REC_WORDING_V1", payload.WordingVersionId);
        Assert.True(payload.PublicationState.IsActive);
        Assert.Equal("analysis-v1-policy@prompt3", payload.PublicationState.RecommendationPolicyVersion);
        Assert.Equal("HELD_REINFORCE", payload.Scenario.ScenarioCode);
        Assert.Equal(RecommendationKind.Reinforce, payload.Scenario.RecommendationKind);
        Assert.Equal(HoldingStatusEnum.Held, payload.Scenario.HoldingStatus);
    }

    private static IMapper CreateMapper()
    {
        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(AdminWordingVersionDetailViewModelProfile).Assembly);
        }, NullLoggerFactory.Instance);

        return mapperConfiguration.CreateMapper();
    }
}
