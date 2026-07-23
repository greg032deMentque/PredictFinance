using BackPredictFinance.Common.Jwt;
using BackPredictFinance.Datas.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


using BackPredictFinance.Common.Auth;

namespace BackPredictFinance.Services.AuthServices
{
    /// <summary>
    /// Gère la génération et la révocation des jetons JWT et refresh tokens.
    /// </summary>
    public interface IJwtGeneratorService
    {
        /// <summary>
        /// Génère un jeton d'accès JWT pour un utilisateur.
        /// </summary>
        public Task<string> GenerateJwtToken(User user);

        /// <summary>
        /// Crée un refresh token persistant pour un utilisateur.
        /// </summary>
        Task<RefreshTokenResult> GenerateUserRefreshToken(User user, string? deviceId = null, CancellationToken ct = default);
        /// <summary>
        /// Effectue la rotation d'un refresh token présenté et retourne de nouveaux jetons.
        /// </summary>
        Task<(string accessToken, RefreshTokenResult refresh)> RotateRefresh(string presentedRefresh, string? deviceId = null, CancellationToken ct = default);
        /// <summary>
        /// Révoque un refresh token présenté.
        /// </summary>
        Task RevokeRefreshAsync(string presentedRefresh, CancellationToken ct = default);
    }

    /// <summary>
    /// Implémente l'émission des JWT et la gestion persistée des refresh tokens.
    /// </summary>
    public class JwtGeneratorService : BaseService, IJwtGeneratorService
    {
        private readonly JWTToken _userJwt;
        private readonly byte[] _refreshTokenHmacKey;

        public JwtGeneratorService(
        IOptions<JWTToken> jwtOptions,
        IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _userJwt = jwtOptions.Value ?? throw new InvalidOperationException("JWT options are missing.");

            var hmacKeyConfig = _configuration["Security:RefreshTokenHmacKey"];
            if (string.IsNullOrWhiteSpace(hmacKeyConfig))
            {
                throw new InvalidOperationException("Security:RefreshTokenHmacKey is missing.");
            }

            _refreshTokenHmacKey = Encoding.UTF8.GetBytes(hmacKeyConfig);
        }

        public async Task<string> GenerateJwtToken(User user)
        {
            var now = DateTime.UtcNow;
            var ttl = Math.Clamp(_userJwt.ValidityMinutesAcessToken > 0 ? _userJwt.ValidityMinutesAcessToken : 15, 5, 60);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_userJwt.Secret));
            var securityStamp = await _userManager.GetSecurityStampAsync(user);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new("amr", "pwd"),
                new(JwtClaimTypes.SecurityStamp, securityStamp)
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
            };

            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(descriptor));
        }


        public async Task<RefreshTokenResult> GenerateUserRefreshToken(User user, string? deviceId = null, CancellationToken ct = default)
        {
            var random = RandomNumberGenerator.GetBytes(64);
            var token = Convert.ToBase64String(random);
            var expires = DateTime.UtcNow.AddMinutes(Math.Max(_userJwt.ValidityMinutesRefreshToken, 60));

            var hash = EncodeHash(ComputeRefreshHmac(token));
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
            var computedHash = EncodeHash(ComputeRefreshHmac(presentedRefresh));
            var rec = await _financeDbContext.RefreshTokens
            .SingleOrDefaultAsync(r => r.TokenHash == computedHash, ct);

            if (rec is null || !VerifyRefreshHash(presentedRefresh, rec.TokenHash) || rec.RevokedAtUtc != null || rec.ExpiresAtUtc < DateTime.UtcNow)
                throw new SecurityException("invalid refresh");

            if (!string.IsNullOrWhiteSpace(rec.ReplacedByTokenHash))
            {
                await RevokeChainAsync(rec, ct);
                throw new SecurityException("refresh replay detected");
            }

            rec.RevokedAtUtc = DateTime.UtcNow; 
            var user = await _userManager.FindByIdAsync(rec.UserId) ?? throw new SecurityException("user missing");
            if (!user.IsActive)
            {
                await _financeDbContext.SaveChangesAsync(ct);
                throw new SecurityException("inactive user");
            }

            var newRefresh = await GenerateUserRefreshToken(user, deviceId, ct);

            var newHash = EncodeHash(ComputeRefreshHmac(newRefresh.Token));
            rec.ReplacedByTokenHash = newHash;

            await _financeDbContext.SaveChangesAsync(ct);

            var access = await GenerateJwtToken(user);
            return (access, newRefresh);

        }

        public async Task RevokeRefreshAsync(string presentedRefresh, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(presentedRefresh))
            {
                return;
            }

            var computedHash = EncodeHash(ComputeRefreshHmac(presentedRefresh));
            var rec = await _financeDbContext.RefreshTokens
                .SingleOrDefaultAsync(r => r.TokenHash == computedHash, ct);

            if (rec is null || !VerifyRefreshHash(presentedRefresh, rec.TokenHash))
            {
                return;
            }

            if (rec.RevokedAtUtc == null)
            {
                rec.RevokedAtUtc = DateTime.UtcNow;
            }

            await RevokeChainAsync(rec, ct);
            await _financeDbContext.SaveChangesAsync(ct);
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


        private byte[] ComputeRefreshHmac(string presented)
            => HMACSHA256.HashData(_refreshTokenHmacKey, Encoding.UTF8.GetBytes(presented));

        private static string EncodeHash(byte[] hash) => Convert.ToBase64String(hash);

        private bool VerifyRefreshHash(string presented, string storedHashBase64)
        {
            byte[] stored;
            try
            {
                stored = Convert.FromBase64String(storedHashBase64);
            }
            catch (FormatException)
            {
                return false;
            }

            var computed = ComputeRefreshHmac(presented);
            return CryptographicOperations.FixedTimeEquals(computed, stored);
        }

    }

}
