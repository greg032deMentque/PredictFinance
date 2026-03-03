using BackPredictFinance.Common.Jwt;
using BackPredictFinance.Datas.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace BackPredictFinance.Services.AuthServices
{
    public sealed record RefreshTokenResult(string Token, DateTime ExpiresUtc);

    public interface IJwtGeneratorService
    {
        public Task<string> GenerateJwtToken(User user);

        Task<RefreshTokenResult> GenerateUserRefreshToken(User user, string? deviceId = null, CancellationToken ct = default);
        Task<(string accessToken, RefreshTokenResult refresh)> RotateRefresh(string presentedRefresh, string? deviceId = null, CancellationToken ct = default);
    }

    public class JwtGeneratorService : BaseService, IJwtGeneratorService
    {
        private readonly JWTToken _userJwt;
        public JwtGeneratorService(
        IOptions<JWTToken> jwtOptions,
        IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _userJwt = jwtOptions.Value ?? throw new InvalidOperationException("JWT options are missing.");
        }

        public async Task<string> GenerateJwtToken(User user)
        {
            var now = DateTime.UtcNow;
            var ttl = Math.Clamp(_userJwt.ValidityMinutesAcessToken > 0 ? _userJwt.ValidityMinutesAcessToken : 15, 5, 60);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_userJwt.Secret));

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new("amr", "pwd")
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles) 
                claims.Add(new Claim(ClaimTypes.Role, role));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            };

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                IssuedAt = now,
                NotBefore = now.AddSeconds(-10),
                Expires = now.AddMinutes(ttl),
                Issuer = _userJwt.Issuer,
                Audience = _userJwt.Audience,
                SigningCredentials = creds,
                /*
                AdditionalHeaderClaims = new Dictionary<string, object>
                {
                    ["typ"] = "at+jwt",
                    ["kid"] = _userJwt.KeyId
                }
                */
            };

            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(descriptor));
        }


        public async Task<RefreshTokenResult> GenerateUserRefreshToken(User user, string? deviceId = null, CancellationToken ct = default)
        {
            var random = RandomNumberGenerator.GetBytes(64);
            var token = Convert.ToBase64String(random);
            var expires = DateTime.UtcNow.AddMinutes(Math.Max(_userJwt.ValidityMinutesRefreshToken, 60));

            var hash = HashRefresh(token);
            _financeDbContext.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = hash,             
                DeviceId = deviceId,
                ExpiresAtUtc = expires,        
                CreatedAtUtc = DateTime.UtcNow
            });
            await _financeDbContext.SaveChangesAsync(ct);

            return new RefreshTokenResult(token, expires);
        }

        public async Task<(string accessToken, RefreshTokenResult refresh)> RotateRefresh(string presentedRefresh, string? deviceId = null, CancellationToken ct = default)
        {
            var rec = await _financeDbContext.RefreshTokens
            .SingleOrDefaultAsync(r => r.TokenHash == HashRefresh(presentedRefresh), ct);

            if (rec is null || rec.RevokedAtUtc != null || rec.ExpiresAtUtc < DateTime.UtcNow)
                throw new SecurityException("invalid refresh");

            if (!string.IsNullOrWhiteSpace(rec.ReplacedByTokenHash))
            {
                await RevokeChainAsync(rec, ct);
                throw new SecurityException("refresh replay detected");
            }

            rec.RevokedAtUtc = DateTime.UtcNow; 
            var user = await _userManager.FindByIdAsync(rec.UserId) ?? throw new SecurityException("user missing");
            var newRefresh = await GenerateUserRefreshToken(user, deviceId, ct);

            var newHash = HashRefresh(newRefresh.Token);
            rec.ReplacedByTokenHash = newHash; 

            await _financeDbContext.SaveChangesAsync(ct);

            var access = await GenerateJwtToken(user);
            return (access, newRefresh);

        }

        private async Task RevokeChainAsync(RefreshToken head, CancellationToken ct)
        {
            var current = head;
            while (!string.IsNullOrWhiteSpace(current.ReplacedByTokenHash))
            {
                var nextHash = current.ReplacedByTokenHash!;
                var next = await _financeDbContext.RefreshTokens
                    .SingleOrDefaultAsync(r => r.TokenHash == nextHash, ct);
                if (next is null) break;
                if (next.RevokedAtUtc == null) next.RevokedAtUtc = DateTime.UtcNow;
                current = next;
            }
            await _financeDbContext.SaveChangesAsync(ct);
        }


        private string HashRefresh(string presented)
        {
            var configuredSalt = _configuration["ServerSalt"];
            byte[] salt;

            if (!string.IsNullOrWhiteSpace(configuredSalt))
            {
                try
                {
                    salt = Convert.FromBase64String(configuredSalt);
                }
                catch (FormatException)
                {
                    salt = SHA256.HashData(Encoding.UTF8.GetBytes(_userJwt.Secret));
                }
            }
            else
            {
                salt = SHA256.HashData(Encoding.UTF8.GetBytes(_userJwt.Secret));
            }

            using var pbkdf2 = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(presented), salt, 100_000, HashAlgorithmName.SHA256);
            return Convert.ToBase64String(pbkdf2.GetBytes(32));
        }

    }

}


