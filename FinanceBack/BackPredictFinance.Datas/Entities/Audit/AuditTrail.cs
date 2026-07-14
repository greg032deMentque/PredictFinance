using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Représente une trace détaillée de modification d'entité pour l'audit applicatif.
    /// </summary>
    public class AuditTrail
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? UserId { get; set; }
        public string EntityName { get; set; } = null!;
        public string? PrimaryKey { get; set; }
        public EntityState EntityStateEnum { get; set; }
        public DateTime DateUtc { get; set; }
        public string OldValues { get; set; } = "{}";
        public string NewValues { get; set; } = "{}";
        public List<string> ChangedColumns { get; set; } = new();
    }
}
