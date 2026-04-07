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
        private const int LIMIT = 25;
        private readonly TimeSpan PERIOD = TimeSpan.FromMinutes(1);

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IMemoryCache cache)
        {
            _next = next;
            _logger = logger;
            _cache = cache;
        }

        public async Task Invoke(HttpContext context)
        {
            var key = $"RateLimit_{context.Connection.RemoteIpAddress}";
            int count = 0;
            if (_cache.TryGetValue(key, out int existingCount))
            {
                count = existingCount;
            }
            count++;

            // Store count in cache
            var cacheEntryOptions = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = PERIOD };
            _cache.Set(key, count, cacheEntryOptions);

            if (count > LIMIT)
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
