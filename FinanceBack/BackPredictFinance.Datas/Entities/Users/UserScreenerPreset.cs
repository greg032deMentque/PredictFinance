namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Combinaison de filtres du screener sauvegardée par un utilisateur pour être rejouée
    /// ultérieurement sans ressaisie.
    /// </summary>
    public class UserScreenerPreset : AuditableEntityBase, ISoftDeletable
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public string QueryJson { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
