using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Learning;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface IClientFinanceLearningService
    {
        Task<LearnOverviewViewModel> GetLearnOverviewAsync(CancellationToken ct = default);
        Task<OnboardingGuidanceViewModel> GetOnboardingGuidanceAsync(CancellationToken ct = default);
    }

    public sealed class ClientFinanceLearningService : BaseService, IClientFinanceLearningService
    {
        private readonly ILearnTopicService _learnTopicService;

        public ClientFinanceLearningService(IServiceProvider serviceProvider, ILearnTopicService learnTopicService)
            : base(serviceProvider)
        {
            _learnTopicService = learnTopicService;
        }

        public async Task<LearnOverviewViewModel> GetLearnOverviewAsync(CancellationToken ct = default)
        {
            var onboarding = await GetOnboardingGuidanceAsync(ct);
            var topics = await _learnTopicService.GetPublishedAsync(ct);

            return new LearnOverviewViewModel
            {
                RuntimeScope = new RuntimeScopeViewModel
                {
                    RuntimePerimeterId = "v1-european-equities",
                    MarketScopeLabel = "Marchés européens principaux",
                    TimeGranularity = "Données quotidiennes",
                    EtfSupportEnabled = false,
                    BrokerExecutionEnabled = false,
                    SupportedPatterns = []
                },
                Topics = topics,
                Onboarding = onboarding
            };
        }

        public async Task<OnboardingGuidanceViewModel> GetOnboardingGuidanceAsync(CancellationToken ct = default)
        {
            var userId = _currentUserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BuildNewUserGuidance();
            }

            var hasWatchlistAsset = await _financeDbContext.UserAssets
                .AsNoTracking()
                .AnyAsync(x => x.UserId == userId, ct);

            var hasCompletedAnalysis = await _financeDbContext.AnalysisRuns
                .AsNoTracking()
                .AnyAsync(x => x.UserId == userId && x.Status == AnalysisRunStatusEnum.Completed, ct);

            var hasTransaction = await _financeDbContext.AssetTransactions
                .AsNoTracking()
                .AnyAsync(x => x.UserAsset.UserId == userId, ct);

            if (!hasWatchlistAsset && !hasCompletedAnalysis && !hasTransaction)
            {
                return BuildNewUserGuidance();
            }

            return new OnboardingGuidanceViewModel
            {
                ShouldDisplay = false,
                GuidanceCode = "USER_ACTIVE",
                Title = string.Empty,
                Summary = string.Empty,
                SuggestedSteps = []
            };
        }

        private static OnboardingGuidanceViewModel BuildNewUserGuidance()
        {
            return new OnboardingGuidanceViewModel
            {
                ShouldDisplay = true,
                GuidanceCode = "NEW_USER",
                Title = "Bienvenue sur PredictFinance",
                Summary = "Commencez par ajouter un instrument à votre watchlist, ou lancez une première analyse guidée.",
                SuggestedSteps =
                [
                    new OnboardingStepViewModel { Order = 1, StepCode = "ADD_WATCHLIST", Label = "Ajouter un instrument", RoutePath = "client/watchlist" },
                    new OnboardingStepViewModel { Order = 2, StepCode = "FIRST_ANALYSIS", Label = "Lancer une première analyse", RoutePath = "client/analysis" },
                    new OnboardingStepViewModel { Order = 3, StepCode = "ADD_POSITION", Label = "Enregistrer une position", RoutePath = "client/portfolio" }
                ]
            };
        }
    }
}
