using AutoMapper;
using BackPredictFinance.Datas.Entities;


namespace BackPredictFinance.ViewModels.UserViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserName { get; set; }
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
            // Entity -> ViewModel
            CreateMap<User, UserViewModel>();


            // ViewModel -> Entity
            CreateMap<UserViewModel, User>();
        }
    }

    public static class UserMapper
    {
        // Méthode d'extension pour User ? UserViewModel
        // Le 'this' devant 'User user' indique que c'est une extension :
        // on peut appeler user.ToViewModel() comme s'il s'agissait d'une méthode d'instance.
        public static UserViewModel ToViewModel(this User user)
        {
            if (user == null) return null;
            return new UserViewModel
            {
                // IdentityUser
                Id = user.Id,
                UserName = user.UserName,
                NormalizedUserName = user.NormalizedUserName,
                Email = user.Email,
                NormalizedEmail = user.NormalizedEmail,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnd = user.LockoutEnd?.DateTime,
                LockoutEnabled = user.LockoutEnabled,
                AccessFailedCount = user.AccessFailedCount,

                // Métier
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastConnection = user.LastConnection,
                RefreshToken = user.RefreshToken,
                RefreshTokenExpiryTime = user.RefreshTokenExpiryTime
            };
        }

        // Méthode d'extension pour UserViewModel ? User
        // Le 'this' devant 'UserViewModel vm' indique que c'est une extension :
        // on peut appeler vm.ToEntity() directement sur n'importe quel UserViewModel.
        public static User ToEntity(this UserViewModel vm)
        {
            if (vm == null) return null;
            return new User
            {
                // IdentityUser
                Id = vm.Id,
                UserName = vm.UserName,
                NormalizedUserName = vm.NormalizedUserName,
                Email = vm.Email,
                NormalizedEmail = vm.NormalizedEmail,
                EmailConfirmed = vm.EmailConfirmed,
                PhoneNumber = vm.PhoneNumber,
                PhoneNumberConfirmed = vm.PhoneNumberConfirmed,
                TwoFactorEnabled = vm.TwoFactorEnabled,
                LockoutEnd = vm.LockoutEnd,
                LockoutEnabled = vm.LockoutEnabled,
                AccessFailedCount = vm.AccessFailedCount,

                // Métier
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                CreatedAt = vm.CreatedAt,
                UpdatedAt = vm.UpdatedAt,
                LastConnection = vm.LastConnection,
                RefreshToken = vm.RefreshToken,
                RefreshTokenExpiryTime = vm.RefreshTokenExpiryTime
            };
        }
    }
}

