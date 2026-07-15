using BackPredictFinance.API.Controllers;
using BackPredictFinance.API.Controllers.ClientFinance;
using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Patterns;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Contracts;
using BackPredictFinance.Services.AuthServices;
using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.Services.ClientFinanceServices.Alerts;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.Services.ClientFinanceServices.Patterns;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.Services.UserServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Assets;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Dashboard;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.History;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Portfolio;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Simulation;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Snapshots;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Transactions;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Watchlist;
using BackPredictFinance.ViewModels.UserViewModels;
using BackPredictFinance.ViewModels.UserViewModels.AuthViewModels;
using BackPredictFinance.ViewModels.WebViewModels.PaginateViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BackPredictFinance.Tests.Infrastructure;

internal static class TestInfrastructure
{
    public static Mock<IClientFinanceService> CreateClientFinanceServiceMock() => new(MockBehavior.Strict);
    public static Mock<IClientFinanceDashboardHistoryService> CreateDashboardHistoryServiceMock() => new(MockBehavior.Strict);
    public static Mock<IClientFinanceWatchlistPortfolioService> CreateWatchlistPortfolioServiceMock() => new(MockBehavior.Strict);
    public static Mock<IClientFinanceTransactionService> CreateTransactionServiceMock() => new(MockBehavior.Strict);
    public static Mock<IClientFinanceInstrumentDetailService> CreateInstrumentDetailServiceMock() => new(MockBehavior.Strict);
    public static Mock<IClientFinanceContactService> CreateContactServiceMock() => new(MockBehavior.Strict);
    public static Mock<IClientFinanceSnapshotComparisonService> CreateSnapshotComparisonServiceMock() => new(MockBehavior.Strict);
    public static Mock<IAccountService> CreateAccountServiceMock() => new(MockBehavior.Strict);
    public static Mock<ICurrentUserSessionService> CreateCurrentUserSessionServiceMock() => new(MockBehavior.Strict);
    public static Mock<ITickerService> CreateTickerServiceMock() => new(MockBehavior.Strict);
    public static Mock<IUserService> CreateUserServiceMock() => new(MockBehavior.Strict);
    public static Mock<IPortfolioService> CreatePortfolioServiceMock() => new(MockBehavior.Strict);

    public static FinanceDbContext CreateInMemoryFinanceDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var context = new FinanceDbContext(options, accessor.Object);
        context.Database.EnsureCreated(); // applique les données seed (HasData)
        return context;
    }

    public static ClientFinanceAnalysisController CreateClientFinanceAnalysisController(
        Mock<IClientFinanceService>? clientFinanceServiceMock = null,
        Mock<IClientFinanceDashboardHistoryService>? dashboardHistoryServiceMock = null,
        Mock<IClientFinanceSnapshotComparisonService>? snapshotComparisonServiceMock = null,
        Mock<IClientFinanceParameterDetailService>? parameterDetailServiceMock = null,
        Mock<IPatternExplorerService>? patternExplorerServiceMock = null,
        Mock<IExPostStatisticsService>? exPostStatisticsServiceMock = null) => new(
            (clientFinanceServiceMock ?? CreateClientFinanceServiceMock()).Object,
            (dashboardHistoryServiceMock ?? CreateDashboardHistoryServiceMock()).Object,
            (snapshotComparisonServiceMock ?? CreateSnapshotComparisonServiceMock()).Object,
            (parameterDetailServiceMock ?? new Mock<IClientFinanceParameterDetailService>(MockBehavior.Strict)).Object,
            (patternExplorerServiceMock ?? new Mock<IPatternExplorerService>(MockBehavior.Strict)).Object,
            (exPostStatisticsServiceMock ?? new Mock<IExPostStatisticsService>(MockBehavior.Strict)).Object);

    public static ClientFinancePortfolioController CreateClientFinancePortfolioController(
        Mock<IClientFinanceWatchlistPortfolioService>? watchlistPortfolioServiceMock = null,
        Mock<IPortfolioService>? portfolioServiceMock = null,
        Mock<IClientFinanceTransactionService>? transactionServiceMock = null) => new(
            (watchlistPortfolioServiceMock ?? CreateWatchlistPortfolioServiceMock()).Object,
            (portfolioServiceMock ?? CreatePortfolioServiceMock()).Object,
            (transactionServiceMock ?? CreateTransactionServiceMock()).Object);

    public static ClientFinanceMarketController CreateClientFinanceMarketController(
        Mock<IClientFinanceService>? clientFinanceServiceMock = null,
        Mock<IClientFinanceWatchlistPortfolioService>? watchlistPortfolioServiceMock = null,
        Mock<IClientFinanceInstrumentDetailService>? instrumentDetailServiceMock = null,
        Mock<IClientFinanceHistoryReadService>? historyReadServiceMock = null) => new(
            (clientFinanceServiceMock ?? CreateClientFinanceServiceMock()).Object,
            (watchlistPortfolioServiceMock ?? CreateWatchlistPortfolioServiceMock()).Object,
            (instrumentDetailServiceMock ?? CreateInstrumentDetailServiceMock()).Object,
            (historyReadServiceMock ?? new Mock<IClientFinanceHistoryReadService>(MockBehavior.Strict)).Object);

    public static ClientFinanceLearningController CreateClientFinanceLearningController(
        Mock<IClientFinanceLearningService>? learningServiceMock = null,
        Mock<IClientGlossaryService>? clientGlossaryServiceMock = null,
        Mock<IEducationContentService>? educationContentServiceMock = null,
        Mock<IGlossaryTermService>? glossaryTermServiceMock = null,
        Mock<IFaqService>? faqServiceMock = null,
        Mock<ILegalCardService>? legalCardServiceMock = null) => new(
            (learningServiceMock ?? new Mock<IClientFinanceLearningService>(MockBehavior.Strict)).Object,
            (clientGlossaryServiceMock ?? new Mock<IClientGlossaryService>(MockBehavior.Strict)).Object,
            (educationContentServiceMock ?? new Mock<IEducationContentService>(MockBehavior.Strict)).Object,
            (glossaryTermServiceMock ?? new Mock<IGlossaryTermService>(MockBehavior.Strict)).Object,
            (faqServiceMock ?? new Mock<IFaqService>(MockBehavior.Strict)).Object,
            (legalCardServiceMock ?? new Mock<ILegalCardService>(MockBehavior.Strict)).Object);

    public static ClientFinanceContactController CreateClientFinanceContactController(
        Mock<IClientFinanceContactService>? contactServiceMock = null) => new(
            (contactServiceMock ?? CreateContactServiceMock()).Object);

    public static ClientFinanceAlertsController CreateClientFinanceAlertsController(
        Mock<IClientAlertService>? clientAlertServiceMock = null) => new(
            (clientAlertServiceMock ?? new Mock<IClientAlertService>(MockBehavior.Strict)).Object);

    public static AccountController CreateAccountController(
        Mock<IAccountService> mock,
        Mock<ICurrentUserSessionService>? currentUserSessionServiceMock = null,
        Mock<IUserService>? userServiceMock = null,
        Mock<IUserPrivacyService>? userPrivacyServiceMock = null) => new(
            mock.Object,
            (currentUserSessionServiceMock ?? CreateCurrentUserSessionServiceMock()).Object,
            (userServiceMock ?? CreateUserServiceMock()).Object,
            (userPrivacyServiceMock ?? new Mock<IUserPrivacyService>(MockBehavior.Strict)).Object);
    public static TickersController CreateTickersController(Mock<ITickerService> mock) => new(mock.Object);
    public static TradingController CreateTradingController() => new();

    public static T AssertOkObject<T>(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        return Assert.IsType<T>(ok.Value);
    }

    public static ProblemDetails AssertGoneProblem(IActionResult result)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(410, objectResult.StatusCode);
        return Assert.IsType<ProblemDetails>(objectResult.Value);
    }

    public static ClientDashboardViewModel CreateDashboard() => new()
    {
        TotalPortfolioValue = 1000m,
        DayProfitLoss = 25m,
        OpenPositions = 2,
        AnalysesThisWeek = 3,
        WatchlistCount = 4,
        NextMarketOpenAt = new DateTime(2026, 4, 10, 7, 0, 0, DateTimeKind.Utc),
        TotalInvested = 800m,
        TotalOutstanding = 1000m
    };

    public static AnalysisResultViewModel CreateAnalysisResult(string pattern, string phase, RecommendationActionEnum action) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Symbol = "AIR.PA",
        CompanyName = "Airbus",
        Pattern = pattern,
        Phase = phase,
        Probability = 0.81m,
        RecommendationAction = action,
        RecommendationReason = "Pattern confirmed.",
        RiskLevel = RiskLevelEnum.Moderate,
        RecommendationHorizonDays = 20,
        PredictedAt = new DateTime(2026, 4, 9, 8, 0, 0, DateTimeKind.Utc),
        IsActionable = action != RecommendationActionEnum.Hold,
        ModelStatus = ModelStatusEnum.Go,
        ModelMessage = "Ready",
        CurrentPrice = 145m,
        NecklinePrice = 140m,
        TargetPrice = 160m,
        InvalidationPrice = 136m
    };

    public static AnalysisDossierViewModel CreateAnalysisDossier(string pattern, string phase, RecommendationActionEnum action) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Symbol = "AIR.PA",
        CompanyName = "Airbus",
        Outcome = "CrediblePatternFound",
        OutcomeMessage = "Pattern confirmed.",
        GlobalSummary = "Analyse pédagogique.",
        PredictedAt = new DateTime(2026, 4, 9, 8, 0, 0, DateTimeKind.Utc),
        ModelStatus = "Go",
        ModelMessage = "Ready",
        MainPattern = new AnalysisPatternViewModel
        {
            PatternId = pattern,
            DisplayName = pattern,
            PhaseCode = phase,
            PhaseLabel = phase,
            ConfidenceScore = 0.81m,
            IsCompatible = true,
            IsCredible = true,
            CurrentPrice = 145m,
            SuggestedTakeProfit = 160m,
            InvalidationLevel = 136m,
            RecommendationAction = action.ToString(),
            RecommendationReason = "Pattern confirmed.",
            IsActionable = action != RecommendationActionEnum.Hold,
            RiskLevel = "Moderate",
            RecommendationHorizonDays = 20
        }
    };

    public static SimulationResultViewModel CreateSimulationResult(string pattern, RecommendationActionEnum action) => new()
    {
        Symbol = "AIR.PA",
        Pattern = pattern,
        Phase = action == RecommendationActionEnum.Sell ? "bearish_breakout_confirmed" : "bullish_breakout_confirmed",
        InvestmentAmount = 1000m,
        HorizonDays = 20,
        EstimatedReturnAmount = 120m,
        EstimatedReturnPct = 0.12m,
        EstimatedFinalAmount = 1120m,
        Assumption = "Target-based simulation",
        CurrentPrice = 145m,
        Probability = 0.8m,
        RecommendationAction = action,
        RecommendationReason = "Actionable setup.",
        RiskLevel = RiskLevelEnum.Moderate,
        IsActionable = action != RecommendationActionEnum.Hold,
        TargetPrice = 160m,
        InvalidationPrice = 136m
    };

    public static WatchlistItemViewModel CreateWatchlistItem(string symbol) => new()
    {
        UserAssetId = Guid.NewGuid().ToString("N"),
        Instrument = new InstrumentIdentityViewModel
        {
            InstrumentId = Guid.NewGuid().ToString("N"),
            Symbol = symbol,
            DisplayName = symbol,
            AssetType = "Stock",
            Exchange = "PAR",
            Currency = "EUR",
            CountryCode = "FR"
        },
        LastPrice = 123.45m,
        DayVariationPct = 1.23m,
        HeldQuantity = 2m,
        AverageBuyPrice = 110m,
        InvestedAmount = 220m,
        OutstandingAmount = 246.90m,
        HoldingStatus = HoldingStatusEnum.Held,
        MarketReading = new MarketReadingViewModel
        {
            Outcome = TechnicalAnalysisOutcomeTypeEnum.CrediblePatternFound,
            OutcomeDisplayLabel = "Pattern crédible détecté",
            PrimaryPatternId = PatternIds.RectangleContinuation,
            PrimaryPatternDisplayName = "Rectangle continuation",
            ProgressStatus = PatternProgressStatusEnum.Confirmed,
            ConfidenceLabel = "HIGH",
            RecommendationStrength = RecommendationStrengthEnum.High,
            ValidationState = ValidationStateEnum.Validated,
            RiskHint = "Risk hint"
        },
        SupportReading = new SupportReadingViewModel
        {
            AvailabilityStatus = SupportAvailabilityStatusEnum.Unavailable,
            AvailabilityDisplayLabel = "Lecture support indisponible",
            PeaEligibilityStatus = PeaEligibilityStatusEnum.Unknown,
            PeaDisplayLabel = "Éligibilité PEA non confirmée"
        },
        Recommendation = new RecommendationSummaryViewModel
        {
            Kind = RecommendationKind.Reinforce,
            HoldingStatus = HoldingStatusEnum.Held,
            DisplayLabel = "Renforcer · fort",
            ExplanationSummary = "Recommendation summary"
        },
        LastAnalysisAtUtc = new DateTime(2026, 4, 9, 9, 0, 0, DateTimeKind.Utc),
        Freshness = new FreshnessViewModel
        {
            Status = FreshnessStatusEnum.Fresh,
            CheckedAtUtc = new DateTime(2026, 4, 9, 9, 0, 0, DateTimeKind.Utc),
            DisplayLabel = "Données à jour"
        }
    };

    public static PortfolioViewModel CreatePortfolio() => new()
    {
        Positions = [new PortfolioPositionViewModel
        {
            UserAssetId = Guid.NewGuid().ToString("N"),
            Instrument = CreateWatchlistItem("AIR.PA").Instrument,
            QuantityHeld = 2m,
            AverageCost = 110m,
            Fees = 2m,
            OutstandingAmount = 246.90m,
            MarketReading = CreateWatchlistItem("AIR.PA").MarketReading,
            SupportReading = CreateWatchlistItem("AIR.PA").SupportReading,
            Recommendation = CreateWatchlistItem("AIR.PA").Recommendation,
            RiskHint = "Risk hint",
            HistoryEntryUrl = "/api/ClientFinance/instruments/AIR.PA/analysis-history",
            SimulationUrl = "/api/ClientFinance/simulation/run"
        }],
        TotalInvestedAmount = 220m,
        TotalOutstandingAmount = 246.90m,
        OpenPositionCount = 1
    };

    public static AnalysisDetailViewModel CreateAnalysisDetail() => new()
    {
        AnalysisId = Guid.NewGuid().ToString("N"),
        Instrument = CreateWatchlistItem("AIR.PA").Instrument,
        GeneratedAtUtc = new DateTime(2026, 4, 9, 8, 0, 0, DateTimeKind.Utc),
        Outcome = TechnicalAnalysisOutcomeTypeEnum.CrediblePatternFound,
        OutcomeDisplayLabel = "Pattern crédible détecté",
        MarketReading = CreateWatchlistItem("AIR.PA").MarketReading,
        ConfidenceBreakdown = new ConfidenceBreakdownViewModel
        {
            Level = "HIGH",
            Criteria =
            [
                new ConfidenceCriterionViewModel
                {
                    Code = "STRUCTURE_COMPATIBLE",
                    Label = "Structure du pattern compatible",
                    State = "met",
                    Source = "DETECTION"
                }
            ]
        },
        SupportReading = CreateWatchlistItem("AIR.PA").SupportReading,
        Recommendation = CreateWatchlistItem("AIR.PA").Recommendation,
        ActionPlan = new ActionPlanViewModel
        {
            HoldingStatus = HoldingStatusEnum.Held,
            PolicyVersion = "accompaniment-fr@v1",
            Steps =
            [
                new ActionPlanStepViewModel
                {
                    Kind = "NOTE_LEVEL",
                    Label = "Noter le niveau d'invalidation",
                    Source = "riskHints.invalidationPrice",
                    Value = "136,00 EUR"
                }
            ]
        },
        WhyRecommendation = "Because the pattern is confirmed.",
        PedagogicalSummary = "Short pedagogical summary",
        SnapshotId = Guid.NewGuid().ToString("N"),
        HistoryRoute = "/api/ClientFinance/instruments/AIR.PA/analysis-history",
        CompactSummary = "Rectangle continuation · Renforcer · fort",
        ModelMessage = "Ready"
    };

    public static HistoryFeedViewModel CreateHistoryFeed() => new()
    {
        Items =
        [
            CreateHistoryItem("AIR.PA"),
            CreateHistoryItem("MC.PA")
        ],
        ReturnedCount = 2
    };

    public static HistoryItemViewModel CreateHistoryItem(string symbol) => new()
    {
        AnalysisId = Guid.NewGuid().ToString("N"),
        SnapshotId = Guid.NewGuid().ToString("N"),
        Instrument = CreateWatchlistItem(symbol).Instrument,
        TimestampUtc = new DateTime(2026, 4, 9, 8, 0, 0, DateTimeKind.Utc),
        Outcome = TechnicalAnalysisOutcomeTypeEnum.CrediblePatternFound,
        OutcomeDisplayLabel = "Pattern credible detecte",
        PrimaryPatternLabel = "Rectangle continuation",
        RecommendationSummary = "Renforcer - fort",
        SupportAvailabilitySummary = "Lecture support non persistee dans le snapshot V1",
        PeaEligibilityStatus = PeaEligibilityStatusEnum.Unknown,
        PeaSummary = "Eligibilite PEA non confirmee",
        AnalysisEngineVersion = "engine@v1",
        RecommendationPolicyVersion = "policy@v1",
        ExplanationPolicyVersion = "explanation@v1",
        DetailUrl = $"/api/ClientFinance/analysis/{Guid.NewGuid():N}",
        HistoryUrl = $"/api/ClientFinance/instruments/{symbol}/analysis-history",
        ComparisonUrl = "/api/ClientFinance/snapshots/compare"
    };

    public static InstrumentDetailViewModel CreateInstrumentDetail(string symbol = "AIR.PA") => new()
    {
        Symbol = symbol,
        InstrumentSummary = new InstrumentSummaryViewModel
        {
            Instrument = CreateWatchlistItem(symbol).Instrument,
            PerimeterLabel = "PEA_FR_EQUITIES / DAILY",
            PeaEligibilityStatus = PeaEligibilityStatusEnum.Unknown,
            PeaDisplayLabel = "Eligibilite PEA non confirmee",
            Freshness = new FreshnessViewModel
            {
                Status = FreshnessStatusEnum.Fresh,
                CheckedAtUtc = new DateTime(2026, 4, 9, 9, 0, 0, DateTimeKind.Utc),
                DisplayLabel = "Donnees a jour"
            },
            HasPersistedAnalysis = true,
            AnalysisAvailabilityLabel = "Analyse persistee disponible",
            LatestAnalysisId = "analysis-1",
            LatestSnapshotId = "snapshot-1"
        },
        MarketReading = new MarketReadingViewModel
        {
            Outcome = TechnicalAnalysisOutcomeTypeEnum.CrediblePatternFound,
            OutcomeDisplayLabel = "Pattern credible detecte",
            PrimaryPatternId = PatternIds.RectangleContinuation,
            PrimaryPatternDisplayName = "Rectangle continuation",
            ProgressStatus = PatternProgressStatusEnum.Confirmed,
            ConfidenceLabel = "HIGH",
            RecommendationStrength = RecommendationStrengthEnum.High,
            ValidationState = ValidationStateEnum.Validated,
            ValidationSummary = "Le pattern est valide selon les regles persistantes du snapshot.",
            InvalidationLevel = 136m,
            RiskHint = "Risk hint",
            PedagogicalSummary = "Short pedagogical summary"
        },
        SupportReading = new SupportReadingViewModel
        {
            AvailabilityStatus = SupportAvailabilityStatusEnum.Partial,
            AvailabilityDisplayLabel = "Lecture support partielle",
            ActiveUniverseId = "PEA_FR_EQUITIES",
            PeaEligibilityStatus = PeaEligibilityStatusEnum.Unknown,
            PeaDisplayLabel = "Eligibilite PEA non confirmee",
            Notes = ["Le statut PEA provient du registre persiste."]
        },
        PersonalSituation = new PersonalSituationReadingViewModel
        {
            HoldingStatus = HoldingStatusEnum.Held,
            HoldsInstrument = true,
            TotalQuantityHeld = 2m,
            AverageUnitCost = 110m,
            OpenLineCount = 1,
            CurrencyCode = "EUR",
            Recommendation = CreateWatchlistItem(symbol).Recommendation,
            GuidanceSummary = "La recommandation contextuelle s applique a une position deja detenue: Renforcer - fort."
        },
        NavigationLinks = new InstrumentNavigationLinksViewModel
        {
            HistoryUrl = $"/api/ClientFinance/instruments/{symbol}/analysis-history",
            SimulationUrl = "/api/ClientFinance/simulation/run",
            ComparisonUrl = "/api/ClientFinance/snapshots/compare"
        },
        LatestAnalysisId = "analysis-1",
        LatestSnapshotId = "snapshot-1"
    };

    public static InstrumentHistoryViewModel CreateInstrumentHistory(string symbol = "AIR.PA") => new()
    {
        Instrument = CreateWatchlistItem(symbol).Instrument,
        Symbol = symbol,
        Items =
        [
            new HistoryItemViewModel
            {
                AnalysisId = "analysis-1",
                SnapshotId = "snapshot-1",
                TimestampUtc = new DateTime(2026, 4, 9, 8, 0, 0, DateTimeKind.Utc),
                Outcome = TechnicalAnalysisOutcomeTypeEnum.CrediblePatternFound,
                OutcomeDisplayLabel = "Pattern credible detecte",
                PrimaryPatternLabel = "Rectangle continuation",
                RecommendationSummary = "Renforcer - fort",
                SupportAvailabilitySummary = "Lecture support non persistee dans le snapshot V1",
                PeaEligibilityStatus = PeaEligibilityStatusEnum.Unknown,
                PeaSummary = "Eligibilite PEA non confirmee",
                AnalysisEngineVersion = "engine@v1",
                RecommendationPolicyVersion = "policy@v1",
                ExplanationPolicyVersion = "explanation@v1",
                DetailUrl = "/api/ClientFinance/analysis/analysis-1",
                ComparisonUrl = "/api/ClientFinance/snapshots/compare"
            }
        ],
        ReturnedCount = 1
    };

    public static SnapshotComparisonViewModel CreateSnapshotComparison() => new()
    {
        Left = CreateHistoryItem("AIR.PA"),
        Right = CreateHistoryItem("AIR.PA"),
        MarketChanges =
        [
            new SnapshotDeltaItemViewModel
            {
                FieldCode = "primary_pattern",
                DisplayLabel = "Pattern principal",
                LeftValue = "Rectangle continuation",
                RightValue = "Bull flag continuation",
                ChangeKind = "changed",
                EvidenceType = "source_fact"
            }
        ],
        SupportChanges =
        [
            new SnapshotDeltaItemViewModel
            {
                FieldCode = "support_snapshot_availability",
                DisplayLabel = "Lecture support",
                LeftValue = "Non persistee",
                RightValue = "Non persistee",
                ChangeKind = "limited",
                EvidenceType = "source_fact"
            }
        ],
        RecommendationChanges =
        [
            new SnapshotDeltaItemViewModel
            {
                FieldCode = "recommendation_kind",
                DisplayLabel = "Action recommandee",
                LeftValue = "Buy",
                RightValue = "Hold",
                ChangeKind = "changed",
                EvidenceType = "derived_consequence"
            }
        ],
        NonComparabilityReasons = ["La lecture support n est pas persistee dans les snapshots V1 actuels et ne peut pas etre comparee de maniere fiable."]
    };

    public static LiveQuoteViewModel CreateLiveQuote(string symbol) => new()
    {
        Symbol = symbol,
        AssetType = "Stock",
        CompanyName = symbol,
        LastPrice = 123.45m,
        DayVariationPct = 0.85m,
        AsOfUtc = new DateTime(2026, 4, 9, 9, 0, 0, DateTimeKind.Utc)
    };

    public static TransactionItemViewModel CreateTransaction(string symbol, string transactionType) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Symbol = symbol,
        CompanyName = symbol,
        TransactionType = transactionType,
        Quantity = 5m,
        UnitPrice = 100m,
        Fees = 1m,
        GrossAmount = 500m,
        NetAmount = transactionType == "Buy" ? 501m : 499m,
        TimestampUtc = new DateTime(2026, 4, 9, 10, 0, 0, DateTimeKind.Utc)
    };

    public static TokenViewModel CreateToken() => new()
    {
        Token = "access-token",
        RefreshToken = "refresh-token",
        RefreshTokenExpiresAtUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
        FirstName = "Greg",
        LastName = "D",
        IsFirstConnection = false
    };

    public static IdentityResult CreateIdentityFailure(string description)
        => IdentityResult.Failed(new IdentityError { Description = description });

    public static IdentityResult CreateIdentitySuccess() => IdentityResult.Success;
}

internal sealed class FakePatternDefinition : IAnalysisPatternDefinition
{
    private readonly string _patternId;
    private readonly int _lookbackMonths;

    public FakePatternDefinition(string patternId, int lookbackMonths = 6)
    {
        _patternId = patternId;
        _lookbackMonths = lookbackMonths;
    }

    public string PatternId => _patternId;
    public string ModelVersion => $"{_patternId.ToLowerInvariant()}@v1";
    public int HistoryLookbackMonths => _lookbackMonths;

    public ResolvedAnalysisPattern BuildResolvedPattern()
    {
        return new ResolvedAnalysisPattern
        {
            PatternId = _patternId,
            ModelVersion = ModelVersion,
            HistoryLookbackMonths = _lookbackMonths
        };
    }

    public Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(new AnalysisExecutionArtifact
        {
            Symbol = request.Instrument.Symbol,
            GeneratedAtUtc = DateTime.UtcNow,
            ModelStatus = ModelStatusEnum.Go,
            ModelMessage = "fake"
        });
    }
}
