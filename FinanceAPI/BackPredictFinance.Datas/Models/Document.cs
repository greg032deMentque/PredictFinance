namespace BackPredictFinance.Datas.Models
{
    public class Document : AuditableEntityBase
	{
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = string.Empty;
        public string GuidName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MiniatureName { get; set; } = string.Empty;

        public Document()
        {
            Id = Guid.NewGuid().ToString();
        }

        public void SetParams()
        {
            Id = Guid.NewGuid().ToString();
            GuidName = string.Empty;
            MiniatureName = string.Empty;
        }
    }
}
