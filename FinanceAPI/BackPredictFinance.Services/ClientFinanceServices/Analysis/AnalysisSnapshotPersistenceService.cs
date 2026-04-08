using BackPredictFinance.Contracts.Analysis;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using BackPredictFinance.Common.Common;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{

public interface IAnalysisSnapshotPersistenceService
{
    Task<PersistedAnalysisRecord> PersistSuccessfulAnalysisAsync(
        AnalysisRequest request,
        ResolvedAnalysisPattern pattern,
        AnalysisExecutionArtifact executionArtifact,
        AnalysisRecommendation recommendation,
        AnalysisOutcomeEnum outcome,
        string pedagogicalSummary,
        string explanationPolicyVersion,
        DateTime startedAtUtc,
        DateTime completedAtUtc,
        CancellationToken ct = default);

    Task PersistFailedAnalysisAsync(
        AnalysisRequest request,
        ResolvedAnalysisPattern pattern,
        DateTime startedAtUtc,
        DateTime completedAtUtc,
        Exception exception,
        CancellationToken ct = default);
}


    public sealed class AnalysisSnapshotPersistenceService : BaseService, IAnalysisSnapshotPersistenceService
    {
        private const string SnapshotPayloadVersion = "analysis-snapshot-history@prompt5";
        private const string MarketDataProviderCode = "YAHOO_FINANCE";
        private static readonly JsonSerializerOptions SnapshotSerializerOptions = CreateSnapshotSerializerOptions();
        private bool? _analysisHistorySchemaAvailable;

        public AnalysisSnapshotPersistenceService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public async Task<PersistedAnalysisRecord> PersistSuccessfulAnalysisAsync(
            AnalysisRequest request,
            ResolvedAnalysisPattern pattern,
            AnalysisExecutionArtifact executionArtifact,
            AnalysisRecommendation recommendation,
            AnalysisOutcomeEnum outcome,
            string pedagogicalSummary,
            string explanationPolicyVersion,
            DateTime startedAtUtc,
            DateTime completedAtUtc,
            CancellationToken ct = default)
        {
            var asset = await EnsureAssetAsync(request.Instrument, ct);
            var userAsset = await EnsureUserAssetAsync(request.UserId, asset.Id, ct);
            var analysisRun = await TryPersistAnalysisRunAsync(request, asset, pattern, executionArtifact, recommendation, outcome, pedagogicalSummary, explanationPolicyVersion, startedAtUtc, completedAtUtc, ct);

            var legacyRecommendation = new BackPredictFinance.Datas.Entities.Recommendation
            {
                UserAssetId = userAsset.Id,
                Action = recommendation.RecommendationAction,
                Confidence = executionArtifact.GetOrderedPatterns()
                    .Select(executedPattern => executedPattern.Confidence)
                    .FirstOrDefault(),
                RecommendedAtUtc = executionArtifact.GeneratedAtUtc,
                Reason = recommendation.Rationale
            };

            await _financeDbContext.Set<BackPredictFinance.Datas.Entities.Recommendation>().AddAsync(legacyRecommendation, ct);
            await _financeDbContext.SaveChangesAsync(ct);

            return new PersistedAnalysisRecord
            {
                PublicId = analysisRun?.Id ?? legacyRecommendation.Id,
                InstrumentId = asset.Id,
                Symbol = asset.Symbol,
                ProviderSymbol = string.IsNullOrWhiteSpace(asset.ProviderSymbol) ? asset.Symbol : asset.ProviderSymbol,
                CompanyName = asset.Name ?? asset.Symbol,
                MarketCode = asset.Exchange,
                CurrencyCode = asset.Currency,
                AssetType = asset.AssetType == AssetTypeEnum.Stock ? "EQUITY" : asset.AssetType.ToString().ToUpperInvariant(),
                CountryCode = asset.Country ?? string.Empty,
                IsActive = true,
                LastProfileSyncUtc = asset.LastProfileSyncUtc,
                Summary = asset.Summary
            };
        }

        public async Task PersistFailedAnalysisAsync(
            AnalysisRequest request,
            ResolvedAnalysisPattern pattern,
            DateTime startedAtUtc,
            DateTime completedAtUtc,
            Exception exception,
            CancellationToken ct = default)
        {
            var asset = await EnsureAssetAsync(request.Instrument, ct);
            await TryPersistFailedAnalysisRunAsync(request, asset, pattern, startedAtUtc, completedAtUtc, exception, ct);
        }

        private async Task<AnalysisRun?> TryPersistAnalysisRunAsync(
            AnalysisRequest request,
            Asset asset,
            ResolvedAnalysisPattern pattern,
            AnalysisExecutionArtifact executionArtifact,
            AnalysisRecommendation recommendation,
            AnalysisOutcomeEnum outcome,
            string pedagogicalSummary,
            string explanationPolicyVersion,
            DateTime startedAtUtc,
            DateTime completedAtUtc,
            CancellationToken ct)
        {
            if (!await CanUseAnalysisHistoryAsync(ct))
            {
                return null;
            }

            var primaryPattern = executionArtifact.GetOrderedPatterns().FirstOrDefault();

            var snapshotPayload = BuildSuccessfulSnapshotPayload(
                request,
                asset,
                pattern,
                executionArtifact,
                recommendation,
                outcome,
                pedagogicalSummary,
                explanationPolicyVersion,
                startedAtUtc,
                completedAtUtc);

            var analysisRun = new AnalysisRun
            {
                Id = snapshotPayload.SnapshotId,
                UserId = request.UserId,
                AssetId = asset.Id,
                RequestedPattern = ParseTradingPattern(pattern.PatternId),
                Status = "Completed",
                StartedAtUtc = startedAtUtc,
                CompletedAtUtc = completedAtUtc,
                RawPayload = JsonSerializer.Serialize(snapshotPayload, SnapshotSerializerOptions),
                PatternAssessments = executionArtifact.Patterns
                    .Select(BuildPersistencePatternAssessment)
                    .ToList(),
                DecisionSignal = new DecisionSignal
                {
                    Action = recommendation.RecommendationAction,
                    IsActionable = recommendation.Parameters.IsActionable,
                    Confidence = primaryPattern?.Confidence ?? 0m,
                    HorizonDays = recommendation.ReviewHorizonDays ?? 0,
                    Reason = string.IsNullOrWhiteSpace(recommendation.Rationale) ? "Aucune justification" : recommendation.Rationale.Trim()
                },
                ModelSnapshot = new ModelSnapshot
                {
                    ModelStatus = executionArtifact.ModelStatus,
                    ModelMessage = string.IsNullOrWhiteSpace(executionArtifact.ModelMessage)
                        ? string.Empty
                        : executionArtifact.ModelMessage.Trim(),
                    ModelVersion = executionArtifact.ResolveAnalysisEngineVersion(pattern.ModelVersion),
                    Precision = executionArtifact.Precision,
                    F1 = executionArtifact.F1,
                    RocAuc = executionArtifact.RocAuc,
                    PositiveSamples = executionArtifact.PositiveSamples,
                    SelectedThreshold = executionArtifact.SelectedThreshold
                }
            };

            await _financeDbContext.AnalysisRuns.AddAsync(analysisRun, ct);
            await _financeDbContext.SaveChangesAsync(ct);
            return analysisRun;
        }

        private async Task<AnalysisRun?> TryPersistFailedAnalysisRunAsync(
            AnalysisRequest request,
            Asset asset,
            ResolvedAnalysisPattern pattern,
            DateTime startedAtUtc,
            DateTime completedAtUtc,
            Exception exception,
            CancellationToken ct)
        {
            if (!await CanUseAnalysisHistoryAsync(ct))
            {
                return null;
            }

            var rawPayload = BuildFailedAnalysisRawPayload(request, asset, pattern, startedAtUtc, completedAtUtc, exception);
            var errorMessage = exception is CustomException customException && !string.IsNullOrWhiteSpace(customException.FrontMessage)
                ? customException.FrontMessage.Trim()
                : "L'analyse V1 n'a pas pu etre calculee.";

            var analysisRun = new AnalysisRun
            {
                UserId = request.UserId,
                AssetId = asset.Id,
                RequestedPattern = ParseTradingPattern(pattern.PatternId),
                Status = "Failed",
                StartedAtUtc = startedAtUtc,
                CompletedAtUtc = completedAtUtc,
                RawPayload = rawPayload,
                ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                    ? "Le moteur IA n'a pas pu terminer l'analyse."
                    : errorMessage
            };

            await _financeDbContext.AnalysisRuns.AddAsync(analysisRun, ct);
            await _financeDbContext.SaveChangesAsync(ct);
            return analysisRun;
        }

        private static string BuildFailedAnalysisRawPayload(
            AnalysisRequest request,
            Asset asset,
            ResolvedAnalysisPattern pattern,
            DateTime startedAtUtc,
            DateTime completedAtUtc,
            Exception exception)
        {

            var payload = new PersistedAnalysisFailurePayload
            {
                SchemaVersion = "analysis-failure@prompt7",
                UserId = request.UserId,
                InstrumentId = asset.Id,
                Symbol = asset.Symbol,
                PatternId = pattern.PatternId,
                RequestedAtUtc = startedAtUtc,
                CompletedAtUtc = completedAtUtc,
                ErrorType = exception.GetType().Name,
                ErrorMessage = exception.Message,
                FrontMessage = exception is CustomException customException && !string.IsNullOrWhiteSpace(customException.FrontMessage)
                    ? customException.FrontMessage.Trim()
                    : "L'analyse V1 n'a pas pu etre calculee."
            };

            return JsonSerializer.Serialize(payload, SnapshotSerializerOptions);
        }

        private static BackPredictFinance.Datas.Entities.PatternAssessment BuildPersistencePatternAssessment(ExecutedPatternArtifact executedPattern)
        {
            return new BackPredictFinance.Datas.Entities.PatternAssessment
            {
                Pattern = executedPattern.Pattern,
                Phase = executedPattern.Phase,
                Probability = executedPattern.Probability,
                Confidence = executedPattern.Confidence,
                CurrentPrice = executedPattern.CurrentPrice,
                NecklinePrice = executedPattern.NecklinePrice,
                TargetPrice = executedPattern.TargetPrice,
                InvalidationPrice = executedPattern.InvalidationPrice,
                FirstPeakAtUtc = executedPattern.FirstPeakAtUtc,
                SecondPeakAtUtc = executedPattern.SecondPeakAtUtc,
                IsPrimary = executedPattern.IsPrimary
            };
        }

        private static PersistedAnalysisSnapshotPayload BuildSuccessfulSnapshotPayload(
            AnalysisRequest request,
            Asset asset,
            ResolvedAnalysisPattern pattern,
            AnalysisExecutionArtifact executionArtifact,
            AnalysisRecommendation recommendation,
            AnalysisOutcomeEnum outcome,
            string pedagogicalSummary,
            string explanationPolicyVersion,
            DateTime startedAtUtc,
            DateTime completedAtUtc)
        {
            var orderedPatterns = executionArtifact.Patterns
                .OrderByDescending(executedPattern => executedPattern.IsPrimary)
                .ThenByDescending(executedPattern => executedPattern.Confidence)
                .ThenByDescending(executedPattern => executedPattern.Probability)
                .ToList();

            var patternRows = orderedPatterns
                .Select((executedPattern, index) => new AnalysisSnapshotPatternRow
                {
                    SnapshotPatternRowId = executedPattern.ContractAssessment.AssessmentId,
                    SnapshotId = string.Empty,
                    PatternId = executedPattern.ContractAssessment.PatternId,
                    DisplayRank = index + 1,
                    IsCompatible = executedPattern.ContractAssessment.Detection.IsCompatible,
                    IsPrimaryDisplayCandidate = executedPattern.ContractAssessment.Trace.IsPrimaryDisplayCandidate,
                    PatternAssessmentPayload = executedPattern.ContractAssessment
                })
                .ToList();

            var primaryPatternId = patternRows
                .Where(x => x.IsCompatible)
                .OrderBy(x => x.DisplayRank)
                .Select(x => x.PatternId)
                .FirstOrDefault();

            var snapshotId = Guid.NewGuid().ToString("N");
            foreach (var patternRow in patternRows)
            {
                patternRow.SnapshotId = snapshotId;
            }

            var recommendationSnapshot = new PersistedAnalysisSnapshotRecommendationPayload
            {
                SnapshotRecommendationId = recommendation.RecommendationId,
                SnapshotId = snapshotId,
                RecommendationPayload = recommendation,
                CreatedAtUtc = completedAtUtc
            };

            return new PersistedAnalysisSnapshotPayload
            {
                SchemaVersion = SnapshotPayloadVersion,
                SnapshotId = snapshotId,
                UserId = request.UserId,
                InstrumentId = asset.Id,
                InstrumentSnapshot = CloneInstrument(request.Instrument),
                RequestedPatternIds = [.. request.RequestedPatternIds],
                ExecutedPatternIds = executionArtifact.GetExecutedPatternIds(pattern.PatternId),
                Outcome = outcome,
                RequestedAtUtc = startedAtUtc,
                CompletedAtUtc = completedAtUtc,
                AsOfDate = request.HistoryEndDate,
                CandleInterval = string.IsNullOrWhiteSpace(request.CandleInterval) ? "1d" : request.CandleInterval.Trim(),
                MarketDataProviderCode = MarketDataProviderCode,
                MarketDataRangeStart = request.HistoryStartDate,
                MarketDataRangeEnd = request.HistoryEndDate,
                PortfolioContextSnapshot = BuildPortfolioContextSummary(request.PortfolioContext),
                PortfolioContextUsed = ClonePortfolioContext(request.PortfolioContext),
                PrimaryPatternId = primaryPatternId,
                RecommendationId = recommendation.RecommendationId,
                TraceId = snapshotId,
                AnalysisEngineVersion = executionArtifact.ResolveAnalysisEngineVersion(pattern.ModelVersion),
                RecommendationPolicyVersion = string.IsNullOrWhiteSpace(recommendation.PolicyVersion) ? null : recommendation.PolicyVersion.Trim(),
                ExplanationPolicyVersion = string.IsNullOrWhiteSpace(explanationPolicyVersion) ? null : explanationPolicyVersion.Trim(),
                MarketNormalizationVersion = null,
                PedagogicalSummary = string.IsNullOrWhiteSpace(pedagogicalSummary) ? string.Empty : pedagogicalSummary.Trim(),
                PatternRows = patternRows,
                Recommendation = recommendationSnapshot,
                ModelSnapshot = new PersistedModelSnapshotPayload
                {
                    ModelStatus = executionArtifact.ModelStatus,
                    ModelMessage = string.IsNullOrWhiteSpace(executionArtifact.ModelMessage) ? string.Empty : executionArtifact.ModelMessage.Trim(),
                    ModelVersion = executionArtifact.ResolveAnalysisEngineVersion(pattern.ModelVersion),
                    Precision = executionArtifact.Precision,
                    F1 = executionArtifact.F1,
                    RocAuc = executionArtifact.RocAuc,
                    PositiveSamples = executionArtifact.PositiveSamples,
                    SelectedThreshold = executionArtifact.SelectedThreshold
                },
                RawProviderPayloadJson = executionArtifact.RawProviderPayloadJson
            };
        }

        private static SnapshotPortfolioContextSummary BuildPortfolioContextSummary(PortfolioContext portfolioContext)
        {
            return new SnapshotPortfolioContextSummary
            {
                HoldsInstrument = portfolioContext.HoldsInstrument,
                TotalQuantityHeld = portfolioContext.TotalQuantityHeld,
                AverageUnitCost = portfolioContext.AverageUnitCost,
                OpenLineCount = portfolioContext.OpenLineCount,
                CurrencyCode = portfolioContext.CurrencyCode
            };
        }

        private static Instrument CloneInstrument(Instrument instrument)
        {
            return new Instrument
            {
                InstrumentId = instrument.InstrumentId,
                Symbol = instrument.Symbol,
                ProviderSymbol = instrument.ProviderSymbol,
                DisplayName = instrument.DisplayName,
                MarketCode = instrument.MarketCode,
                CountryCode = instrument.CountryCode,
                CurrencyCode = instrument.CurrencyCode,
                AssetType = instrument.AssetType,
                IsActive = instrument.IsActive,
                LastProfileSyncUtc = instrument.LastProfileSyncUtc,
                Summary = instrument.Summary
            };
        }

        private static PortfolioContext ClonePortfolioContext(PortfolioContext portfolioContext)
        {
            return new PortfolioContext
            {
                UserId = portfolioContext.UserId,
                InstrumentId = portfolioContext.InstrumentId,
                HoldsInstrument = portfolioContext.HoldsInstrument,
                OpenLineCount = portfolioContext.OpenLineCount,
                TotalQuantityHeld = portfolioContext.TotalQuantityHeld,
                AverageUnitCost = portfolioContext.AverageUnitCost,
                CurrencyCode = portfolioContext.CurrencyCode,
                OpenLines = portfolioContext.OpenLines
                    .Select(openLine => new PortfolioContextLine
                    {
                        Quantity = openLine.Quantity,
                        UnitBuyPrice = openLine.UnitBuyPrice,
                        BuyDate = openLine.BuyDate,
                        FeesAmount = openLine.FeesAmount,
                        CurrencyCode = openLine.CurrencyCode
                    })
                    .ToList(),
                OldestOpenBuyDate = portfolioContext.OldestOpenBuyDate,
                LatestOpenBuyDate = portfolioContext.LatestOpenBuyDate
            };
        }

        private static JsonSerializerOptions CreateSnapshotSerializerOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        private async Task<bool> CanUseAnalysisHistoryAsync(CancellationToken ct)
        {
            if (_analysisHistorySchemaAvailable.HasValue)
            {
                return _analysisHistorySchemaAvailable.Value;
            }

            const string sql = """
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_NAME IN ('AnalysisRuns', 'PatternAssessments', 'DecisionSignals', 'ModelSnapshots')
                """;

            var connection = _financeDbContext.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;
            if (shouldClose)
            {
                await connection.OpenAsync(ct);
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                var scalar = await command.ExecuteScalarAsync(ct);
                _analysisHistorySchemaAvailable = Convert.ToInt32(scalar) == 4;
                return _analysisHistorySchemaAvailable.Value;
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private async Task<Asset> EnsureAssetAsync(Instrument instrument, CancellationToken ct)
        {
            var normalizedSymbol = NormalizeSymbol(instrument.Symbol);

            var existing = await _financeDbContext.Assets
                .FirstOrDefaultAsync(x => x.Symbol == normalizedSymbol, ct);

            if (existing != null)
            {
                ApplyInstrument(existing, instrument);
                await _financeDbContext.SaveChangesAsync(ct);
                return existing;
            }

            var asset = new Asset
            {
                Symbol = normalizedSymbol,
                ProviderSymbol = string.IsNullOrWhiteSpace(instrument.ProviderSymbol) ? normalizedSymbol : instrument.ProviderSymbol.Trim(),
                Name = string.IsNullOrWhiteSpace(instrument.DisplayName) ? normalizedSymbol : instrument.DisplayName.Trim(),
                Exchange = instrument.MarketCode,
                Currency = string.IsNullOrWhiteSpace(instrument.CurrencyCode) ? "EUR" : instrument.CurrencyCode.Trim(),
                Country = instrument.CountryCode,
                Summary = instrument.Summary,
                LastProfileSyncUtc = instrument.LastProfileSyncUtc,
                AssetType = ParseAssetType(instrument.AssetType)
            };

            await _financeDbContext.Assets.AddAsync(asset, ct);
            await _financeDbContext.SaveChangesAsync(ct);
            return asset;
        }

        private static void ApplyInstrument(Asset asset, Instrument instrument)
        {
            asset.ProviderSymbol = string.IsNullOrWhiteSpace(asset.ProviderSymbol)
                ? (string.IsNullOrWhiteSpace(instrument.ProviderSymbol) ? asset.Symbol : instrument.ProviderSymbol.Trim())
                : asset.ProviderSymbol;
            if (string.IsNullOrWhiteSpace(asset.Name) && !string.IsNullOrWhiteSpace(instrument.DisplayName))
            {
                asset.Name = instrument.DisplayName.Trim();
            }

            if (string.IsNullOrWhiteSpace(asset.Exchange))
            {
                asset.Exchange = instrument.MarketCode;
            }

            if (string.IsNullOrWhiteSpace(asset.Currency))
            {
                asset.Currency = string.IsNullOrWhiteSpace(instrument.CurrencyCode) ? "EUR" : instrument.CurrencyCode.Trim();
            }

            if (string.IsNullOrWhiteSpace(asset.Country))
            {
                asset.Country = instrument.CountryCode;
            }

            if (string.IsNullOrWhiteSpace(asset.Summary))
            {
                asset.Summary = instrument.Summary;
            }

            if (!asset.LastProfileSyncUtc.HasValue && instrument.LastProfileSyncUtc.HasValue)
            {
                asset.LastProfileSyncUtc = instrument.LastProfileSyncUtc;
            }
        }

        private async Task<UserAsset> EnsureUserAssetAsync(string userId, string assetId, CancellationToken ct)
        {
            var userAsset = await _financeDbContext.UserAssets
                .FirstOrDefaultAsync(x => x.UserId == userId && x.AssetId == assetId, ct);

            if (userAsset != null)
            {
                return userAsset;
            }

            var created = new UserAsset
            {
                UserId = userId,
                AssetId = assetId,
                Quantity = 0m
            };

            await _financeDbContext.UserAssets.AddAsync(created, ct);
            await _financeDbContext.SaveChangesAsync(ct);
            return created;
        }

        private static string NormalizeSymbol(string? symbol)
        {
            return (symbol ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static TradingPatternEnum ParseTradingPattern(string? rawPattern)
        {
            var normalizedPattern = (rawPattern ?? string.Empty).Trim().ToUpperInvariant();
            return normalizedPattern switch
            {
                "DOUBLE_TOP" => TradingPatternEnum.DoubleTop,
                _ => throw new InvalidOperationException($"Le runtime V1 actif ne prend pas en charge le pattern {normalizedPattern}.")
            };
        }

        private static AssetTypeEnum ParseAssetType(string? assetType)
        {
            return (assetType ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "EQUITY" => AssetTypeEnum.Stock,
                "ETF" => AssetTypeEnum.Etf,
                "CRYPTO" => AssetTypeEnum.Crypto,
                _ => AssetTypeEnum.Stock
            };
        }

        private sealed class PersistedAnalysisSnapshotPayload
        {
            public string SchemaVersion { get; set; } = string.Empty;
            public string SnapshotId { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public string InstrumentId { get; set; } = string.Empty;
            public Instrument InstrumentSnapshot { get; set; } = new();
            public List<string> RequestedPatternIds { get; set; } = [];
            public List<string> ExecutedPatternIds { get; set; } = [];
            public AnalysisOutcomeEnum Outcome { get; set; }
            public DateTime RequestedAtUtc { get; set; }
            public DateTime CompletedAtUtc { get; set; }
            public DateOnly AsOfDate { get; set; }
            public string CandleInterval { get; set; } = "1d";
            public string MarketDataProviderCode { get; set; } = string.Empty;
            public DateOnly MarketDataRangeStart { get; set; }
            public DateOnly MarketDataRangeEnd { get; set; }
            public SnapshotPortfolioContextSummary PortfolioContextSnapshot { get; set; } = new();
            public PortfolioContext PortfolioContextUsed { get; set; } = new();
            public string? PrimaryPatternId { get; set; }
            public string? RecommendationId { get; set; }
            public string TraceId { get; set; } = string.Empty;
            public string AnalysisEngineVersion { get; set; } = string.Empty;
            public string? RecommendationPolicyVersion { get; set; }
            public string? ExplanationPolicyVersion { get; set; }
            public string? MarketNormalizationVersion { get; set; }
            public string PedagogicalSummary { get; set; } = string.Empty;
            public List<AnalysisSnapshotPatternRow> PatternRows { get; set; } = [];
            public PersistedAnalysisSnapshotRecommendationPayload? Recommendation { get; set; }
            public PersistedModelSnapshotPayload ModelSnapshot { get; set; } = new();
            public string RawProviderPayloadJson { get; set; } = string.Empty;
        }

        private sealed class PersistedAnalysisSnapshotRecommendationPayload
        {
            public string SnapshotRecommendationId { get; set; } = string.Empty;
            public string SnapshotId { get; set; } = string.Empty;
            public AnalysisRecommendation RecommendationPayload { get; set; } = new();
            public DateTime CreatedAtUtc { get; set; }
        }

        private sealed class PersistedModelSnapshotPayload
        {
            public ModelStatusEnum ModelStatus { get; set; }
            public string ModelMessage { get; set; } = string.Empty;
            public string ModelVersion { get; set; } = string.Empty;
            public decimal? Precision { get; set; }
            public decimal? F1 { get; set; }
            public decimal? RocAuc { get; set; }
            public int? PositiveSamples { get; set; }
            public decimal? SelectedThreshold { get; set; }
        }

        private sealed class PersistedAnalysisFailurePayload
        {
            public string SchemaVersion { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public string InstrumentId { get; set; } = string.Empty;
            public string Symbol { get; set; } = string.Empty;
            public string PatternId { get; set; } = string.Empty;
            public DateTime RequestedAtUtc { get; set; }
            public DateTime CompletedAtUtc { get; set; }
            public string ErrorType { get; set; } = string.Empty;
            public string ErrorMessage { get; set; } = string.Empty;
            public string FrontMessage { get; set; } = string.Empty;
        }
    }
}
