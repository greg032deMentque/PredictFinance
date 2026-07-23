namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Marque une entité comme soumise au soft-delete : le filtre global EF Core
    /// (HasQueryFilter) exclut automatiquement les lignes IsDeleted des lectures.
    /// </summary>
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
    }
}
