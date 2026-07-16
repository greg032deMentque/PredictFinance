using BackPredictFinance.Datas.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BackPredictFinance.Datas.Context
{
    public class FinanceDbContext : IdentityDbContext<User>
    {
        private readonly HttpContext? _context;

        public string CurrentUserId { get; private set; } = string.Empty;
        public User? CurrentUser { get; private set; }

        public FinanceDbContext(DbContextOptions<FinanceDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _context = httpContextAccessor?.HttpContext;

            if (_context != null)
            {
                CurrentUserId = ResolveCurrentUserId(_context.User) ?? string.Empty;
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
        public DbSet<Analytic> Analytics { get; set; }
        #endregion

        #region User Data
        public new DbSet<IdentityUserRole<string>> UserRoles { get; set; } = null!;
        public new DbSet<IdentityRole> Roles { get; set; } = null!;
        public DbSet<UserAsset> UserAssets { get; set; } = null!;
        public DbSet<Portfolio> Portfolios { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<UserScreenerPreset> UserScreenerPresets { get; set; } = null!;

        #endregion

        #region Finance
        public DbSet<Asset> Assets { get; set; } = null!;
        public DbSet<Recommendation> Recommendations { get; set; } = null!;
        public DbSet<PriceHistory> PriceHistories { get; set; } = null!;
        public DbSet<AssetTransaction> AssetTransactions { get; set; } = null!;
        public DbSet<AssetQuoteSnapshot> AssetQuoteSnapshots { get; set; } = null!;
        public DbSet<AssetFundamentalsSnapshot> AssetFundamentalsSnapshots { get; set; } = null!;
        public DbSet<AssetCandleSnapshot> AssetCandleSnapshots { get; set; } = null!;
        public DbSet<AnalysisRun> AnalysisRuns { get; set; } = null!;
        public DbSet<PatternAssessment> PatternAssessments { get; set; } = null!;
        public DbSet<DecisionSignal> DecisionSignals { get; set; } = null!;
        public DbSet<ModelSnapshot> ModelSnapshots { get; set; } = null!;
        public DbSet<AssetPeaEligibility> AssetPeaEligibilities { get; set; } = null!;
        public DbSet<ParameterDictionaryEntry> ParameterDictionaryEntries { get; set; } = null!;
        public DbSet<RecommendationWordingVersion> RecommendationWordingVersions { get; set; } = null!;
        public DbSet<FundamentalScoringPolicyVersion> FundamentalScoringPolicyVersions { get; set; } = null!;
        public DbSet<RecommendationWordingScenario> RecommendationWordingScenarios { get; set; } = null!;
        public DbSet<UserNotification> UserNotifications { get; set; } = null!;
        public DbSet<SignalOutcome> SignalOutcomes { get; set; } = null!;
        public DbSet<PatternDefinition> PatternDefinitions { get; set; } = null!;
        public DbSet<AnalysisConceptExplanation> AnalysisConceptExplanations { get; set; } = null!;
        #endregion

        #region Education
        public DbSet<EducationArticle> EducationArticles { get; set; } = null!;
        public DbSet<GlossaryTerm> GlossaryTerms { get; set; } = null!;
        #endregion

        #region Content
        public DbSet<FaqEntry> FaqEntries { get; set; } = null!;
        public DbSet<LegalCard> LegalCards { get; set; } = null!;
        public DbSet<LearnTopic> LearnTopics { get; set; } = null!;
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ModelBuilderConfigurationExtensions.ConfiguraModels(modelBuilder);
           
        }


        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetAuditableProperties();

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

        private static string? ResolveCurrentUserId(ClaimsPrincipal? user)
        {
            return user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}

