using BackPredictFinance.Common;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.PythonServices;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using RecommendationContract = BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1.Recommendation;

namespace BackPredictFinance.Services.ClientFinanceServices.AnalysisV1
{
    public sealed class AnalysisSnapshotPersistenceService : BaseService, IAnalysisSnapshotPersistenceService
    {
        private bool? _analysisHistorySchemaAvailable;

        public AnalysisSnapshotPersistenceService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public async Task<PersistedAnalysisRecord> PersistSuccessfulAnalysisAsync(
            ResolvedAnalysisRunRequest request,
            ResolvedAnalysisPattern pattern,
            AnalysisExecutionArtifact executionArtifact,
            RecommendationContract recommendation,
            DateTime startedAtUtc,
            DateTime completedAtUtc,
            CancellationToken ct = default)
        {
            var asset = await EnsureAssetAsync(request.Symbol, request.Symbol, ct);
            var userAsset = await EnsureUserAssetAsync(request.UserId, asset.Id, ct);
            var analysisRun = await TryPersistAnalysisRunAsync(request, asset, pattern, executionArtifact, recommendation, startedAtUtc, completedAtUtc, ct);

            var legacyRecommendation = new Recommendation
            {
                UserAssetId = userAsset.Id,
                Action = MapRecommendationAction(recommendation.Kind),
                Confidence = executionArtifact.Patterns
                    .OrderByDescending(executedPattern => executedPattern.IsPrimary)
                    .ThenByDescending(executedPattern => executedPattern.Confidence)
                    .Select(executedPattern => executedPattern.Confidence)
                    .FirstOrDefault(),
                RecommendedAtUtc = executionArtifact.GeneratedAtUtc,
                Reason = recommendation.Rationale
            };

            await _financeDbContext.Set<Recommendation>().AddAsync(legacyRecommendation, ct);
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
            ResolvedAnalysisRunRequest request,
            ResolvedAnalysisPattern pattern,
            DateTime startedAtUtc,
            DateTime completedAtUtc,
            Exception exception,
            CancellationToken ct = default)
        {
            var asset = await EnsureAssetAsync(request.Symbol, request.Symbol, ct);
            await TryPersistFailedAnalysisRunAsync(request, asset, pattern, startedAtUtc, completedAtUtc, exception, ct);
        }

        private async Task<AnalysisRun?> TryPersistAnalysisRunAsync(
            ResolvedAnalysisRunRequest request,
            Asset asset,
            ResolvedAnalysisPattern pattern,
            AnalysisExecutionArtifact executionArtifact,
            RecommendationContract recommendation,
            DateTime startedAtUtc,
            DateTime completedAtUtc,
            CancellationToken ct)
        {
            if (!await CanUseAnalysisHistoryAsync(ct))
            {
                return null;
            }

            var primaryPattern = executionArtifact.Patterns
                .OrderByDescending(executedPattern => executedPattern.IsPrimary)
                .ThenByDescending(executedPattern => executedPattern.Confidence)
                .ThenByDescending(executedPattern => executedPattern.Probability)
                .FirstOrDefault();

            var analysisRun = new AnalysisRun
            {
                UserId = request.UserId,
                AssetId = asset.Id,
                RequestedPattern = ParseTradingPattern(pattern.PatternId),
                Status = "Completed",
                StartedAtUtc = startedAtUtc,
                CompletedAtUtc = completedAtUtc,
                RawPayload = JsonSerializer.Serialize(executionArtifact),
                PatternAssessments = executionArtifact.Patterns
                    .Select(BuildPersistencePatternAssessment)
                    .ToList(),
                DecisionSignal = new DecisionSignal
                {
                    Action = MapRecommendationAction(recommendation.Kind),
                    IsActionable = recommendation.Kind is BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1.RecommendationKind.Buy or BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1.RecommendationKind.Sell,
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
                    ModelVersion = string.IsNullOrWhiteSpace(executionArtifact.ModelVersion)
                        ? pattern.ModelVersion
                        : executionArtifact.ModelVersion.Trim(),
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
            ResolvedAnalysisRunRequest request,
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

            var envelope = PythonCliErrorHandling.GetOrBuildEnvelope(exception, "predict", asset.Symbol, pattern.PatternId);
            var rawPayload = PythonCliErrorHandling.TryGetSerializedEnvelope(exception) ?? PythonCliErrorHandling.SerializeEnvelope(envelope);
            var errorMessage = exception is CustomException customException && !string.IsNullOrWhiteSpace(customException.FrontMessage)
                ? customException.FrontMessage.Trim()
                : envelope.UserMessage;

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

        private async Task<Asset> EnsureAssetAsync(string symbol, string? companyName, CancellationToken ct)
        {
            var normalizedSymbol = NormalizeSymbol(symbol);

            var existing = await _financeDbContext.Assets
                .FirstOrDefaultAsync(x => x.Symbol == normalizedSymbol, ct);

            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(companyName) && string.IsNullOrWhiteSpace(existing.Name))
                {
                    existing.Name = companyName.Trim();
                    await _financeDbContext.SaveChangesAsync(ct);
                }

                return existing;
            }

            var asset = new Asset
            {
                Symbol = normalizedSymbol,
                ProviderSymbol = normalizedSymbol,
                Name = string.IsNullOrWhiteSpace(companyName) ? normalizedSymbol : companyName.Trim(),
                AssetType = AssetTypeEnum.Stock
            };

            await _financeDbContext.Assets.AddAsync(asset, ct);
            await _financeDbContext.SaveChangesAsync(ct);
            return asset;
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
            var normalized = (rawPattern ?? string.Empty).Trim().ToUpperInvariant();
            return normalized switch
            {
                "HEAD_AND_SHOULDERS" => TradingPatternEnum.HeadAndShoulders,
                "DOUBLE_TOP" => TradingPatternEnum.DoubleTop,
                "DOUBLE_BOTTOM" => TradingPatternEnum.DoubleBottom,
                "CUP_AND_HANDLE" => TradingPatternEnum.CupAndHandle,
                "TRIANGLE" => TradingPatternEnum.Triangle,
                _ => TradingPatternEnum.DoubleTop
            };
        }

        private static RecommendationActionEnum MapRecommendationAction(BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1.RecommendationKind kind)
        {
            return kind switch
            {
                BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1.RecommendationKind.Buy => RecommendationActionEnum.Buy,
                BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1.RecommendationKind.Sell => RecommendationActionEnum.Sell,
                _ => RecommendationActionEnum.Hold
            };
        }
    }
}
