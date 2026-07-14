using Microsoft.AspNetCore.Identity;

namespace BackPredictFinance.Datas.Entities
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastConnection { get; set; }

        public string RefreshToken { get; set; } = string.Empty;
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public bool IsActive { get; set; }

        public bool AlertPatternStateChangeEnabled { get; set; } = true;
        public bool AlertLevelCrossedEnabled { get; set; } = true;
        public bool AlertDataStaleEnabled { get; set; } = true;

        public bool AnalyticsConsent { get; set; } = false;
        public bool MarketingEmailConsent { get; set; } = false;
        public bool ProductImprovementConsent { get; set; } = false;
        public DateTime? ConsentLastUpdatedUtc { get; set; }
        public DateTime? DeletedAt { get; set; }

        public List<UserAsset>? UserAssets { get; set; }
    }
}
