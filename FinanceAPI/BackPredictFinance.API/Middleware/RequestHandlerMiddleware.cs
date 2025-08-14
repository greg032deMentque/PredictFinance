using BackPredictFinance.Services;
using Serilog.Context;

namespace BackPredictFinance.API.Middleware
{
    public class RequestHandlerMiddleware
    {
        private readonly RequestDelegate next;
        private ILogger _logger;

        public RequestHandlerMiddleware(RequestDelegate next, ILogger<RequestHandlerMiddleware> logger)
        {
            this.next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context, AnalyticService analyticService)
        {
            try
            {
                LogContext.PushProperty("UserName", context.User?.Claims?.FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value);
                context.Request.EnableBuffering();
                await analyticService.AddAnalytic(context);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error to add analytic, ex: {ex}");
            }

            await next(context);
        }
    }
}
