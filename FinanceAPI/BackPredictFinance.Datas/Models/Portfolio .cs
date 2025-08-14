using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.Datas.Models
{
    /// <summary>
    /// Permettre à l’utilisateur de distinguer plusieurs « portefeuilles » (par exemple : « Pécule Crypto » vs « Actions Long Terme »).
    /// </summary>
    public class Portfolio : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public User User { get; set; } = null!;

        public string Name { get; set; }
        public string? Description { get; set; }

        public List<UserAsset> UserAssets { get; set; } = new List<UserAsset>();
    }


}
