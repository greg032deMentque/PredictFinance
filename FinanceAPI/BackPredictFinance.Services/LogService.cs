using Microsoft.AspNetCore.Http;
using Serilog;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace BackPredictFinance.Services
{
    public interface ILogService
    {
        void LogError(string message, [CallerMemberName] string functionName = "");
        void LogError(string customMessage, Exception ex, [CallerMemberName] string functionName = "");
        void LogError(Exception ex, [CallerMemberName] string functionName = "");

        void LogInformation(string message, [CallerMemberName] string functionName = "");

        void LogWarning(string message, [CallerMemberName] string functionName = "");
        void LogWarning(string customMessage, Exception ex, [CallerMemberName] string functionName = "");

        void LogDebug(string message, [CallerMemberName] string functionName = "");

        void LogWarning(string messageTemplate, params object[] propertyValues);
        void LogInformation(string messageTemplate, params object[] propertyValues);
        void LogDebug(string messageTemplate, params object[] propertyValues);
        void LogError(string messageTemplate, params object[] propertyValues);
    }

    public class LogService : ILogService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LogService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ILogger WithContext(string functionName)
        {
            var context = _httpContextAccessor.HttpContext;

            var userId = context?.User.FindFirstValue(ClaimTypes.Sid) ?? "null";
            var traceId = context?.TraceIdentifier ?? "";

            var endpoint = context?.GetEndpoint();
            var endpointName = endpoint?.DisplayName ?? "";

            var rv = context?.Request.RouteValues;
            var controller = rv is null ? "" : (rv.TryGetValue("controller", out var c) ? c?.ToString() ?? "" : "");
            var action = rv is null ? "" : (rv.TryGetValue("action", out var a) ? a?.ToString() ?? "" : "");

            var path = context?.Request.Path.Value ?? "";
            var method = context?.Request.Method ?? "";

            return Log.ForContext("UserId", userId)
                .ForContext("TraceId", traceId)
                .ForContext("Function", functionName)
                .ForContext("Endpoint", endpointName)
                .ForContext("Controller", controller)
                .ForContext("Action", action)
                .ForContext("Path", path)
                .ForContext("Method", method);
        }

        public void LogError(string message, [CallerMemberName] string functionName = "")
            => WithContext(functionName).Error("{Message}", message);

        public void LogError(string customMessage, Exception ex, [CallerMemberName] string functionName = "")
            => WithContext(functionName).Error(ex, "{CustomMessage}", customMessage);

        public void LogError(Exception ex, [CallerMemberName] string functionName = "")
            => WithContext(functionName).Error(ex, "Unhandled exception");

        public void LogWarning(string message, [CallerMemberName] string functionName = "")
            => WithContext(functionName).Warning("{Message}", message);

        public void LogWarning(string customMessage, Exception ex, [CallerMemberName] string functionName = "")
            => WithContext(functionName).Warning(ex, "{CustomMessage}", customMessage);

        public void LogInformation(string message, [CallerMemberName] string functionName = "")
            => WithContext(functionName).Information("{Message}", message);

        public void LogDebug(string message, [CallerMemberName] string functionName = "")
            => WithContext(functionName).Debug("{Message}", message);

        public void LogWarning(string messageTemplate, params object[] propertyValues)
            => LogWarningTemplateCore(messageTemplate, propertyValues);

        public void LogInformation(string messageTemplate, params object[] propertyValues)
            => LogInformationTemplateCore(messageTemplate, propertyValues);

        public void LogDebug(string messageTemplate, params object[] propertyValues)
            => LogDebugTemplateCore(messageTemplate, propertyValues);

        public void LogError(string messageTemplate, params object[] propertyValues)
            => LogErrorTemplateCore(messageTemplate, propertyValues);

        private void LogWarningTemplateCore(string messageTemplate, object[] propertyValues, [CallerMemberName] string functionName = "")
            => WithContext(functionName).Warning(messageTemplate, propertyValues);

        private void LogInformationTemplateCore(string messageTemplate, object[] propertyValues, [CallerMemberName] string functionName = "")
            => WithContext(functionName).Information(messageTemplate, propertyValues);

        private void LogDebugTemplateCore(string messageTemplate, object[] propertyValues, [CallerMemberName] string functionName = "")
            => WithContext(functionName).Debug(messageTemplate, propertyValues);

        private void LogErrorTemplateCore(string messageTemplate, object[] propertyValues, [CallerMemberName] string functionName = "")
            => WithContext(functionName).Error(messageTemplate, propertyValues);
    }
}
