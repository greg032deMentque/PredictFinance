using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.AdminViewModels.Content
{
    public sealed class LearnTopicUpsertRequestViewModel
    {
        [Required]
        [MaxLength(128)]
        public string TopicId { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1024)]
        public string Summary { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string RoutePath { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsPublished { get; set; }
    }
}
