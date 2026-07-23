using BackPredictFinance.Patterns.Contracts;
using BackPredictFinance.Common;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Patterns;
using BackPredictFinance.Datas.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using BackPredictFinance.Common.AnalysisV1;
using AnalysisRecommendation = BackPredictFinance.Common.AnalysisV1.AnalysisRecommendation;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Services.TwelveDataServices;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    /// <summary>
    /// Persiste les snapshots d'analyse et les échecs d'exécution pour l'historique.
    /// </summary>
    public interface IAnalysisSnapshotPersistenceService
    {
        /// <summary>
        /// Persiste une analyse réussie et retourne le record public associé.
        /// </summary>
        Task<PersistedAnalysisRecord> PersistSuccessfulAnalysisAsync(
            AnalysisRequest request,
            ResolvedAnalysisPattern pattern,
            AnalysisExecutionArtifact executionArtifact,
            AnalysisRecommendation recommendation,
            AnalysisOutcome outcome,
            string pedagogicalSummary,
            string explanationPolicyVersion,
            DateTime startedAtUtc,
            DateTime completedAtUtc,
            CancellationToken ct = default);

        /// <summary>
        /// Persiste la trace d'une analyse échouée.
        /// </summary>
        Task PersistFailedAnalysisAsync(
            AnalysisRequest request,
            ResolvedAnalysisPattern pattern,
            DateTime startedAtUtc,
            DateTime completedAtUtc,
            Exception exception,
            CancellationToken ct = default);

        /// <summary>
        /// Upsert de bougies 1d pour un actif déjà connu — utilisé par le job EOD de rafraîchissement.
        /// </summary>
        Task UpsertCandlesForRefreshAsync(string assetId, IReadOnlyList<TickerCandle> candles, CancellationToken ct = default);
    }

    /// <summary>
    /// Implémente l'écriture de l'historique d'analyse et de ses artefacts persistés.
    /// </summary>
    public sealed class AnalysisSnapshotPersistenceService : BaseService, IAnalysisSnapshotPersistenceService
    {
        private const string SnapshotPayloadVersion = "analysis-snapshot-history@prompt5";
        private const string MarketDataProviderCode = "YAHOO_FINANCE";
        private readonly IFundamentalsProvider _fundamentalsProvider;
        private bool? _analysisHistorySchemaAvailable;

        public AnalysisSnapshotPersistenceService(IServiceProvider serviceProvider, IFundamentalsProvider fundamentalsProvider)
            : base(serviceProvider)
        {
            _fundamentalsProvider = fundamentalsProvider;
        }

        // Upsert en 1 SELECT groupé + staging dans le tracker (pas de SaveChanges ici) : l'appelant
        // committe l'ensemble (bougies + éventuelle AnalysisRun associée) en un seul SaveChangesAsync,
        // dans une transaction explicite côté appelant. Ne jamais réintroduire un ChangeTracker.Clear()
        // ici : ce contexte peut porter une AnalysisRun non encore sauvegardée (voir TryPersistAnalysisRunAsync).
        private async Task UpsertCandlesAsync(
            string assetId,
            IReadOnlyList<TickerCandle> candles,
            CancellationToken ct)
        {
            var normalizedCandles = candles
                .Select(candle => (Candle: candle, TimestampUtc: NormalizeCandleTimestampUtc(candle.Date)))
                .ToList();

            var timestamps = normalizedCandles.Select(x => x.TimestampUtc).ToList();

            var existingSnapshots = await _financeDbContext.AssetCandleSnapshots
                .Where(x => x.AssetId == assetId && x.Interval == "1d" && timestamps.Contains(x.TimestampUtc))
                .ToDictionaryAsync(x => x.TimestampUtc, ct);

            foreach (var (candle, candleTimestampUtc) in normalizedCandles)
            {
                if (existingSnapshots.TryGetValue(candleTimestampUtc, out var existing))
                {
                    existing.Open = candle.Open;
                    existing.High = candle.High;
                    existing.Low = candle.Low;
                    existing.Close = candle.Close;
                    existing.Volume = candle.Volume;
                }
                else
                {
                    _financeDbContext.AssetCandleSnapshots.Add(
                        new AssetCandleSnapshot
                        {
                            AssetId = assetId,
                            Interval = "1d",
                            TimestampUtc = candleTimestampUtc,
                            Open = candle.Open,
                            High = candle.High,
                            Low = candle.Low,
                            Close = candle.Close,
                            Volume = candle.Volume,
                            Source = "PIPELINE"
                        });
                }
            }
        }

        public async Task<PersistedAnalysisRecord> PersistSuccessfulAnalysisAsync(
            AnalysisRequest request,
            ResolvedAnalysisPattern pattern,
            AnalysisExecutionArtifact executionArtifact,
            AnalysisRecommendation recommendation,
            AnalysisOutcome outcome,
            string pedagogicalSummary,
            string explanationPolicyVersion,
            DateTime startedAtUtc,
            DateTime completedAtUtc,
            CancellationToken ct = default)
        {
            var asset = await EnsureAssetAsync(request.Instrument, ct);
            await EnsureUserAssetAsync(request.UserId, asset.Id, ct);
            var analysisRun = await TryPersistAnalysisRunAsync(request, asset, pattern, executionArtifact, recommendation, outcome, pedagogicalSummary, explanationPolicyVersion, startedAtUtc, completedAtUtc, ct);

            return new PersistedAnalysisRecord
            {
                PublicId = analysisRun?.Id ?? Guid.NewGuid().ToString("N"),
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
                Summary = asset.Summary,
                EarningsDateUtc = analysisRun?.DecisionSignal?.EarningsDateUtc
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

        public async Task UpsertCandlesForRefreshAsync(string assetId, IReadOnlyList<TickerCandle> candles, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(assetId) || candles.Count == 0)
            {
                return;
            }

            await UpsertCandlesAsync(assetId, candles, ct);
            await _financeDbContext.SaveChangesAsync(ct);
        }

        private async Task<AnalysisRun?> TryPersistAnalysisRunAsync(
            AnalysisRequest request,
            Asset asset,
            ResolvedAnalysisPattern pattern,
            AnalysisExecutionArtifact executionArtifact,
            AnalysisRecommendation recommendation,
            AnalysisOutcome outcome,
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
            var earningsDateUtc = await TryResolveEarningsDateUtcAsync(asset.Symbol, ct);

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
                Status = AnalysisRunStatusEnum.Completed,
                StartedAtUtc = startedAtUtc,
                CompletedAtUtc = completedAtUtc,
                // Piège connu : RawPayload doit être sérialisé avec EXACTEMENT les mêmes
                // JsonSerializerOptions que celles utilisées côté lecture
                // (AnalysisResultProjectionService.TryMapAnalysisRunResult). Les deux services
                // partagent désormais AnalysisSnapshotJsonOptions.Shared (avec JsonStringEnumConverter)
                // précisément parce qu'avant ce partage, la persistance sérialisait les enums
                // (Outcome, ModelStatus, etc.) en chaînes tandis que la projection désérialisait avec
                // des options par défaut (enums en entiers) : toute valeur d'enum non numérique dans
                // RawPayload faisait alors échouer la désérialisation (JsonException silencieusement
                // avalée par le try/catch de la projection, snapshot ignoré). Ne jamais réintroduire
                // une instance de JsonSerializerOptions locale à ce service pour RawPayload.
                RawPayload = JsonSerializer.Serialize(snapshotPayload, AnalysisSnapshotJsonOptions.Shared),
                PatternAssessments = executionArtifact.Patterns
                    .Select(BuildPersistencePatternAssessment)
                    .ToList(),
                DecisionSignal = new DecisionSignal
                {
                    Action = MapRecommendationAction(recommendation.Kind),
                    IsActionable = recommendation.Kind is RecommendationKind.Buy
                        or RecommendationKind.Reinforce
                        or RecommendationKind.Lighten
                        or RecommendationKind.Sell,
                    Confidence = primaryPattern?.Confidence ?? 0m,
                    HorizonDays = recommendation.ReviewHorizonDays ?? 0,
                    Reason = string.IsNullOrWhiteSpace(recommendation.Rationale) ? "Aucune justification" : recommendation.Rationale.Trim(),
                    EarningsDateUtc = earningsDateUtc
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

            await using var transaction = await _financeDbContext.Database.BeginTransactionAsync(ct);

            await _financeDbContext.AnalysisRuns.AddAsync(analysisRun, ct);
            if (executionArtifact.Candles.Count > 0)
            {
                await UpsertCandlesAsync(asset.Id, executionArtifact.Candles, ct);
            }
            await _financeDbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return analysisRun;
        }

        private async Task<DateTime?> TryResolveEarningsDateUtcAsync(string symbol, CancellationToken ct)
        {
            try
            {
                var fundamentals = await _fundamentalsProvider.GetFundamentalsAsync(symbol, ct);
                return fundamentals.EarningsDate;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"AnalysisSnapshotPersistenceService.TryResolveEarningsDateUtcAsync: calendrier de resultats indisponible pour {symbol} ({ex.GetType().Name})", ex);
                return null;
            }
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
                Status = AnalysisRunStatusEnum.Failed,
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

            // Même règle que pour le payload de succès : options partagées obligatoires (voir
            // TryPersistAnalysisRunAsync) pour rester compatible avec la désérialisation côté projection.
            return JsonSerializer.Serialize(payload, AnalysisSnapshotJsonOptions.Shared);
        }

        private static Datas.Entities.PatternAssessment BuildPersistencePatternAssessment(ExecutedPatternArtifact executedPattern)
        {
            return new BackPredictFinance.Datas.Entities.PatternAssessment
            {
                PatternId = executedPattern.PatternId,
                Phase = executedPattern.Phase,
                ProgressStatus = MapPatternStatus(executedPattern.ContractAssessment.Detection.Status),
                Direction = executedPattern.ContractAssessment.Direction,
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

        private static PatternProgressStatusEnum MapPatternStatus(PatternStatus status)
        {
            return status switch
            {
                PatternStatus.Forming => PatternProgressStatusEnum.Forming,
                PatternStatus.Monitoring => PatternProgressStatusEnum.Monitoring,
                PatternStatus.Confirmed => PatternProgressStatusEnum.Confirmed,
                PatternStatus.Invalidated => PatternProgressStatusEnum.Invalidated,
                PatternStatus.Completed => PatternProgressStatusEnum.Completed,
                _ => PatternProgressStatusEnum.Absent
            };
        }

        private static PersistedAnalysisSnapshotPayload BuildSuccessfulSnapshotPayload(
            AnalysisRequest request,
            Asset asset,
            ResolvedAnalysisPattern pattern,
            AnalysisExecutionArtifact executionArtifact,
            AnalysisRecommendation recommendation,
            AnalysisOutcome outcome,
            string pedagogicalSummary,
            string explanationPolicyVersion,
            DateTime startedAtUtc,
            DateTime completedAtUtc)
        {
            // Ordre d'affichage persisté : le pattern marqué IsPrimary passe toujours en tête, puis
            // tri par confiance décroissante, puis par probabilité décroissante en cas d'égalité de
            // confiance. Cet ordre est celui que la projection réutilisera tel quel (DisplayRank).
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

            var recommendationSnapshot = new AnalysisSnapshotRecommendation
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
                CurrencyCode = portfolioContext.CurrencyCode,
                HasDataIntegrityWarning = portfolioContext.HasDataIntegrityWarning
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
                LatestOpenBuyDate = portfolioContext.LatestOpenBuyDate,
                HasDataIntegrityWarning = portfolioContext.HasDataIntegrityWarning
            };
        }

        // Détecte si le schéma d'historique d'analyse (4 tables) est déployé, pour permettre à ce
        // service de fonctionner en environnement où la migration n'a pas encore été appliquée
        // (dégradation silencieuse : PersistSuccessfulAnalysisAsync retourne alors un record avec un
        // PublicId généré à la volée, sans persistance réelle). Résultat mis en cache sur la durée de
        // vie du service (scoped) pour éviter une requête INFORMATION_SCHEMA à chaque analyse.
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
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.AssetId == assetId, ct);

            if (userAsset != null)
            {
                if (userAsset.IsDeleted)
                {
                    userAsset.IsDeleted = false;
                    await _financeDbContext.SaveChangesAsync(ct);
                }

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

        private static DateTime NormalizeCandleTimestampUtc(DateTime candleTimestamp)
        {
            return candleTimestamp.Kind switch
            {
                DateTimeKind.Utc => candleTimestamp,
                DateTimeKind.Unspecified => DateTime.SpecifyKind(candleTimestamp, DateTimeKind.Utc),
                _ => candleTimestamp.ToUniversalTime()
            };
        }

        private static RecommendationActionEnum MapRecommendationAction(RecommendationKind kind)
        {
            return kind switch
            {
                RecommendationKind.Buy => RecommendationActionEnum.Buy,
                RecommendationKind.Reinforce => RecommendationActionEnum.Buy,
                RecommendationKind.Lighten => RecommendationActionEnum.Sell,
                RecommendationKind.Sell => RecommendationActionEnum.Sell,
                _ => RecommendationActionEnum.Hold
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
            public AnalysisOutcome Outcome { get; set; }
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
            public AnalysisSnapshotRecommendation? Recommendation { get; set; }
            public PersistedModelSnapshotPayload ModelSnapshot { get; set; } = new();
            public string RawProviderPayloadJson { get; set; } = string.Empty;
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
