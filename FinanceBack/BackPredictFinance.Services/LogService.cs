using Microsoft.AspNetCore.Http;
using Serilog;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace BackPredictFinance.Services
{
    /// <summary>
    /// Journalise des événements applicatifs enrichis avec le contexte HTTP courant.
    /// </summary>
    public interface ILogService
    {
        /// <summary>
        /// Journalise une erreur simple.
        /// </summary>
        void LogError(string message, [CallerMemberName] string functionName = "");
        /// <summary>
        /// Journalise une erreur accompagnée d'une exception.
        /// </summary>
        void LogError(string customMessage, Exception ex, [CallerMemberName] string functionName = "");
        /// <summary>
        /// Journalise une exception non gérée.
        /// </summary>
        void LogError(Exception ex, [CallerMemberName] string functionName = "");

        /// <summary>
        /// Journalise un message d'information.
        /// </summary>
        void LogInformation(string message, [CallerMemberName] string functionName = "");

        /// <summary>
        /// Journalise un avertissement simple.
        /// </summary>
        void LogWarning(string message, [CallerMemberName] string functionName = "");
        /// <summary>
        /// Journalise un avertissement accompagné d'une exception.
        /// </summary>
        void LogWarning(string customMessage, Exception ex, [CallerMemberName] string functionName = "");

        /// <summary>
        /// Journalise un message de debug.
        /// </summary>
        void LogDebug(string message, [CallerMemberName] string functionName = "");

        /// <summary>
        /// Journalise un avertissement structuré avec propriétés.
        /// </summary>
        void LogWarning(string messageTemplate, params object[] propertyValues);
        /// <summary>
        /// Journalise une information structurée avec propriétés.
        /// </summary>
        void LogInformation(string messageTemplate, params object[] propertyValues);
        /// <summary>
        /// Journalise un debug structuré avec propriétés.
        /// </summary>
        void LogDebug(string messageTemplate, params object[] propertyValues);
        /// <summary>
        /// Journalise une erreur structurée avec propriétés.
        /// </summary>
        void LogError(string messageTemplate, params object[] propertyValues);
    }

    /// <summary>
    /// Implémente la journalisation applicative enrichie avec des métadonnées de requête.
    /// </summary>
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
