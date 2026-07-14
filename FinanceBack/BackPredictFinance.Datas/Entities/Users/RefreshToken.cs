namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Stocke un refresh token persistant et son cycle de rotation pour un utilisateur.
    /// </summary>
    public class RefreshToken
    {
        public long Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string TokenHash { get; set; } = string.Empty;
        public string? ReplacedByTokenHash { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAtUtc { get; set; }
        public string? DeviceId { get; set; }
        public string? FingerprintHash { get; set; }
    }
}

