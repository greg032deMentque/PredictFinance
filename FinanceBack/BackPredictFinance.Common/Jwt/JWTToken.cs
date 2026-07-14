using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackPredictFinance.Common.Jwt
{
    public class JWTToken
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public int ValidityMinutesRefreshToken { get; set; }
        public int ValidityMinutesAcessToken { get; set; }
    }
}
