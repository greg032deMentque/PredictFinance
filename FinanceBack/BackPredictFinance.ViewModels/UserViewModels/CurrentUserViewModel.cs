using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.UserViewModels
{
    public sealed class CurrentUserViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<UserRoleEnum> Roles { get; set; } = [];
        public List<UserAreaEnum> AllowedAreas { get; set; } = [];
    }
}
