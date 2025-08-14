using AutoMapper;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Models;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;


namespace BackPredictFinance.Services
{
    public class Messages
    {
        // Cette classe peut rester vide.
        // Elle est utilisée pour identifier le fichier de ressources associé (Messages.resx et Messages.fr.resx).
    }

    public static class ClaimsPrincipalExtensions
    {
        public static string? GetUserId(this ClaimsPrincipal user)
        {
            return user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }


    public abstract class BaseService
    {
        public ILogService _logger;
        public IMapper _mapper;
        public IServiceProvider _serviceProvider { get; }
        public IConfiguration _configuration { get; }
        public FinanceDbContext _financeDbContext { get; }
        public IHttpContextAccessor? _httpContextAccessor { get; }
        public User? _currentUser { get; private set; }
        public List<IdentityUserRole<string>> _currentUserRoles { get; private set; }

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
                var userId = _httpContextAccessor.HttpContext?.User?.GetUserId();
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    _currentUserId = userId;
                    _currentUser = _financeDbContext.Users.FirstOrDefault(x => x.Id == userId);

                    _currentUserRoles = _financeDbContext.UserRoles
                        .Where(ur => ur.UserId == userId).ToList();

                   
                }
            }
        }

        protected async Task<User?> GetCurrentUserAsync()
        {
            if (_currentUser is null && !string.IsNullOrWhiteSpace(_currentUserId))
            {
                _currentUser = await _financeDbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == _currentUserId);
            }
            return _currentUser;
        }


        public string DoubleToString(decimal  value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }


        public static string hashPassword(string password)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12), false, hashType: HashType.SHA512);
            return passwordHash;
        }

        public DateTime StringToUTCDate(string hour)
        {
            var date = DateTime.Parse(hour, null, DateTimeStyles.AssumeUniversal);

            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

            return date;
        }

        public string UTCDateToString(DateTime date)
        {
            var hour = $"{date.Hour}:{date.Minute}";
            return hour;
        }

        public DateTime SetReservationTimes(string departureHour, DateTime date)
        {
            // Parse the hour and ensure it is in UTC
            var hour = StringToUTCDate(departureHour);

            var returnDate = new DateTime(
                date.Year,
                date.Month,
                date.Day,
                hour.Hour,
                hour.Minute,
                0,
                DateTimeKind.Utc
            );

            return returnDate;
        }

        public (DateTime date, string hour) SplitDateTime(DateTime date)
        {
            var hour = $"{date.Hour}:{date.Minute}";

            var returnDate = new DateTime(
               date.Year,
               date.Month,
               date.Day,
               date.Hour,
               date.Minute,
               0,
               DateTimeKind.Utc
           );

            return (returnDate, hour);
        }

        public static string GeneratePassword(int length = 8)
        {
            if (length < 8)
                throw new ArgumentException("Le mot de passe doit contenir au minimum 8 caractères.", nameof(length));

            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string nonAlphanumeric = "!@#$%^&*()_+-=[]{}|;:,.<>/?";

            List<char> passwordChars = new List<char>
            {
                GetRandomChar(upperCase),
                GetRandomChar(lowerCase),
                GetRandomChar(digits),
                GetRandomChar(nonAlphanumeric)
            };

            string allChars = upperCase + lowerCase + digits + nonAlphanumeric;
            int remainingLength = length - passwordChars.Count;

            for (int i = 0; i < remainingLength; i++)
            {
                passwordChars.Add(GetRandomChar(allChars));
            }

            // Mélange des caractères pour éviter toute prévisibilité.
            return new string(passwordChars.OrderBy(x => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
        }

        private static char GetRandomChar(string characters)
        {
            int index = RandomNumberGenerator.GetInt32(characters.Length);
            return characters[index];
        }


        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Rayon de la Terre en kilomètres

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        public double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }


    }
}