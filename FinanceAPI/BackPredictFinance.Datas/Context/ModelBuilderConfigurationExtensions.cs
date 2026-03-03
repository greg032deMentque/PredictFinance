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
        }

        private static void ConfigureRefreshTokens(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.UserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.Property(x => x.TokenHash)
                    .HasMaxLength(512)
                    .IsRequired();

                entity.Property(x => x.ReplacedByTokenHash)
                    .HasMaxLength(512);

                entity.Property(x => x.FingerprintHash)
                    .HasMaxLength(512);

                entity.Property(x => x.DeviceId)
                    .HasMaxLength(200);

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
                entity.Property(x => x.Symbol)
                    .HasMaxLength(32)
                    .IsRequired();

                entity.HasIndex(x => x.Symbol)
                    .IsUnique();
            });

            modelBuilder.Entity<UserAsset>(entity =>
            {
                entity.HasIndex(x => new { x.UserId, x.AssetId })
                    .IsUnique();

                entity.Property(x => x.Quantity)
                    .HasPrecision(18, 8);

                entity.ToTable(tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_UserAssets_Quantity_NonNegative",
                        "[Quantity] >= 0");
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
                    tableBuilder.HasCheckConstraint(
                        "CK_AssetTransactions_Quantity_Positive",
                        "[Quantity] > 0");
                    tableBuilder.HasCheckConstraint(
                        "CK_AssetTransactions_UnitPrice_Positive",
                        "[UnitPrice] > 0");
                    tableBuilder.HasCheckConstraint(
                        "CK_AssetTransactions_Fees_NonNegative",
                        "[Fees] >= 0");
                });
            });

            modelBuilder.Entity<PriceHistory>(entity =>
            {
                entity.Property(x => x.Price).HasPrecision(18, 8);
                entity.Property(x => x.Volume).HasPrecision(18, 4);
                entity.HasIndex(x => new { x.AssetId, x.RetrievedAtUtc });

                entity.ToTable(tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_PriceHistories_Price_Positive",
                        "[Price] > 0");
                });
            });
        }

    }
}
