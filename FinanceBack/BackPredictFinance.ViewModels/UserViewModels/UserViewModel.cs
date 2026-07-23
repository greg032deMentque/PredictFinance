using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.UserViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserName => $"{FirstName} {LastName}";
        public string NormalizedUserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NormalizedEmail { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastConnection { get; set; }

        public List<UserRoleViewModel>? Roles { get; set; } = new();
        public string FullName => $"{FirstName} {LastName}";

        public bool IsActive { get; set; }
    }

    public class UserViewModelProfile : Profile
    {
        public UserViewModelProfile()
        {
            // LockoutEnd : DateTimeOffset? cote Identity (IdentityUser) vs DateTime? cote ViewModel —
            // AutoMapper ne convertit pas ce couple par convention (AssertConfigurationIsValid le signale).
            // Roles : resolu par service (UserManager.GetRolesAsync), jamais par navigation directe sur User.
            CreateMap<User, UserViewModel>()
                .ForMember(dest => dest.LockoutEnd, opt => opt.MapFrom(src => src.LockoutEnd.HasValue ? src.LockoutEnd.Value.UtcDateTime : (DateTime?)null))
                .ForMember(dest => dest.Roles, opt => opt.Ignore());
            // UserViewModel -> User : champs Identity/securite/consentement/alertes jamais ecrasables
            // depuis un formulaire d'edition de profil (geres par leurs propres flux dedies :
            // UserManager pour PasswordHash/SecurityStamp/ConcurrencyStamp, JwtGeneratorService pour
            // RefreshToken*, endpoints RGPD dedies pour les consentements, endpoints alertes pour les
            // preferences AlertX, suppression de compte pour DeletedAt).
            CreateMap<UserViewModel, User>()
                .ForMember(dest => dest.UserName, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.MapFrom(src => src.LockoutEnd.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(src.LockoutEnd.Value, DateTimeKind.Utc)) : (DateTimeOffset?)null))
                .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshTokenExpiryTime, opt => opt.Ignore())
                .ForMember(dest => dest.AlertPatternStateChangeEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.AlertLevelCrossedEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.AlertDataStaleEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.AnalyticsConsent, opt => opt.Ignore())
                .ForMember(dest => dest.MarketingEmailConsent, opt => opt.Ignore())
                .ForMember(dest => dest.ProductImprovementConsent, opt => opt.Ignore())
                .ForMember(dest => dest.ConsentLastUpdatedUtc, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UserAssets, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
                .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore());
        }
    }

}

