using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.UserViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserName => $"{FirstName} {LastName}";
        public string NormalizedUserName { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public bool EmailConfirmed { get; set; }
        public string PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }

        public string? Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastConnection { get; set; }

        public string RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public List<UserRoleViewModel>? Roles { get; set; } = new();
        public string FullName => $"{FirstName} {LastName}";

        public bool IsActive { get; set; }
    }

    public class UserViewModelProfile : Profile
    {
        public UserViewModelProfile()
        {
            CreateMap<User, UserViewModel>();
            CreateMap<UserViewModel, User>()
                .ForMember(dest => dest.UserName, opt => opt.Ignore());
        }
    }
}
