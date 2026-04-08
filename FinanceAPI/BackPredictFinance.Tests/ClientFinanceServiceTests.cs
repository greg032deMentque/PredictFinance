using AutoMapper;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Common.enums;
using BackPredictFinance.API.Controllers;
using BackPredictFinance.Services;
using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;
using System.Text.Json;
using System.Security.Claims;
using BackPredictFinance.Contracts.MarketData;
using BackPredictFinance.Contracts.Analysis;
using BackPredictFinance.Contracts.Trading;
using BackPredictFinance.Common.Common;

namespace BackPredictFinance.Tests
{
    public class ClientFinanceServiceTests
    {
        [Fact]
        public async Task AnalysisOrchestrator_RunAnalysisAsync_PersistsFailedAnalysisRun_WhenExecutionFails()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            var httpContextAccessor = BuildHttpContextAccessor();
            services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
            services.AddDbContext<FinanceDbContext>(options =>
                options.UseInMemoryDatabase($"PredictFinanceTests_{Guid.NewGuid()}"));

            var mapperConfiguration = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(typeof(AnalysisResultViewModelProfile).Assembly);
            }, NullLoggerFactory.Instance);
            services.AddSingleton<IMapper>(mapperConfiguration.CreateMapper());

            services.AddSingleton(Mock.Of<IStringLocalizer<Messages>>());
            services.AddSingleton(CreateUserManagerMock().Object);
            services.AddSingleton(CreateRoleManagerMock().Object);
            services.AddSingleton(sp => CreateSignInManagerMock(sp.GetRequiredService<UserManager<User>>(), httpContextAccessor).Object);
            services.AddSingleton(Mock.Of<ILogService>());

            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var scopedProvider = scope.ServiceProvider;
            var dbContext = scopedProvider.GetRequiredService<FinanceDbContext>();

            var executionException = new CustomException(
                "Deterministic execution failed.",
                "Le moteur d'analyse est indisponible pour le moment.");

            var executionService = new Mock<IAnalysisExecutionService>();
            executionService
                .Setup(service => service.ExecuteAsync(It.IsAny<AnalysisRequest>(), It.IsAny<IReadOnlyList<ResolvedAnalysisPattern>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(executionException);
            var riskEvaluationService = new Mock<IRiskEvaluationService>();
            var recommendationPolicyService = new Mock<IRecommendationPolicyService>();
            var pedagogicalExplanationService = new Mock<IPedagogicalExplanationService>();
            var patternRegistry = new Mock<IAnalysisPatternRegistry>();
            patternRegistry
                .Setup(service => service.ResolveRequestedPattern(It.IsAny<string?>()))
                .Returns(new ResolvedAnalysisPattern
                {
                    PatternId = "DOUBLE_TOP",
                    ModelDir = "artifacts/double_top",
                    ModelVersion = "double_top@v1"
                });

            var persistenceService = new AnalysisSnapshotPersistenceService(scopedProvider);
            SetAnalysisHistoryAvailable(persistenceService, true);
            var service = new ClientAnalysisOrchestrator(
                patternRegistry.Object,
                executionService.Object,
                riskEvaluationService.Object,
                recommendationPolicyService.Object,
                pedagogicalExplanationService.Object,
                persistenceService);

            var exception = await Assert.ThrowsAsync<CustomException>(() => service.RunAnalysisAsync(
                new AnalysisRequest
                {
                    InstrumentId = "asset-1",
                    UserId = "user-1",
                    Instrument = new Instrument
                    {
                        InstrumentId = "asset-1",
                        Symbol = "AAPL",
                        DisplayName = "AAPL",
                        AssetType = "EQUITY",
                        CurrencyCode = "USD"
                    },
                    PortfolioContext = new PortfolioContext
                    {
                        UserId = "user-1",
                        InstrumentId = "asset-1",
                        HoldsInstrument = false,
                        CurrencyCode = "USD"
                    },
                    RequestedPatternIds = ["DOUBLE_TOP"],
                    ResolvedPatternIds = ["DOUBLE_TOP"],
                    HistoryStartDate = new DateOnly(2025, 1, 1),
                    HistoryEndDate = new DateOnly(2025, 1, 31)
                }));

            Assert.Equal("Le moteur d'analyse est indisponible pour le moment.", exception.FrontMessage);

            var analysisRun = await dbContext.AnalysisRuns.SingleAsync();
            Assert.Equal("Failed", analysisRun.Status);
            Assert.Equal("Le moteur d'analyse est indisponible pour le moment.", analysisRun.ErrorMessage);
            Assert.Contains("\"error_code\":\"artifact_missing\"", analysisRun.RawPayload, StringComparison.Ordinal);
            Assert.Equal(0, await dbContext.DecisionSignals.CountAsync());
            Assert.Equal(0, await dbContext.ModelSnapshots.CountAsync());
            Assert.Equal(0, await dbContext.PatternAssessments.CountAsync());
        }

        [Fact]
        public async Task PortfolioContextLoader_TryLoadAsync_ReconstructsOpenLinesWithFifo()
        {
            var options = new DbContextOptionsBuilder<FinanceDbContext>()
                .UseInMemoryDatabase($"PredictFinancePortfolioContext_{Guid.NewGuid()}")
                .Options;

            await using var dbContext = new FinanceDbContext(options, new HttpContextAccessor());
            var asset = new Asset
            {
                Id = "asset-1",
                Symbol = "AIR.PA",
                ProviderSymbol = "AIR.PA",
                Name = "Airbus",
                Currency = "EUR",
                Exchange = "PAR",
                Country = "FR",
                AssetType = AssetTypeEnum.Stock
            };

            var userAsset = new UserAsset
            {
                Id = "ua-1",
                UserId = "user-1",
                AssetId = asset.Id,
                Quantity = 4m,
                Asset = asset
            };

            dbContext.Assets.Add(asset);
            dbContext.UserAssets.Add(userAsset);
            dbContext.AssetTransactions.AddRange(
                new AssetTransaction
                {
                    Id = "tx-1",
                    UserAssetId = userAsset.Id,
                    TimestampUtc = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                    TransactionType = TransactionTypeEnum.Buy,
                    Quantity = 5m,
                    UnitPrice = 10m,
                    Fees = 1m
                },
                new AssetTransaction
                {
                    Id = "tx-2",
                    UserAssetId = userAsset.Id,
                    TimestampUtc = new DateTime(2025, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                    TransactionType = TransactionTypeEnum.Buy,
                    Quantity = 3m,
                    UnitPrice = 20m,
                    Fees = 2m
                },
                new AssetTransaction
                {
                    Id = "tx-3",
                    UserAssetId = userAsset.Id,
                    TimestampUtc = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                    TransactionType = TransactionTypeEnum.Sell,
                    Quantity = 4m,
                    UnitPrice = 18m,
                    Fees = 0.5m
                });

            await dbContext.SaveChangesAsync();

            var service = new PortfolioContextLoader(dbContext);
            var context = await service.TryLoadAsync("user-1", asset.Id, new DateOnly(2025, 1, 31));

            Assert.NotNull(context);
            Assert.True(context!.HoldsInstrument);
            Assert.Equal(2, context.OpenLineCount);
            Assert.Equal(4m, context.TotalQuantityHeld);
            Assert.Equal(18.05m, context.AverageUnitCost);
            Assert.Equal(new DateOnly(2025, 1, 1), context.OldestOpenBuyDate);
            Assert.Equal(new DateOnly(2025, 1, 10), context.LatestOpenBuyDate);
            Assert.Equal("EUR", context.CurrencyCode);
            Assert.Collection(
                context.OpenLines.OrderBy(x => x.BuyDate),
                first =>
                {
                    Assert.Equal(1m, first.Quantity);
                    Assert.Equal(10m, first.UnitBuyPrice);
                    Assert.Equal(0.2m, first.FeesAmount);
                },
                second =>
                {
                    Assert.Equal(3m, second.Quantity);
                    Assert.Equal(20m, second.UnitBuyPrice);
                    Assert.Equal(2m, second.FeesAmount);
                });
        }



        [Fact]
        public async Task PortfolioContextLoader_TryLoadAsync_ThrowsWhenReconstructedQuantityDoesNotMatchUserAssetQuantity()
        {
            var options = new DbContextOptionsBuilder<FinanceDbContext>()
                .UseInMemoryDatabase($"PredictFinancePortfolioMismatch_{Guid.NewGuid()}")
                .Options;

            await using var dbContext = new FinanceDbContext(options, new HttpContextAccessor());
            var asset = new Asset
            {
                Id = "asset-1",
                Symbol = "AIR.PA",
                ProviderSymbol = "AIR.PA",
                Name = "Airbus",
                Currency = "EUR",
                Exchange = "PAR",
                Country = "FR",
                AssetType = AssetTypeEnum.Stock
            };

            var userAsset = new UserAsset
            {
                Id = "ua-1",
                UserId = "user-1",
                AssetId = asset.Id,
                Quantity = 5m,
                Asset = asset
            };

            dbContext.Assets.Add(asset);
            dbContext.UserAssets.Add(userAsset);
            dbContext.AssetTransactions.AddRange(
                new AssetTransaction
                {
                    Id = "tx-1",
                    UserAssetId = userAsset.Id,
                    TimestampUtc = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                    TransactionType = TransactionTypeEnum.Buy,
                    Quantity = 5m,
                    UnitPrice = 10m,
                    Fees = 1m
                },
                new AssetTransaction
                {
                    Id = "tx-2",
                    UserAssetId = userAsset.Id,
                    TimestampUtc = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                    TransactionType = TransactionTypeEnum.Sell,
                    Quantity = 1m,
                    UnitPrice = 18m,
                    Fees = 0.5m
                });

            await dbContext.SaveChangesAsync();

            var service = new PortfolioContextLoader(dbContext);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.TryLoadAsync("user-1", asset.Id, new DateOnly(2025, 1, 31)));

            Assert.Equal("Le contexte portefeuille FIFO est incoherent: la quantite reconstruite ne correspond pas a la quantite agregee persistee.", exception.Message);
        }

        [Fact]
        public async Task AnalysisSnapshotPersistenceService_PersistSuccessfulAnalysisAsync_StoresVersionedSnapshotPayload()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            var httpContextAccessor = BuildHttpContextAccessor();
            services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
            services.AddDbContext<FinanceDbContext>(options =>
                options.UseInMemoryDatabase($"PredictFinanceSnapshotPersistence_{Guid.NewGuid()}"));

            var mapperConfiguration = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(typeof(AnalysisResultViewModelProfile).Assembly);
            }, NullLoggerFactory.Instance);
            services.AddSingleton<IMapper>(mapperConfiguration.CreateMapper());

            services.AddSingleton(Mock.Of<IStringLocalizer<Messages>>());
            services.AddSingleton(CreateUserManagerMock().Object);
            services.AddSingleton(CreateRoleManagerMock().Object);
            services.AddSingleton(sp => CreateSignInManagerMock(sp.GetRequiredService<UserManager<User>>(), httpContextAccessor).Object);
            services.AddSingleton(Mock.Of<ILogService>());

            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var scopedProvider = scope.ServiceProvider;
            var dbContext = scopedProvider.GetRequiredService<FinanceDbContext>();

            var service = new AnalysisSnapshotPersistenceService(scopedProvider);
            SetAnalysisHistoryAvailable(service, true);

            var request = new AnalysisRequest
            {
                InstrumentId = "asset-1",
                RequestedPatternIds = ["DOUBLE_TOP"],
                AsOfDate = new DateOnly(2025, 1, 31),
                UserId = "user-1",
                Instrument = new Instrument
                {
                    InstrumentId = "asset-1",
                    Symbol = "AIR.PA",
                    ProviderSymbol = "AIR.PA",
                    DisplayName = "Airbus",
                    MarketCode = "PAR",
                    CountryCode = "FR",
                    CurrencyCode = "EUR",
                    AssetType = "EQUITY",
                    IsActive = true,
                    Summary = "Airbus SE"
                },
                PortfolioContext = new PortfolioContext
                {
                    UserId = "user-1",
                    InstrumentId = "asset-1",
                    HoldsInstrument = true,
                    OpenLineCount = 1,
                    TotalQuantityHeld = 4m,
                    AverageUnitCost = 122.75m,
                    CurrencyCode = "EUR",
                    OpenLines =
                    [
                        new PortfolioContextLine
                        {
                            Quantity = 4m,
                            UnitBuyPrice = 122m,
                            BuyDate = new DateOnly(2025, 1, 10),
                            FeesAmount = 3m,
                            CurrencyCode = "EUR"
                        }
                    ],
                    OldestOpenBuyDate = new DateOnly(2025, 1, 10),
                    LatestOpenBuyDate = new DateOnly(2025, 1, 10)
                },
                CandleInterval = "1d",
                AnalysisMode = "on_demand",
                ResolvedPatternIds = ["DOUBLE_TOP"],
                HistoryStartDate = new DateOnly(2024, 12, 1),
                HistoryEndDate = new DateOnly(2025, 1, 31)
            };

            var assessment = new BackPredictFinance.Contracts.Analysis.PatternAssessment
            {
                AssessmentId = "assessment-1",
                PatternId = "DOUBLE_TOP",
                DisplayName = "Double sommet",
                PedagogicalDescription = "Schema de retournement baissier avec deux sommets proches.",
                AnalysisWindow = new PatternAnalysisWindow
                {
                    Interval = "1d",
                    StartDate = new DateOnly(2024, 12, 1),
                    EndDate = new DateOnly(2025, 1, 31),
                    RequiredCandles = 60,
                    ActualCandles = 60
                },
                Detection = new PatternDetection
                {
                    IsCompatible = true,
                    Status = PatternStatusEnum.Confirmed,
                    CurrentPhaseCode = "CONFIRMED",
                    CurrentPhaseLabel = "Confirme",
                    StatusReason = "La structure reste confirmee autour de 130.",
                    CurrentPrice = 130m,
                    StructuralPoints = []
                },
                Validation = new PatternValidation
                {
                    State = "VALIDATED",
                    Reason = "La structure a atteint un etat confirme."
                },
                Invalidation = new PatternInvalidation
                {
                    State = "ACTIVE",
                    Reason = "Le scenario reste actif a ce stade.",
                    InvalidationLevel = 118m
                },
                Scoring = new PatternScoring
                {
                    ConfidenceScore = 0.78m,
                    ConfidenceLabel = "HIGH",
                    IsCredible = true,
                    ScoreReasons = ["Confiance calculee a 78.00%."],
                    ScoreVersion = "double_top@v1"
                },
                RiskHints = new PatternRiskHints
                {
                    HasRiskPlan = true,
                    SuggestedStopLoss = 118m,
                    SuggestedTakeProfit = 145m,
                    RiskRewardRatio = 1.25m,
                    PositioningNote = "Le plan de risque reste exploitable."
                },
                Explanation = new PatternExplanation
                {
                    WhyListed = "Le pattern est conserve car son etat confirme reste compatible avec les regles V1.",
                    PedagogicalSummary = "Double sommet en phase confirme avec une confiance elevee.",
                    AmbiguityNote = null,
                    LimitationsNote = null
                },
                Trace = new PatternTrace
                {
                    PatternVersion = "double_top@v1",
                    RuleSetVersion = "double_top@v1",
                    IsPrimaryDisplayCandidate = true,
                    ScoringVersion = "double_top@v1"
                }
            };

            var executionArtifact = new AnalysisExecutionArtifact
            {
                Symbol = "AIR.PA",
                GeneratedAtUtc = new DateTime(2025, 1, 31, 18, 0, 0, DateTimeKind.Utc),
                Patterns =
                [
                    new ExecutedPatternArtifact
                    {
                        Pattern = TradingPatternEnum.DoubleTop,
                        Phase = "CONFIRMED",
                        Probability = 0.78m,
                        Confidence = 0.78m,
                        CurrentPrice = 130m,
                        NecklinePrice = 125m,
                        TargetPrice = 145m,
                        InvalidationPrice = 118m,
                        IsPrimary = true,
                        ContractAssessment = assessment
                    }
                ],
                ModelStatus = ModelStatusEnum.Go,
                ModelMessage = "Model quality gate passed.",
                ModelVersion = "double_top@v1",
                Precision = 0.81m,
                F1 = 0.66m,
                RocAuc = 0.79m,
                PositiveSamples = 18,
                SelectedThreshold = 0.30m,
                RawProviderPayloadJson = "{\"provider\":\"yfinance\"}"
            };

            var recommendation = new AnalysisRecommendation
            {
                RecommendationId = "rec-1",
                SituationCode = RecommendationSituationCode.CompatiblePatternConfirmed,
                AdviceScenarioCode = AdviceScenarioCode.PrimaryPatternHeldNeutral,
                RecommendationAction = RecommendationActionEnum.Hold,
                RecommendationStrength = RecommendationStrengthEnum.Moderate,
                Parameters = new AnalysisRecommendationParameters
                {
                    HoldsInstrument = true,
                    IsActionable = false,
                    PrimaryPatternId = "DOUBLE_TOP",
                    PrimaryPatternDisplayName = "Double sommet",
                    CurrentPhaseCode = "CONFIRMED",
                    CurrentPhaseLabel = "Confirme",
                    ValidationState = "VALIDATED",
                    InvalidationState = "ACTIVE",
                    ConfidenceScore = 0.78m,
                    ConfidenceLabel = "HIGH",
                    SuggestedTakeProfit = 145m,
                    InvalidationLevel = 118m
                },
                HoldingContext = "HELD",
                Rationale = "La posture retenue est HOLD.",
                BasedOnPatternIds = ["DOUBLE_TOP"],
                ReviewHorizonDays = 20,
                PolicyVersion = "analysis-v1-policy@prompt8"
            };

            var startedAtUtc = new DateTime(2025, 1, 31, 18, 0, 0, DateTimeKind.Utc);
            var completedAtUtc = new DateTime(2025, 1, 31, 18, 1, 0, DateTimeKind.Utc);

            var persisted = await service.PersistSuccessfulAnalysisAsync(
                request,
                new ResolvedAnalysisPattern
                {
                    PatternId = "DOUBLE_TOP",
                    ModelDir = "artifacts/double_top",
                    ModelVersion = "double_top@v1"
                },
                executionArtifact,
                recommendation,
                AnalysisOutcomeEnum.CrediblePatternFound,
                "Vous detenez deja cette valeur. Le scenario principal retenu est double sommet, actuellement confirme. La recommandation HOLD reste alignee sur cette lecture.",
                "analysis-v1-explanation@prompt5",
                startedAtUtc,
                completedAtUtc);

            var analysisRun = await dbContext.AnalysisRuns.SingleAsync();
            using var document = JsonDocument.Parse(analysisRun.RawPayload);
            var root = document.RootElement;

            Assert.Equal(persisted.PublicId, analysisRun.Id);
            Assert.Equal(analysisRun.Id, root.GetProperty("snapshotId").GetString());
            Assert.Equal("analysis-snapshot-history@prompt5", root.GetProperty("schemaVersion").GetString());
            Assert.Equal("user-1", root.GetProperty("userId").GetString());
            Assert.Equal(persisted.InstrumentId, root.GetProperty("instrumentId").GetString());
            Assert.Equal("AIR.PA", root.GetProperty("instrumentSnapshot").GetProperty("symbol").GetString());
            Assert.Equal("PAR", root.GetProperty("instrumentSnapshot").GetProperty("marketCode").GetString());
            Assert.Equal("DOUBLE_TOP", root.GetProperty("requestedPatternIds")[0].GetString());
            Assert.Equal("DOUBLE_TOP", root.GetProperty("executedPatternIds")[0].GetString());
            Assert.Equal("CrediblePatternFound", root.GetProperty("outcome").GetString());
            Assert.Equal("YAHOO_FINANCE", root.GetProperty("marketDataProviderCode").GetString());
            Assert.Equal("2024-12-01", root.GetProperty("marketDataRangeStart").GetString());
            Assert.Equal("2025-01-31", root.GetProperty("marketDataRangeEnd").GetString());
            Assert.True(root.GetProperty("portfolioContextSnapshot").GetProperty("holdsInstrument").GetBoolean());
            Assert.Equal(4m, root.GetProperty("portfolioContextSnapshot").GetProperty("totalQuantityHeld").GetDecimal());
            Assert.Equal(122.75m, root.GetProperty("portfolioContextSnapshot").GetProperty("averageUnitCost").GetDecimal());
            Assert.Equal(1, root.GetProperty("portfolioContextUsed").GetProperty("openLines").GetArrayLength());
            Assert.Equal("DOUBLE_TOP", root.GetProperty("primaryPatternId").GetString());
            Assert.Equal("rec-1", root.GetProperty("recommendationId").GetString());
            Assert.Equal("COMPATIBLE_PATTERN_CONFIRMED", root.GetProperty("recommendation").GetProperty("recommendationPayload").GetProperty("situationCode").GetString());
            Assert.Equal("PRIMARY_PATTERN_HELD_NEUTRAL", root.GetProperty("recommendation").GetProperty("recommendationPayload").GetProperty("adviceScenarioCode").GetString());
            Assert.Equal("Hold", root.GetProperty("recommendation").GetProperty("recommendationPayload").GetProperty("recommendationAction").GetString());
            Assert.Equal("double_top@v1", root.GetProperty("analysisEngineVersion").GetString());
            Assert.Equal("analysis-v1-policy@prompt8", root.GetProperty("recommendationPolicyVersion").GetString());
            Assert.Equal("analysis-v1-explanation@prompt5", root.GetProperty("explanationPolicyVersion").GetString());
            Assert.Equal("Vous detenez deja cette valeur. Le scenario principal retenu est double sommet, actuellement confirme. La recommandation HOLD reste alignee sur cette lecture.", root.GetProperty("pedagogicalSummary").GetString());
            Assert.Equal("DOUBLE_TOP", root.GetProperty("patternRows")[0].GetProperty("patternId").GetString());
            Assert.Equal("Double sommet en phase confirme avec une confiance elevee.", root.GetProperty("patternRows")[0].GetProperty("patternAssessmentPayload").GetProperty("explanation").GetProperty("pedagogicalSummary").GetString());
            Assert.Equal("analysis-v1-policy@prompt3", root.GetProperty("recommendation").GetProperty("recommendationPayload").GetProperty("policyVersion").GetString());
            Assert.Equal("Go", root.GetProperty("modelSnapshot").GetProperty("modelStatus").GetString());
            Assert.Equal("{\"provider\":\"yfinance\"}", root.GetProperty("rawProviderPayloadJson").GetString());
        }

        [Fact]
        public async Task TickerService_SearchAssetsAsync_ReturnsOnlyFrenchListedEquities()
        {
            var catalogProvider = new Mock<IMarketCatalogProvider>();
            catalogProvider
                .Setup(service => service.SearchAssetsAsync("AIR", It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    new MarketAssetDescriptor
                    {
                        Symbol = "AIR.PA",
                        ProviderSymbol = "AIR.PA",
                        CompanyName = "Airbus",
                        Exchange = "PAR",
                        Currency = "EUR",
                        AssetType = AssetTypeEnum.Stock,
                        LastPrice = 145m,
                        DayVariationPct = 1.5m
                    },
                    new MarketAssetDescriptor
                    {
                        Symbol = "AAPL",
                        ProviderSymbol = "AAPL",
                        CompanyName = "Apple",
                        Exchange = "NASDAQ",
                        Currency = "USD",
                        AssetType = AssetTypeEnum.Stock,
                        LastPrice = 200m,
                        DayVariationPct = 0.4m
                    }
                ]);
            catalogProvider
                .Setup(service => service.GetAssetProfileAsync("AIR.PA", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MarketAssetProfileData
                {
                    Symbol = "AIR.PA",
                    ProviderSymbol = "AIR.PA",
                    CompanyName = "Airbus",
                    AssetType = AssetTypeEnum.Stock,
                    Exchange = "PAR",
                    Currency = "EUR",
                    Country = "France",
                    LastPrice = 145m,
                    DayVariationPct = 1.5m,
                    AsOfUtc = DateTime.UtcNow
                });
            catalogProvider
                .Setup(service => service.GetAssetProfileAsync("AAPL", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MarketAssetProfileData
                {
                    Symbol = "AAPL",
                    ProviderSymbol = "AAPL",
                    CompanyName = "Apple",
                    AssetType = AssetTypeEnum.Stock,
                    Exchange = "NASDAQ",
                    Currency = "USD",
                    Country = "United States",
                    LastPrice = 200m,
                    DayVariationPct = 0.4m,
                    AsOfUtc = DateTime.UtcNow
                });

            var priceProvider = new Mock<IMarketPriceProvider>();
            var options = new DbContextOptionsBuilder<FinanceDbContext>()
                .UseInMemoryDatabase($"PredictFinanceTickerSearch_{Guid.NewGuid()}")
                .Options;

            await using var dbContext = new FinanceDbContext(options, new HttpContextAccessor());
            var service = new TickerService(catalogProvider.Object, priceProvider.Object, dbContext);

            var results = await service.SearchAssetsAsync("AIR");

            Assert.Single(results);
            Assert.Equal("AIR.PA", results[0].Symbol);
            Assert.Equal("PAR", results[0].Exchange);
            Assert.Equal("EUR", results[0].Currency);
        }

        [Fact]
        public async Task TickerService_GetExchangesAsync_DoesNotFallbackToUsExchanges()
        {
            var catalogProvider = new Mock<IMarketCatalogProvider>();
            var priceProvider = new Mock<IMarketPriceProvider>();
            var options = new DbContextOptionsBuilder<FinanceDbContext>()
                .UseInMemoryDatabase($"PredictFinanceTickerExchanges_{Guid.NewGuid()}")
                .Options;

            await using var dbContext = new FinanceDbContext(options, new HttpContextAccessor());
            var service = new TickerService(catalogProvider.Object, priceProvider.Object, dbContext);

            var exchanges = await service.GetExchangesAsync();

            Assert.Empty(exchanges);
        }

        [Fact]
        public async Task TickerService_GetQuoteAsync_RejectsNonFrenchInstrument()
        {
            var catalogProvider = new Mock<IMarketCatalogProvider>();
            catalogProvider
                .Setup(service => service.GetAssetProfileAsync("AAPL", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MarketAssetProfileData
                {
                    Symbol = "AAPL",
                    ProviderSymbol = "AAPL",
                    CompanyName = "Apple",
                    AssetType = AssetTypeEnum.Stock,
                    Exchange = "NASDAQ",
                    Currency = "USD",
                    Country = "United States",
                    LastPrice = 200m,
                    DayVariationPct = 0.4m,
                    AsOfUtc = DateTime.UtcNow
                });

            var priceProvider = new Mock<IMarketPriceProvider>();
            var options = new DbContextOptionsBuilder<FinanceDbContext>()
                .UseInMemoryDatabase($"PredictFinanceTickerQuote_{Guid.NewGuid()}")
                .Options;

            await using var dbContext = new FinanceDbContext(options, new HttpContextAccessor());
            var service = new TickerService(catalogProvider.Object, priceProvider.Object, dbContext);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetQuoteAsync("AAPL"));

            Assert.Equal("L'instrument AAPL n'entre pas dans le perimetre V1. Seules les actions francaises cotees avec des donnees de marche disponibles sont prises en charge.", exception.Message);
        }

        [Fact]
        public async Task ClientFinanceService_SearchAssetsAsync_UsesProviderBackedFrenchDescriptors()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            var httpContextAccessor = BuildHttpContextAccessor();
            services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
            services.AddDbContext<FinanceDbContext>(options =>
                options.UseInMemoryDatabase($"PredictFinanceClientSearch_{Guid.NewGuid()}"));

            var mapperConfiguration = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(typeof(AnalysisResultViewModelProfile).Assembly);
            }, NullLoggerFactory.Instance);
            services.AddSingleton<IMapper>(mapperConfiguration.CreateMapper());

            services.AddSingleton(Mock.Of<IStringLocalizer<Messages>>());
            services.AddSingleton(CreateUserManagerMock().Object);
            services.AddSingleton(CreateRoleManagerMock().Object);
            services.AddSingleton(sp => CreateSignInManagerMock(sp.GetRequiredService<UserManager<User>>(), httpContextAccessor).Object);
            services.AddSingleton(Mock.Of<ILogService>());

            var tickerService = new Mock<ITickerService>();
            tickerService
                .Setup(service => service.SearchAssetsAsync("AIR", It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    new MarketAssetDescriptor
                    {
                        Symbol = "AIR.PA",
                        ProviderSymbol = "AIR.PA",
                        CompanyName = "Airbus",
                        Exchange = "PAR",
                        Currency = "EUR",
                        AssetType = AssetTypeEnum.Stock,
                        LastPrice = 145m,
                        DayVariationPct = 1.5m
                    }
                ]);

            await using var provider = services.BuildServiceProvider();

            var service = new ClientFinanceService(
                provider,
                tickerService.Object,
                Mock.Of<IAnalysisRequestCompatibilityResolver>(),
                Mock.Of<IAnalysisLegacyCompatibilityService>(),
                Mock.Of<IAnalysisOrchestrator>());

            var results = await service.SearchAssetsAsync("AIR");

            var result = Assert.Single(results);
            Assert.Equal("AIR.PA", result.Symbol);
            Assert.Equal("Airbus", result.CompanyName);
            Assert.Equal("PAR", result.Market);
            Assert.Equal("EUR", result.Currency);
            Assert.Equal("STOCK", result.AssetType);
        }

        [Fact]
        public async Task ClientFinanceService_AddToWatchlistAsync_RejectsNonFrenchInstrument()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            var httpContextAccessor = BuildHttpContextAccessor();
            services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
            services.AddDbContext<FinanceDbContext>(options =>
                options.UseInMemoryDatabase($"PredictFinanceWatchlistScope_{Guid.NewGuid()}"));

            var mapperConfiguration = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(typeof(AnalysisResultViewModelProfile).Assembly);
            }, NullLoggerFactory.Instance);
            services.AddSingleton<IMapper>(mapperConfiguration.CreateMapper());

            services.AddSingleton(Mock.Of<IStringLocalizer<Messages>>());
            services.AddSingleton(CreateUserManagerMock().Object);
            services.AddSingleton(CreateRoleManagerMock().Object);
            services.AddSingleton(sp => CreateSignInManagerMock(sp.GetRequiredService<UserManager<User>>(), httpContextAccessor).Object);
            services.AddSingleton(Mock.Of<ILogService>());

            var tickerService = new Mock<ITickerService>();
            tickerService
                .Setup(service => service.GetAssetProfileAsync("AAPL", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(BuildV1ScopeMessage("AAPL")));

            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

            var service = new ClientFinanceService(
                provider,
                tickerService.Object,
                Mock.Of<IAnalysisRequestCompatibilityResolver>(),
                Mock.Of<IAnalysisLegacyCompatibilityService>(),
                Mock.Of<IAnalysisOrchestrator>());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddToWatchlistAsync(
                new WatchlistUpsertRequestViewModel
                {
                    Symbol = "AAPL",
                    CompanyName = "Apple"
                }));

            Assert.Equal(BuildV1ScopeMessage("AAPL"), exception.Message);
            Assert.Equal(0, await dbContext.Assets.CountAsync());
            Assert.Equal(0, await dbContext.UserAssets.CountAsync());
            tickerService.Verify(service => service.GetQuoteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task TickerService_GetTimeSeriesAsync_RejectsNonFrenchInstrument()
        {
            var catalogProvider = new Mock<IMarketCatalogProvider>();
            catalogProvider
                .Setup(service => service.GetAssetProfileAsync("AAPL", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MarketAssetProfileData
                {
                    Symbol = "AAPL",
                    ProviderSymbol = "AAPL",
                    CompanyName = "Apple",
                    AssetType = AssetTypeEnum.Stock,
                    Exchange = "NASDAQ",
                    Currency = "USD",
                    Country = "United States",
                    LastPrice = 200m,
                    DayVariationPct = 0.4m,
                    AsOfUtc = DateTime.UtcNow
                });

            var priceProvider = new Mock<IMarketPriceProvider>();
            var options = new DbContextOptionsBuilder<FinanceDbContext>()
                .UseInMemoryDatabase($"PredictFinanceTickerTimeSeries_{Guid.NewGuid()}")
                .Options;

            await using var dbContext = new FinanceDbContext(options, new HttpContextAccessor());
            var service = new TickerService(catalogProvider.Object, priceProvider.Object, dbContext);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetTimeSeriesAsync("AAPL", "1day", 30));

            Assert.Equal(BuildV1ScopeMessage("AAPL"), exception.Message);
            priceProvider.Verify(service => service.GetChartAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void AnalysisPatternRegistry_ResolveRequestedPattern_ReturnsApiOwnedDoubleTopOnly()
        {
            var registry = new AnalysisPatternRegistry([new DoubleTopAnalysisPatternDefinition(Mock.Of<ITickerService>())]);

            var resolved = registry.ResolveRequestedPattern(null);
            var enabled = Assert.Single(registry.GetEnabledPatterns());

            Assert.Equal("DOUBLE_TOP", resolved.PatternId);
            Assert.Equal("analysis-v1-deterministic-double-top@prompt7", resolved.ModelVersion);
            Assert.Equal(6, resolved.HistoryLookbackMonths);
            Assert.Equal("DOUBLE_TOP", enabled.PatternId);
        }

        [Fact]
        public async Task AnalysisPatternRegistry_ResolveRequestedPattern_RejectsPatternsOutsideActiveV1Scope()
        {
            var registry = new AnalysisPatternRegistry([new DoubleTopAnalysisPatternDefinition(Mock.Of<ITickerService>())]);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromResult(registry.ResolveRequestedPattern("TRIANGLE")));

            Assert.Equal("Le runtime V1 actif ne prend pas en charge le pattern TRIANGLE.", exception.Message);
        }

        [Fact]
        public async Task AnalysisRequestCompatibilityResolver_ResolveAsync_UsesApiOwnedPatternWindow()
        {
            var options = new DbContextOptionsBuilder<FinanceDbContext>()
                .UseInMemoryDatabase($"PredictFinanceAnalysisResolver_{Guid.NewGuid()}")
                .Options;

            await using var dbContext = new FinanceDbContext(options, new HttpContextAccessor());
            var tickerService = new Mock<ITickerService>();
            tickerService
                .Setup(service => service.GetQuoteAsync("AIR.PA", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MarketQuoteData
                {
                    Symbol = "AIR.PA",
                    AssetType = AssetTypeEnum.Stock,
                    LastPrice = 145m,
                    DayVariationPct = 0.5m,
                    AsOfUtc = new DateTime(2025, 3, 31, 16, 0, 0, DateTimeKind.Utc)
                });

            tickerService
                .Setup(service => service.GetAssetProfileAsync("AIR.PA", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MarketAssetProfileData
                {
                    Symbol = "AIR.PA",
                    ProviderSymbol = "AIR.PA",
                    CompanyName = "Airbus",
                    Exchange = "PAR",
                    Currency = "EUR",
                    Country = "FR",
                    AssetType = AssetTypeEnum.Stock,
                    LastPrice = 145m,
                    DayVariationPct = 0.5m,
                    AsOfUtc = new DateTime(2025, 3, 31, 16, 0, 0, DateTimeKind.Utc)
                });

            var resolver = new AnalysisRequestCompatibilityResolver(
                dbContext,
                new AnalysisPatternRegistry([new DoubleTopAnalysisPatternDefinition(tickerService.Object)]),
                Mock.Of<IPortfolioContextLoader>(),
                tickerService.Object);

            var result = await resolver.ResolveAsync(new AnalysisRunRequest
            {
                Symbol = "AIR.PA"
            }, "user-1");

            Assert.Equal(new DateOnly(2025, 3, 31), result.HistoryEndDate);
            Assert.Equal(new DateOnly(2024, 9, 30), result.HistoryStartDate);
            Assert.Equal("DOUBLE_TOP", Assert.Single(result.ResolvedPatternIds));
        }

        [Fact]
        public async Task DeterministicAnalysisExecutionService_ExecuteAsync_ProducesDoubleTopAssessment()
        {
            var tickerService = new Mock<ITickerService>();
            tickerService
                .Setup(service => service.GetTimeSeriesAsync("AIR.PA", "1d", It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TickerTimeSeriesResponse
                {
                    Symbol = "AIR.PA",
                    Interval = "1d",
                    OutputSize = 120,
                    Candles = BuildDoubleTopCandles()
                });

            var patternDefinition = new DoubleTopAnalysisPatternDefinition(tickerService.Object);
            var registry = new AnalysisPatternRegistry([patternDefinition]);
            var service = new DeterministicAnalysisExecutionService(registry);
            var request = new AnalysisRequest
            {
                InstrumentId = "asset-1",
                UserId = "user-1",
                Instrument = new Instrument
                {
                    InstrumentId = "asset-1",
                    Symbol = "AIR.PA",
                    DisplayName = "Airbus",
                    AssetType = "EQUITY",
                    CurrencyCode = "EUR"
                },
                PortfolioContext = new PortfolioContext
                {
                    UserId = "user-1",
                    InstrumentId = "asset-1",
                    HoldsInstrument = false,
                    CurrencyCode = "EUR"
                },
                CandleInterval = "1d",
                RequestedPatternIds = ["DOUBLE_TOP"],
                ResolvedPatternIds = ["DOUBLE_TOP"],
                HistoryStartDate = new DateOnly(2025, 1, 1),
                HistoryEndDate = new DateOnly(2025, 3, 31)
            };

            var result = await service.ExecuteAsync(request, [registry.ResolveRequestedPattern("DOUBLE_TOP")]);

            Assert.Equal("AIR.PA", result.Symbol);
            Assert.Equal(ModelStatusEnum.Go, result.ModelStatus);
            Assert.Equal("analysis-v1-deterministic-double-top@prompt7", result.ModelVersion);

            var pattern = Assert.Single(result.Patterns);
            Assert.Equal(TradingPatternEnum.DoubleTop, pattern.Pattern);
            Assert.Equal("neckline_break_confirmed", pattern.Phase);
            Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
            Assert.Equal(PatternStatusEnum.Confirmed, pattern.ContractAssessment.Detection.Status);
            Assert.True(pattern.ContractAssessment.Scoring.ConfidenceScore >= 0.74m);
            Assert.NotNull(pattern.NecklinePrice);
            Assert.NotNull(pattern.TargetPrice);
            Assert.NotNull(pattern.InvalidationPrice);
            Assert.Contains("\"engine\":\"API_DETERMINISTIC\"", result.RawProviderPayloadJson, StringComparison.Ordinal);
        }

        [Fact]
        public void AnalysisPatternRegistry_CanResolveExplicitPatternAmongMultipleDefinitionsWithoutClaimingRuntimeBreadth()
        {
            var registry = new AnalysisPatternRegistry(
            [
                new DoubleTopAnalysisPatternDefinition(Mock.Of<ITickerService>()),
                new TestAnalysisPatternDefinition("HEAD_AND_SHOULDERS", "analysis-v1-deterministic-head-and-shoulders@test", 9)
            ]);

            var resolved = registry.ResolveRequestedPattern("HEAD_AND_SHOULDERS");
            var enabled = registry.GetEnabledPatterns();

            Assert.Equal("HEAD_AND_SHOULDERS", resolved.PatternId);
            Assert.Equal("analysis-v1-deterministic-head-and-shoulders@test", resolved.ModelVersion);
            Assert.Equal(9, resolved.HistoryLookbackMonths);
            Assert.Equal(2, enabled.Count);
        }

        [Fact]
        public async Task AnalysisRequestCompatibilityResolver_ResolveAsync_RequiresExplicitPatternWhenMultipleDefinitionsAreActive()
        {
            var options = new DbContextOptionsBuilder<FinanceDbContext>()
                .UseInMemoryDatabase($"PredictFinanceAnalysisResolverMulti_{Guid.NewGuid()}")
                .Options;

            await using var dbContext = new FinanceDbContext(options, new HttpContextAccessor());
            var tickerService = new Mock<ITickerService>();
            tickerService
                .Setup(service => service.GetQuoteAsync("AIR.PA", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MarketQuoteData
                {
                    Symbol = "AIR.PA",
                    AssetType = AssetTypeEnum.Stock,
                    LastPrice = 145m,
                    DayVariationPct = 0.5m,
                    AsOfUtc = new DateTime(2025, 3, 31, 16, 0, 0, DateTimeKind.Utc)
                });

            var resolver = new AnalysisRequestCompatibilityResolver(
                dbContext,
                new AnalysisPatternRegistry(
                [
                    new DoubleTopAnalysisPatternDefinition(tickerService.Object),
                    new TestAnalysisPatternDefinition("HEAD_AND_SHOULDERS", "analysis-v1-deterministic-head-and-shoulders@test", 9)
                ]),
                Mock.Of<IPortfolioContextLoader>(),
                tickerService.Object);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.ResolveAsync(new AnalysisRunRequest
            {
                Symbol = "AIR.PA"
            }, "user-1"));

            Assert.Equal("Le runtime V1 actif requiert un pattern explicite tant que plusieurs definitions de patterns sont actives.", exception.Message);
        }


        [Fact]
        public void AnalysisLegacyCompatibilityService_MapRunResult_PreservesModelTruthFromActiveResponse()
        {
            var service = new AnalysisLegacyCompatibilityService(BuildServiceProviderForLegacyCompatibility());
            var response = new AnalysisResponse
            {
                AnalysisId = "analysis-1",
                GeneratedAtUtc = new DateTime(2025, 4, 1, 12, 0, 0, DateTimeKind.Utc),
                AsOfDate = new DateOnly(2025, 4, 1),
                AnalysisOutcome = AnalysisOutcomeEnum.NoCrediblePattern,
                Instrument = new Instrument
                {
                    InstrumentId = "asset-1",
                    Symbol = "AIR.PA",
                    DisplayName = "Airbus"
                },
                ExecutedPatternIds = ["DOUBLE_TOP"],
                PedagogicalSummary = "Aucun pattern credible n'a ete retenu.",
                ModelStatus = ModelStatusEnum.Go,
                ModelMessage = "Model quality gate passed."
            };

            var result = service.MapRunResult(response);

            Assert.Equal(ModelStatusEnum.Go, result.ModelStatus);
            Assert.Equal("Model quality gate passed.", result.ModelMessage);
            Assert.Equal(TradingPatternEnum.DoubleTop, result.Pattern);
        }

        [Fact]
        public void RecommendationPolicyService_EvaluateAnalysis_ProducesStructuredAdviceTruth()
        {
            var tradingRecommendationService = new Mock<ITradingRecommendationService>();
            tradingRecommendationService
                .Setup(x => x.EvaluateAnalysis(
                    TradingPatternEnum.DoubleTop,
                    "CONFIRMED",
                    0.78m,
                    145m,
                    118m))
                .Returns(new TradingRecommendationResult
                {
                    Action = RecommendationActionEnum.Buy,
                    Reason = "legacy",
                    HorizonDays = 20,
                    IsActionable = true,
                    RiskLevel = RiskLevelEnum.Low
                });

            var service = new RecommendationPolicyService();
            var request = new AnalysisRequest
            {
                InstrumentId = "asset-1",
                UserId = "user-1",
                Instrument = new Instrument
                {
                    InstrumentId = "asset-1",
                    Symbol = "AIR.PA",
                    DisplayName = "Airbus",
                    AssetType = "EQUITY",
                    CurrencyCode = "EUR"
                },
                PortfolioContext = new PortfolioContext
                {
                    UserId = "user-1",
                    InstrumentId = "asset-1",
                    HoldsInstrument = true,
                    CurrencyCode = "EUR"
                }
            };

            var patternAssessment = new BackPredictFinance.Contracts.Analysis.PatternAssessment
            {
                PatternId = "DOUBLE_TOP",
                DisplayName = "Double sommet",
                Detection = new PatternDetection
                {
                    IsCompatible = true,
                    Status = PatternStatusEnum.Confirmed,
                    CurrentPhaseCode = "CONFIRMED",
                    CurrentPhaseLabel = "Confirme"
                },
                Validation = new PatternValidation
                {
                    State = "VALIDATED"
                },
                Scoring = new PatternScoring
                {
                    ConfidenceScore = 0.78m,
                    ConfidenceLabel = "HIGH",
                    IsCredible = true
                },
                RiskHints = new PatternRiskHints
                {
                    HasRiskPlan = true,
                    SuggestedTakeProfit = 145m,
                    RiskRewardRatio = 1.8m
                },
                Invalidation = new PatternInvalidation
                {
                    State = "ACTIVE",
                    InvalidationLevel = 118m
                }
            };

            var recommendation = service.EvaluateAnalysis(
                request,
                [patternAssessment],
                AnalysisOutcomeEnum.CrediblePatternFound);

            Assert.Equal(RecommendationSituationCode.CompatiblePatternConfirmed, recommendation.SituationCode);
            Assert.Equal(AdviceScenarioCode.PrimaryPatternHeldBullish, recommendation.AdviceScenarioCode);
            Assert.Equal(RecommendationActionEnum.Buy, recommendation.RecommendationAction);
            Assert.Equal(RecommendationStrengthEnum.High, recommendation.RecommendationStrength);
            Assert.True(recommendation.Parameters.HoldsInstrument);
            Assert.True(recommendation.Parameters.IsActionable);
            Assert.Equal("DOUBLE_TOP", recommendation.Parameters.PrimaryPatternId);
            Assert.Equal("VALIDATED", recommendation.Parameters.ValidationState);
            Assert.Equal(20, recommendation.ReviewHorizonDays);
        }

        [Fact]
        public void RecommendationPolicyService_EvaluateAnalysis_RejectsUnsupportedPatternId()
        {
            var service = new RecommendationPolicyService();
            var request = new AnalysisRequest
            {
                InstrumentId = "asset-1",
                UserId = "user-1",
                Instrument = new Instrument
                {
                    InstrumentId = "asset-1",
                    Symbol = "AIR.PA",
                    DisplayName = "Airbus",
                    AssetType = "EQUITY",
                    CurrencyCode = "EUR"
                },
                PortfolioContext = new PortfolioContext
                {
                    UserId = "user-1",
                    InstrumentId = "asset-1",
                    HoldsInstrument = false,
                    CurrencyCode = "EUR"
                }
            };

            var patternAssessment = new BackPredictFinance.Contracts.Analysis.PatternAssessment
            {
                PatternId = "TRIANGLE",
                DisplayName = "Triangle",
                Detection = new PatternDetection
                {
                    IsCompatible = true,
                    Status = PatternStatusEnum.Forming,
                    CurrentPhaseCode = "FORMING",
                    CurrentPhaseLabel = "Formation"
                },
                Scoring = new PatternScoring
                {
                    ConfidenceScore = 0.55m,
                    ConfidenceLabel = "MEDIUM",
                    IsCredible = true
                },
                RiskHints = new PatternRiskHints(),
                Invalidation = new PatternInvalidation()
            };

            var exception = Assert.Throws<InvalidOperationException>(() => service.EvaluateAnalysis(
                request,
                [patternAssessment],
                AnalysisOutcomeEnum.CrediblePatternFound));

            Assert.Equal("Le runtime V1 actif ne prend pas en charge la recommandation pour le pattern TRIANGLE.", exception.Message);
        }

        private static IServiceProvider BuildServiceProviderForLegacyCompatibility()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddSingleton<IHttpContextAccessor>(BuildHttpContextAccessor());
            services.AddDbContext<FinanceDbContext>(options =>
                options.UseInMemoryDatabase($"PredictFinanceLegacyCompatibility_{Guid.NewGuid()}"));

            var mapperConfiguration = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(typeof(AnalysisResultViewModelProfile).Assembly);
            }, NullLoggerFactory.Instance);

            services.AddSingleton<IMapper>(mapperConfiguration.CreateMapper());
            services.AddSingleton(Mock.Of<IStringLocalizer<Messages>>());
            services.AddSingleton(CreateUserManagerMock().Object);
            services.AddSingleton(CreateRoleManagerMock().Object);
            services.AddSingleton(sp => CreateSignInManagerMock(sp.GetRequiredService<UserManager<User>>(), sp.GetRequiredService<IHttpContextAccessor>()).Object);
            services.AddSingleton(Mock.Of<ILogService>());

            return services.BuildServiceProvider();
        }

        [Fact]
        public void TradingController_PredictEndpoints_ReturnLegacyGone()
        {
            var controller = new TradingController();

            var predictResult = Assert.IsType<ObjectResult>(controller.Predict());
            var predictDetails = Assert.IsType<ProblemDetails>(predictResult.Value);
            Assert.Equal(StatusCodes.Status410Gone, predictResult.StatusCode);
            Assert.Contains("surface V1", predictDetails.Detail, StringComparison.Ordinal);

            var predictBySymbolResult = Assert.IsType<ObjectResult>(controller.PredictBySymbol("AIR.PA"));
            var predictBySymbolDetails = Assert.IsType<ProblemDetails>(predictBySymbolResult.Value);
            Assert.Equal(StatusCodes.Status410Gone, predictBySymbolResult.StatusCode);
            Assert.Contains("surface V1", predictBySymbolDetails.Detail, StringComparison.Ordinal);
        }

        private static IHttpContextAccessor BuildHttpContextAccessor()
        {
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
                new Claim("sub", "user-1")
            ], "TestAuth"));

            return new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }

        private static Mock<UserManager<User>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(
                store.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<User>>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<User>>>());
        }

        private static Mock<RoleManager<IdentityRole>> CreateRoleManagerMock()
        {
            var store = new Mock<IRoleStore<IdentityRole>>();
            return new Mock<RoleManager<IdentityRole>>(
                store.Object,
                Array.Empty<IRoleValidator<IdentityRole>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<ILogger<RoleManager<IdentityRole>>>());
        }

        private static Mock<SignInManager<User>> CreateSignInManagerMock(UserManager<User> userManager, IHttpContextAccessor httpContextAccessor)
        {
            return new Mock<SignInManager<User>>(
                userManager,
                httpContextAccessor,
                Mock.Of<IUserClaimsPrincipalFactory<User>>(),
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<ILogger<SignInManager<User>>>(),
                Mock.Of<IAuthenticationSchemeProvider>(),
                Mock.Of<IUserConfirmation<User>>());
        }

        private static void SetAnalysisHistoryAvailable(AnalysisSnapshotPersistenceService service, bool value)
        {
            var field = typeof(AnalysisSnapshotPersistenceService).GetField("_analysisHistorySchemaAvailable", BindingFlags.Instance | BindingFlags.NonPublic);
            field!.SetValue(service, value);
        }

        private static string BuildV1ScopeMessage(string symbol)
            => $"L'instrument {symbol} n'entre pas dans le perimetre V1. Seules les actions francaises cotees avec des donnees de marche disponibles sont prises en charge.";

        private static List<TickerCandle> BuildDoubleTopCandles()
        {
            var closes = new decimal[]
            {
                90m, 91m, 92m, 93m, 94m, 95m, 96m, 97m, 98m, 99m,
                101m, 103m, 106m, 110m, 114m, 122m, 124m, 126m, 123m, 121m,
                119m, 117m, 119m, 121m, 123m, 124m, 125m, 123m, 121m, 118m,
                114m, 111m, 109m, 108m, 107m, 106m, 105m, 104m, 103m, 102m,
                101m, 100m, 99m, 98m, 97m, 96m, 95m, 94m, 93m, 92m
            };

            var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return closes
                .Select((close, index) => new TickerCandle
                {
                    Date = start.AddDays(index),
                    Open = close,
                    High = close + 1m,
                    Low = close - 1m,
                    Close = close,
                    Volume = 1000m + index
                })
                .ToList();
        }
        private sealed class TestAnalysisPatternDefinition : IAnalysisPatternDefinition
        {
            public TestAnalysisPatternDefinition(string patternId, string modelVersion, int historyLookbackMonths)
            {
                PatternId = patternId;
                ModelVersion = modelVersion;
                HistoryLookbackMonths = historyLookbackMonths;
            }

            public string PatternId { get; }
            public string DisplayName => PatternId;
            public string FamilyId => "TEST_FAMILY";
            public string BiasCode => "NEUTRAL";
            public string ModelVersion { get; }
            public int HistoryLookbackMonths { get; }
            public int MinimumRequiredCandles => 20;

            public ResolvedAnalysisPattern BuildResolvedPattern()
            {
                return new ResolvedAnalysisPattern
                {
                    PatternId = PatternId,
                    ModelVersion = ModelVersion,
                    HistoryLookbackMonths = HistoryLookbackMonths
                };
            }

            public Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, CancellationToken ct = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
