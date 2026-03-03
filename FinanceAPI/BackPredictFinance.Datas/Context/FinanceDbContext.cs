using BackPredictFinance.Datas.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Security.Claims;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace BackPredictFinance.Datas.Context
{
    public class FinanceDbContext : IdentityDbContext<User>
    {
        private readonly HttpContext _context;

        public string CurrentUserId { get; private set; }
        public User CurrentUser { get; private set; }

        public FinanceDbContext(DbContextOptions<FinanceDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _context = httpContextAccessor?.HttpContext;

            if (_context != null)
            {
                CurrentUserId = _context.User.FindFirst(ClaimTypes.Sid)?.Value;
                if (!string.IsNullOrWhiteSpace(CurrentUserId))
                {
                    CurrentUser = Users.AsNoTracking().FirstOrDefault(u => u.Id == CurrentUserId);
                }
                else
                {
                    CurrentUserId = "Unkwon user";
                }
            }
        }


        // Audit & Analytics
        #region Audit & Analytics
        public DbSet<AuditTrail> AuditTrails { get; set; } = null!;
        public DbSet<Analytic> Analytics { get; set; }
        #endregion

        #region User Data
        public DbSet<IdentityUserRole<string>> UserRoles { get; set; }
        public DbSet<IdentityRole> Roles { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<UserAsset> UserAssets { get; set; }

        #endregion

        #region Finance
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetTransaction> AssetTransactions { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<MarketPrice> MarketPrices { get; set; }
        public DbSet<Portfolio> Portfolios { get; set; }
        public DbSet<PriceAlert> PriceAlerts { get; set; }

        public DbSet<PriceHistory> PriceHistories { get; set; }
        public DbSet<IAModelVersion> IAModelVersions { get; set; }

        


        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ModelBuilderConfigurationExtensions.ConfiguraModels(modelBuilder);
           
        }


        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetAuditableProperties();

            /*
            var auditEntries = HandleAuditingBeforeSaveChanges(CurrentUserId).ToList();

            if (auditEntries.Any())
            {
                await AuditTrails.AddRangeAsync(auditEntries, cancellationToken);
            }

            */

            return await base.SaveChangesAsync(cancellationToken);
        }

        private void SetAuditableProperties()
        {
            foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
            {
                if (entry.Entity.GetType().Name.Contains("Analytic", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
                        break;
                }
            }
        }

        /*
        private IEnumerable<AuditTrail> HandleAuditingBeforeSaveChanges(string? userId)
        {
            foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
            {
                if (entry.Entity.GetType().Name.Contains("Analytic", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                {
                    yield return CreateTrailEntry(userId, entry);
                }
            }
        }

        private static AuditTrail CreateTrailEntry(string? userId, EntityEntry<IAuditableEntity> entry)
        {
            var trailEntry = new AuditTrail
            {
                Id = Guid.NewGuid().ToString(),
                EntityName = entry.Entity.GetType().Name,
                UserId = userId,
                DateUtc = DateTime.UtcNow,
                OldValues = "{}",
                NewValues = "{}",
                EntityStateEnum = entry.State
            };

            var oldValuesDict = new Dictionary<string, object?>();
            var newValuesDict = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsPrimaryKey())
                {
                    trailEntry.PrimaryKey = property.CurrentValue?.ToString();
                    continue;
                }

                if (!property.IsModified || property.Metadata.Name == "PasswordHash")
                {
                    continue;
                }

                var original = entry.GetDatabaseValues()?.GetValue<object>(property.Metadata.Name);
                var current = property.CurrentValue;

                if ((original == null && current != null) || (original != null && !original.Equals(current)))
                {
                    trailEntry.ChangedColumns.Add(property.Metadata.Name);
                    oldValuesDict[property.Metadata.Name] = original;
                    newValuesDict[property.Metadata.Name] = current;
                }
            }

            trailEntry.OldValues = JsonSerializer.Serialize(oldValuesDict);
            trailEntry.NewValues = JsonSerializer.Serialize(newValuesDict);

            return trailEntry;
        }
        */

    }
}