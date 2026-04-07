using AutoMapper;
using BackPredictFinance.Contracts.Common;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace BackPredictFinance.Services
{
    public abstract class BaseService
    {
        public ILogService _logger;
        public IMapper _mapper;
        public IServiceProvider _serviceProvider { get; }
        public IConfiguration _configuration { get; }
        public FinanceDbContext _financeDbContext { get; }
        public IHttpContextAccessor? _httpContextAccessor { get; }
        public List<IdentityUserRole<string>> _currentUserRoles { get; private set; } = [];
        public string? _currentUserId { get; private set; }
        public readonly UserManager<User> _userManager;
        public readonly RoleManager<IdentityRole> _roleManager;
        public readonly SignInManager<User> _signInManager;
        public readonly IStringLocalizer<Messages> _localizer;

        public BaseService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _financeDbContext = serviceProvider.GetRequiredService<FinanceDbContext>();
            _httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
            _mapper = serviceProvider.GetRequiredService<IMapper>();
            _logger = serviceProvider.GetRequiredService<ILogService>();
            _userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            _roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            _signInManager = serviceProvider.GetRequiredService<SignInManager<User>>();
            _localizer = serviceProvider.GetRequiredService<IStringLocalizer<Messages>>();
            SetCurrentUserId();
        }

        private void SetCurrentUserId()
        {
            if (_httpContextAccessor != null && _httpContextAccessor.HttpContext != null)
            {
                var userId = _httpContextAccessor.HttpContext.User.GetUserId();
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    _currentUserId = userId;
                    _currentUserRoles = _financeDbContext.UserRoles.Where(ur => ur.UserId == userId).ToList();
                }
            }
        }

        protected Task<User?> GetCurrentUserAsync(CancellationToken ct = default)
        {
            var userId = _currentUserId;
            return string.IsNullOrWhiteSpace(userId)
                ? Task.FromResult<User?>(null)
                : _financeDbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        }
    }
}
