using BackPredictFinance.Common.Jwt;
using BackPredictFinance.Datas.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BackPredictFinance.Services
{

    public class JwtGeneratorService : BaseService
    {
        private readonly JWTToken _jwtTokenConfig;

        public JwtGeneratorService(IOptions<JWTToken> jwtTokenOptions, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _jwtTokenConfig = jwtTokenOptions.Value;
        }

        public async Task<string> GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtTokenConfig.Secret);  // supprimer le secret de l'appsetting, grosse faille de sécu

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)

            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtTokenConfig.ValidityMinutesAcessToken),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _jwtTokenConfig.Issuer,
                Audience = _jwtTokenConfig.Audience
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }


        /// <summary>
        /// Récupère le refresh token stocké pour l'utilisateur s'il est encore valide.
        /// </summary>
        public async Task<string> GetRefreshTokenAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null &&
                user.RefreshTokenExpiryTime.HasValue &&
                user.RefreshTokenExpiryTime.Value > DateTime.UtcNow)
            {
                return user.RefreshToken;
            }
            return null;
        }

        /// <summary>
        /// Met à jour le refresh token de l'utilisateur en définissant une validité de 3 mois.
        /// </summary>
        public async Task UpdateRefreshTokenAsync(string userId, string newRefreshToken)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(_jwtTokenConfig.ValidityMinutesRefreshToken);

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    throw new Exception("La mise à jour du refresh token a échoué : " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        /// <summary>
        /// Génère un nouveau refresh token.
        /// </summary>
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }

            var now = DateTime.UtcNow.AddMinutes(_jwtTokenConfig.ValidityMinutesRefreshToken);

            return Convert.ToBase64String(randomNumber);
        }


        /// <summary>
        /// Récupérer les claims du token expiré sans tenir compte de sa durée de vie
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="SecurityTokenException"></exception>
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = false, // Expiré accepté
                ValidIssuer = _jwtTokenConfig.Issuer,
                ValidAudience = _jwtTokenConfig.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtTokenConfig.Secret!))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
                if (validatedToken is JwtSecurityToken jwtSecurityToken &&
                    jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return principal;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to validate token: " + ex.Message);
            }

            return null;
        }


       
    }

}
