using BackPredictFinance.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace BackPredictFinance.API.Middleware
{
    public class CustomExceptionReturn
    {
        public string Exception { get; set; }
        public int StatusCode { get; set; }
    }
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (SecurityTokenExpiredException)
            {
                await HandleSecurityTokenExpiredExceptionAsync(context);
            }
            catch (CustomException ex)
            {
                await HandleCustomExceptionAsync(context, ex);
            }
            catch (Exception ex)
            {
                await HandleInternalServerErrorAsync(context, ex);
            }
        }

        private async Task HandleSecurityTokenExpiredExceptionAsync(HttpContext context)
        {
            context.Response.StatusCode = 401;

            await WriteErrorResponseAsync(context, "", "token expired");
        }

        private async Task HandleCustomExceptionAsync(HttpContext context, CustomException ex)
        {
            context.Response.StatusCode = (int)ex.StatusCode;
            await WriteErrorResponseAsync(context, ex.FunctionName, ex.Message, ex.FrontMessage);
        }

        private async Task HandleInternalServerErrorAsync(HttpContext context, Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            string message = ex.Message;
            if (message == "Exception of type 'System.Exception' was thrown")
                message = "Une erreur est survenue";

            await WriteErrorResponseAsync(context, ex.StackTrace, message);
        }

        private async Task WriteErrorResponseAsync(HttpContext context, string trace, string logMessage, string frontMessage = null)
        {
            context.Response.ContentType = "application/json";
            string currentUserId = context.User.FindFirst(ClaimTypes.Sid)?.Value ?? "Unknown user";

            var errorObject = new CustomErrorMessage
            {
                DateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss,fff"),
                StatusCode = context.Response.StatusCode,
                Exception = logMessage,
                Source_ip = context.Connection.RemoteIpAddress?.ToString(),
                Host_ip = context.Connection.LocalIpAddress?.ToString(),
                Hostname = context.Request.Host.Host,
                protocol = context.Request.Scheme,
                Port = context.Request.Host.Port,
                Request_uri = context.Request.Path,
                Request_method = context.Request.Method,
                Trace = trace,
                CurrentUserId = currentUserId,
            };

            var stringObject = JsonConvert.SerializeObject(errorObject);

            var stringObjectArray = stringObject.Split("\\r\\n");
            foreach (var item in stringObjectArray)
            {
                _logger.LogError(item);
            }

            frontMessage = frontMessage ?? "Une erreur est survenue";

            var responseBody = new CustomExceptionReturn()
            {
                Exception = frontMessage,
                StatusCode = context.Response.StatusCode,
            };

            stringObject = JsonConvert.SerializeObject(responseBody);
            await context.Response.WriteAsync(stringObject, Encoding.UTF8);
        }
    }
}
