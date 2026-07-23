using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.AdminViewModels.Content;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Learning;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface ILearnTopicService
    {
        Task<List<LearnTopicViewModel>> GetPublishedAsync(CancellationToken ct = default);
        Task<List<LearnTopicAdminViewModel>> GetAllAdminAsync(CancellationToken ct = default);
        Task<LearnTopicAdminViewModel?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<LearnTopicAdminViewModel> CreateAsync(LearnTopicUpsertRequestViewModel request, CancellationToken ct = default);
        Task<LearnTopicAdminViewModel> UpdateAsync(string id, LearnTopicUpsertRequestViewModel request, CancellationToken ct = default);
        Task DeleteAsync(string id, CancellationToken ct = default);
    }

    public sealed class LearnTopicService : BaseService, ILearnTopicService
    {
        public LearnTopicService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<List<LearnTopicViewModel>> GetPublishedAsync(CancellationToken ct = default)
        {
            var topics = await _financeDbContext.LearnTopics
                .AsNoTracking()
                .Where(t => t.IsActive && t.IsPublished)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync(ct);

            return _mapper.Map<List<LearnTopicViewModel>>(topics);
        }

        public async Task<List<LearnTopicAdminViewModel>> GetAllAdminAsync(CancellationToken ct = default)
        {
            var topics = await _financeDbContext.LearnTopics
                .AsNoTracking()
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync(ct);

            return _mapper.Map<List<LearnTopicAdminViewModel>>(topics);
        }

        public async Task<LearnTopicAdminViewModel?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var topic = await _financeDbContext.LearnTopics
                .AsNoTracking()
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync(ct);

            return topic is null ? null : _mapper.Map<LearnTopicAdminViewModel>(topic);
        }

        public async Task<LearnTopicAdminViewModel> CreateAsync(LearnTopicUpsertRequestViewModel request, CancellationToken ct = default)
        {
            var topic = new LearnTopic
            {
                Id = Guid.NewGuid().ToString(),
                TopicId = request.TopicId,
                Title = request.Title,
                Summary = request.Summary,
                RoutePath = request.RoutePath,
                DisplayOrder = request.DisplayOrder,
                IsPublished = request.IsPublished,
                IsActive = true,
                IsDeleted = false
            };

            _financeDbContext.LearnTopics.Add(topic);
            await _financeDbContext.SaveChangesAsync(ct);

            return _mapper.Map<LearnTopicAdminViewModel>(topic);
        }

        public async Task<LearnTopicAdminViewModel> UpdateAsync(string id, LearnTopicUpsertRequestViewModel request, CancellationToken ct = default)
        {
            var topic = await _financeDbContext.LearnTopics
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync(ct);

            if (topic is null)
                throw new KeyNotFoundException($"Learn topic {id} introuvable.");

            topic.TopicId = request.TopicId;
            topic.Title = request.Title;
            topic.Summary = request.Summary;
            topic.RoutePath = request.RoutePath;
            topic.DisplayOrder = request.DisplayOrder;
            topic.IsPublished = request.IsPublished;

            await _financeDbContext.SaveChangesAsync(ct);

            return _mapper.Map<LearnTopicAdminViewModel>(topic);
        }

        public async Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var topic = await _financeDbContext.LearnTopics
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync(ct);

            if (topic is null)
                throw new KeyNotFoundException($"Learn topic {id} introuvable.");

            topic.IsDeleted = true;
            await _financeDbContext.SaveChangesAsync(ct);
        }
    }
}
