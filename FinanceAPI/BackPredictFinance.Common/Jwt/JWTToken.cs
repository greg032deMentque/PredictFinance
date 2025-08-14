using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackPredictFinance.Common.Jwt
{
    public class JWTToken
    {
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string Name { get; set; }
        public string Token { get; set; }
        public int ValidityMinutesRefreshToken { get; set; }
        public int ValidityMinutesAcessToken { get; set; }
    }
}
