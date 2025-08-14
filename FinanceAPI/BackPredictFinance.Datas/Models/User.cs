
using BackPredictFinance.Common;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics.Metrics;
using System.Net;

namespace BackPredictFinance.Datas.Models
{
    public class User : IdentityUser
    {

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastConnection { get; set; }

      
        public string RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public bool IsActive { get; set; }

        public Device? Device { get; set; }

        public List<UserAsset>? UserAssets { get; set; }
    }
}
