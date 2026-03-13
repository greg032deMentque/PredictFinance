using BackPredictFinance.Datas.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Datas.Context
{
    internal static class ModelBuilderConfigurationExtensions
    {
        public static void ConfiguraModels(ModelBuilder modelBuilder)
        {
            ConfigureRefreshTokens(modelBuilder);
            ConfigureDomainConstraints(modelBuilder);
            ConfigureAnalysisDomain(modelBuilder);
        }

        private static void ConfigureRefreshTokens(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
                entity.Property(x => x.TokenHash).HasMaxLength(512).IsRequired();
                entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(512);
                entity.Property(x => x.FingerprintHash).HasMaxLength(512);
                entity.Property(x => x.DeviceId).HasMaxLength(200);

                entity.HasIndex(x => x.TokenHash).IsUnique();
                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => x.ExpiresAtUtc);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureDomainConstraints(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Asset>(entity =>
            {
                entity.Property(x => x.Symbol).HasMaxLength(32).IsRequired();
                entity.Property(x => x.ProviderSymbol).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Exchange).HasMaxLength(32);
                entity.Property(x => x.Currency).HasMaxLength(12).IsRequired();
                entity.Property(x => x.Country).HasMaxLength(64);
                entity.Property(x => x.Sector).HasMaxLength(128);
                entity.Property(x => x.Category).HasMaxLength(128);
                entity.Property(x => x.Summary).HasMaxLength(4000);

                entity.HasIndex(x => x.Symbol).IsUnique();
            });

            modelBuilder.Entity<UserAsset>(entity =>
            {
                entity.HasIndex(x => new { x.UserId, x.AssetId }).IsUnique();
                entity.Property(x => x.Quantity).HasPrecision(18, 8);

                entity.ToTable(tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint("CK_UserAssets_Quantity_NonNegative", "[Quantity] >= 0");
                });
            });

            modelBuilder.Entity<AssetTransaction>(entity =>
            {
                entity.HasIndex(x => new { x.UserAssetId, x.TimestampUtc });
                entity.Property(x => x.Quantity).HasPrecision(18, 8);
                entity.Property(x => x.UnitPrice).HasPrecision(18, 8);
                entity.Property(x => x.Fees).HasPrecision(18, 8);

                entity.ToTable(tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint("CK_AssetTransactions_Quantity_Positive", "[Quantity] > 0");
                    tableBuilder.HasCheckConstraint("CK_AssetTransactions_UnitPrice_Positive", "[UnitPrice] > 0");
                    tableBuilder.HasCheckConstraint("CK_AssetTransactions_Fees_NonNegative", "[Fees] >= 0");
                });
            });

            modelBuilder.Entity<PriceHistory>(entity =>
            {
                entity.Property(x => x.Price).HasPrecision(18, 8);
                entity.Property(x => x.Volume).HasPrecision(18, 4);
                entity.HasIndex(x => new { x.AssetId, x.RetrievedAtUtc });

                entity.ToTable(tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint("CK_PriceHistories_Price_Positive", "[Price] > 0");
                });
            });

            modelBuilder.Entity<AssetQuoteSnapshot>(entity =>
            {
                entity.Property(x => x.LastPrice).HasPrecision(18, 8);
                entity.Property(x => x.DayVariationPct).HasPrecision(9, 4);
                entity.Property(x => x.Source).HasMaxLength(64).IsRequired();
                entity.HasIndex(x => new { x.AssetId, x.AsOfUtc });
            });

            modelBuilder.Entity<AssetCandleSnapshot>(entity =>
            {
                entity.Property(x => x.Interval).HasMaxLength(16).IsRequired();
                entity.Property(x => x.Source).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Open).HasPrecision(18, 8);
                entity.Property(x => x.High).HasPrecision(18, 8);
                entity.Property(x => x.Low).HasPrecision(18, 8);
                entity.Property(x => x.Close).HasPrecision(18, 8);
                entity.Property(x => x.Volume).HasPrecision(18, 4);
                entity.HasIndex(x => new { x.AssetId, x.Interval, x.TimestampUtc }).IsUnique();
            });
        }

        private static void ConfigureAnalysisDomain(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AnalysisBatch>(entity =>
            {
                entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
                entity.Property(x => x.ErrorMessage).HasMaxLength(1024);
                entity.HasIndex(x => new { x.UserId, x.RequestedAtUtc });
            });

            modelBuilder.Entity<AnalysisRun>(entity =>
            {
                entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
                entity.Property(x => x.RawPayload).HasMaxLength(32000);
                entity.Property(x => x.ErrorMessage).HasMaxLength(2048);
                entity.HasIndex(x => new { x.UserId, x.StartedAtUtc });
                entity.HasOne(x => x.AnalysisBatch)
                    .WithMany(x => x.Runs)
                    .HasForeignKey(x => x.AnalysisBatchId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<PatternAssessment>(entity =>
            {
                entity.Property(x => x.Phase).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Probability).HasPrecision(9, 6);
                entity.Property(x => x.Confidence).HasPrecision(9, 6);
                entity.Property(x => x.CurrentPrice).HasPrecision(18, 8);
                entity.Property(x => x.NecklinePrice).HasPrecision(18, 8);
                entity.Property(x => x.TargetPrice).HasPrecision(18, 8);
                entity.Property(x => x.InvalidationPrice).HasPrecision(18, 8);
                entity.HasIndex(x => new { x.AnalysisRunId, x.IsPrimary });
            });

            modelBuilder.Entity<DecisionSignal>(entity =>
            {
                entity.Property(x => x.Confidence).HasPrecision(9, 6);
                entity.Property(x => x.Reason).HasMaxLength(2048).IsRequired();
                entity.HasIndex(x => x.AnalysisRunId).IsUnique();
            });

            modelBuilder.Entity<ModelSnapshot>(entity =>
            {
                entity.Property(x => x.ModelVersion).HasMaxLength(256).IsRequired();
                entity.Property(x => x.ModelMessage).HasMaxLength(2048).IsRequired();
                entity.Property(x => x.Precision).HasPrecision(9, 6);
                entity.Property(x => x.F1).HasPrecision(9, 6);
                entity.Property(x => x.RocAuc).HasPrecision(9, 6);
                entity.Property(x => x.SelectedThreshold).HasPrecision(9, 6);
                entity.HasIndex(x => x.AnalysisRunId).IsUnique();
            });
        }
    }
}
