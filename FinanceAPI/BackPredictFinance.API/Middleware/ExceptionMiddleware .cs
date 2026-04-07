using BackPredictFinance.Common;
using BackPredictFinance.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;

namespace BackPredictFinance.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _env;

        public ExceptionMiddleware(
            RequestDelegate next,
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment env)
        {
            _next = next;
            _scopeFactory = scopeFactory;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context, IAuthenticationService auth)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, auth);
            }
        }


        private async Task HandleExceptionAsync(
                HttpContext context,
                Exception ex,
                IAuthenticationService auth)
        {
            using var scope = _scopeFactory.CreateScope();
            var logSvc = scope.ServiceProvider.GetRequiredService<ILogService>();

            var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

            var statusCode = MapStatusCode(ex);
            var userMessage = MapUserMessage(statusCode);
            string? detail = _env.IsDevelopment() ? ex.Message : null;
            IReadOnlyCollection<string>? errors = null;

            if (ex is SecurityTokenExpiredException)
            {
                statusCode = HttpStatusCode.Unauthorized;
                userMessage = "Veuillez vous reconnecter.";
                detail = _env.IsDevelopment() ? "Le token a expiré." : null;
            }
            else if (ex is CustomException customEx)
            {
                statusCode = customEx.StatusCode;
                userMessage = string.IsNullOrWhiteSpace(customEx.FrontMessage)
                    ? MapUserMessage(customEx.StatusCode)
                    : customEx.FrontMessage;
                detail = _env.IsDevelopment() ? customEx.Message : null;
                errors = customEx.ErrorMessages?.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            }

            context.Response.Headers["X-Trace-Id"] = traceId;

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;

            var problem = new
            {
                type = "about:blank",
                title = StatusCodeTitle(statusCode),
                status = (int)statusCode,
                message = userMessage,
                errors,
                detail,
                traceId,
                instance = context.Request.Path.Value,
                method = context.Request.Method,
            };

            await context.Response.WriteAsync(JsonConvert.SerializeObject(problem));

            var endpoint = context.GetEndpoint();
            var endpointName = endpoint?.DisplayName ?? "";

            var rv = context.Request.RouteValues;
            var controller = rv.TryGetValue("controller", out var c) ? c?.ToString() ?? "" : "";
            var action = rv.TryGetValue("action", out var a) ? a?.ToString() ?? "" : "";

            logSvc.LogError(
                $"Unhandled exception middleware. statusCode={(int)statusCode} traceId={traceId} path={context.Request.Path.Value} method={context.Request.Method} endpoint={endpointName} controller={controller} action={action}",
                ex
            );
        }

        private static HttpStatusCode MapStatusCode(Exception ex)
            => ex switch
            {
                SecurityTokenExpiredException => HttpStatusCode.Unauthorized, // 401
                SecurityTokenException => HttpStatusCode.Unauthorized,        // 401
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,   // 401
                CustomException cex => cex.StatusCode,

                TaskCanceledException => HttpStatusCode.GatewayTimeout,      // 504
                OperationCanceledException => HttpStatusCode.GatewayTimeout, // 504

                KeyNotFoundException => HttpStatusCode.NotFound,             // 404
                ArgumentException => HttpStatusCode.BadRequest,              // 400
                _ => HttpStatusCode.InternalServerError
            };

        private static string MapUserMessage(HttpStatusCode status) => status switch
        {
            HttpStatusCode.BadRequest => "Requête invalide.",
            HttpStatusCode.Unauthorized => "Veuillez vous reconnecter.",
            HttpStatusCode.Forbidden => "Action non autorisée.",
            HttpStatusCode.NotFound => "Ressource introuvable.",
            HttpStatusCode.TooManyRequests => "Trop de requêtes. Patientez un instant.",
            HttpStatusCode.RequestTimeout => "Temps de réponse dépassé. Réessayez plus tard.",
            HttpStatusCode.GatewayTimeout => "Temps de réponse dépassé. Réessayez plus tard.",
            HttpStatusCode.Conflict => "Conflit sur la ressource.",
            (HttpStatusCode)422 => "Données invalides.",
            _ when (int)status >= 500 => "Service momentanément indisponible.",
            _ => "Une erreur est survenue."
        };

        private static string StatusCodeTitle(HttpStatusCode status) => status switch
        {
            HttpStatusCode.BadRequest => "Requête invalide",
            HttpStatusCode.Unauthorized => "Authentification requise",
            HttpStatusCode.Forbidden => "Interdit",
            HttpStatusCode.NotFound => "Introuvable",
            HttpStatusCode.Conflict => "Conflit",
            (HttpStatusCode)422 => "Entité non traitable",
            HttpStatusCode.TooManyRequests => "Trop de requêtes",
            HttpStatusCode.RequestTimeout => "Délai dépassé",
            HttpStatusCode.GatewayTimeout => "Délai dépassé",
            HttpStatusCode.BadGateway => "Passerelle en erreur",
            HttpStatusCode.ServiceUnavailable => "Service indisponible",
            _ => "Erreur"
        };
    }

    public static class ExceptionMiddlewareExtension
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
            => app.UseMiddleware<ExceptionMiddleware>();
    }
}
