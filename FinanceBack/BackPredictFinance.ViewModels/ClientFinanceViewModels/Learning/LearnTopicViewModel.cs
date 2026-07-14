using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Learning
{
    public sealed class LearnTopicViewModel
    {
        public string TopicId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string RoutePath { get; set; } = string.Empty;
    }

    public sealed class LearnTopicViewModelProfile : Profile
    {
        public LearnTopicViewModelProfile()
        {
            CreateMap<LearnTopic, LearnTopicViewModel>();
        }
    }
}
