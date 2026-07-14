namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Définit les champs d'audit communs aux entités persistées.
    /// </summary>
    public interface IAuditableEntity
    {
        DateTime CreatedAtUtc { get; set; }
        DateTime? UpdatedAtUtc { get; set; }
    }

    /// <summary>
    /// Fournit une base technique partagée pour les entités auditables.
    /// </summary>
    public abstract class AuditableEntityBase : IAuditableEntity
    {
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}

