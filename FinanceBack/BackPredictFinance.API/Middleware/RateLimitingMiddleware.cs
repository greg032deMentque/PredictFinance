using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;


namespace BackPredictFinance.API.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly IMemoryCache _cache;
        private const int LIMIT = 200;
        private readonly TimeSpan PERIOD = TimeSpan.FromMinutes(1);

        private sealed record WindowState(int Count, DateTime WindowStart);

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IMemoryCache cache)
        {
            _next = next;
            _logger = logger;
            _cache = cache;
        }

        public async Task Invoke(HttpContext context)
        {
            var key = $"RateLimit_{context.Connection.RemoteIpAddress}";
            var now = DateTime.UtcNow;

            WindowState state;
            if (_cache.TryGetValue(key, out WindowState? existing) && existing != null && now - existing.WindowStart < PERIOD)
            {
                state = existing with { Count = existing.Count + 1 };
            }
            else
            {
                state = new WindowState(1, now);
            }

            var remaining = PERIOD - (now - state.WindowStart);
            _cache.Set(key, state, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = remaining });

            if (state.Count > LIMIT)
            {
                _logger.LogWarning("Rate limit exceeded for {IP}.", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Too many requests. Please try again later.");
                return;
            }

            await _next(context);
        }
    }
}
