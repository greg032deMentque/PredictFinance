using BackPredictFinance.Common;
using BackPredictFinance.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace BackPredictFinance.API.Middleware
{
    public class CustomErrorMessage
    {
        public int StatusCode { get; set; }
        public string Exception { get; set; } = "";
        public string Request_uri { get; set; } = "";
        public string Request_method { get; set; } = "";
        public string CurrentUserId { get; set; } = "";
        public string TraceId { get; set; } = "";
    }

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
            var userMessage = MapUserMessage(statusCode, ex);
            string detail = ex.Message;

            if (ex is SecurityTokenExpiredException)
            {
                statusCode = HttpStatusCode.Unauthorized;
                userMessage = "Veuillez vous reconnecter.";
                detail = "Le token a expiré.";
            }
            else if (ex is CustomException customEx)
            {
                statusCode = customEx.StatusCode;
                userMessage = string.IsNullOrWhiteSpace(customEx.FrontMessage)
                    ? "Requête invalide."
                    : customEx.FrontMessage;
                detail = customEx.Message;
            }

          

            context.Response.Headers["X-Trace-Id"] = traceId;

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;

            var problem = new
            {
                type = "about:blank",
                title = StatusCodeTitle(statusCode),
                status = (int)statusCode,
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
                $"Unhandled exception middleware. traceId={traceId} path={context.Request.Path.Value} method={context.Request.Method} endpoint={endpointName} controller={controller} action={action}",
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

        private static string MapUserMessage(HttpStatusCode status, Exception ex) => status switch
        {
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
