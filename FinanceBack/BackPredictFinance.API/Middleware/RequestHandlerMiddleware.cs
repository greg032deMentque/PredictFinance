using BackPredictFinance.Common.Security;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Serilog.Context;

namespace BackPredictFinance.API.Middleware
{
    public sealed class RequestHandlerMiddleware
    {
        private static readonly TimeSpan AnalyticsConsentCacheTtl = TimeSpan.FromMinutes(5);

        private readonly RequestDelegate _next;
        private readonly ILogger<RequestHandlerMiddleware> _logger;

        public RequestHandlerMiddleware(RequestDelegate next, ILogger<RequestHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(
            HttpContext context,
            AnalyticService analyticService,
            IMemoryCache memoryCache,
            FinanceDbContext financeDbContext)
        {
            try
            {
                var currentUserId = context.User.GetUserId();
                using var _ = LogContext.PushProperty("UserName", currentUserId ?? string.Empty);

                if (await ShouldTrackAsync(context, currentUserId, memoryCache, financeDbContext))
                {
                    context.Request.EnableBuffering();
                    await analyticService.AddAnalytic(context, context.RequestAborted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist request analytics for path {RequestPath}", context.Request.Path);
            }

            await _next(context);
        }

        private static async Task<bool> ShouldTrackAsync(
            HttpContext context,
            string? currentUserId,
            IMemoryCache memoryCache,
            FinanceDbContext financeDbContext)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            var requestPath = context.Request.Path.Value ?? string.Empty;
            if (requestPath.Contains("/refresh", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return false;
            }

            var cacheKey = $"analytics-consent:{currentUserId}";
            if (memoryCache.TryGetValue<bool>(cacheKey, out var cachedConsent))
            {
                return cachedConsent;
            }

            var hasConsent = await financeDbContext.Users
                .AsNoTracking()
                .Where(user => user.Id == currentUserId)
                .Select(user => user.AnalyticsConsent)
                .FirstOrDefaultAsync(context.RequestAborted);

            memoryCache.Set(cacheKey, hasConsent, AnalyticsConsentCacheTtl);
            return hasConsent;
        }
    }
}
