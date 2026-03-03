using Microsoft.EntityFrameworkCore;
namespace BackPredictFinance.Datas.Entities
{
    public interface IAuditableEntity
    {
        DateTime CreatedAtUtc { get; set; }
        DateTime? UpdatedAtUtc { get; set; }
    }

    public abstract class AuditableEntityBase : IAuditableEntity
    {
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }

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

