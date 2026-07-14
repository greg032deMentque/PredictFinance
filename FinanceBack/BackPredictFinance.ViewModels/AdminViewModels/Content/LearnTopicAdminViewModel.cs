using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.AdminViewModels.Content
{
    public sealed class LearnTopicAdminViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string RoutePath { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsPublished { get; set; }
    }

    public sealed class LearnTopicAdminViewModelProfile : Profile
    {
        public LearnTopicAdminViewModelProfile()
        {
            CreateMap<LearnTopic, LearnTopicAdminViewModel>();
        }
    }
}
