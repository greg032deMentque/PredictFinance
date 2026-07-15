using AutoMapper;
using BackPredictFinance.Common.Security;
using BackPredictFinance.Common.Localization;
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
    /// <summary>
    /// Fournit les dépendances techniques communes aux services du backend.
    /// </summary>
    public abstract class BaseService
    {
        protected ILogService _logger;
        protected IMapper _mapper;
        protected IServiceProvider _serviceProvider { get; }
        protected IConfiguration _configuration { get; }
        protected FinanceDbContext _financeDbContext { get; }
        protected IHttpContextAccessor? _httpContextAccessor { get; }
        protected List<IdentityUserRole<string>> _currentUserRoles { get; private set; } = [];
        protected string? _currentUserId { get; private set; }
        protected readonly UserManager<User> _userManager;
        protected readonly RoleManager<IdentityRole> _roleManager;
        protected readonly SignInManager<User> _signInManager;
        protected readonly IStringLocalizer<SharedResources> _localizer;

        protected BaseService(IServiceProvider serviceProvider)
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
            _localizer = serviceProvider.GetRequiredService<IStringLocalizer<SharedResources>>();
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
