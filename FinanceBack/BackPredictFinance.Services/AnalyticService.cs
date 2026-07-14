using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Common.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace BackPredictFinance.Services
{
    /// <summary>
    /// Persiste des traces analytiques à partir des requêtes HTTP reçues par l'API.
    /// </summary>
    public class AnalyticService: BaseService
    {
          public AnalyticService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<bool> AddAnalytic(HttpContext context, CancellationToken ct = default)
        {
            var analyticAdded = new Analytic();
            analyticAdded.Request = context.Request.Path.ToString();

            var bodyAsText = "";

            try
            {
                if (context.Request.ContentLength > 0 && context.Request.Body.CanRead)
                {
                    context.Request.EnableBuffering();
                    context.Request.Body.Position = 0;
                    using (var bodyReader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true))
                    {
                        //  Read the stream as text
                        bodyAsText = await bodyReader.ReadToEndAsync();

                        if (!string.IsNullOrWhiteSpace(bodyAsText))
                        {
                            try
                            {
                                var bodyAsJson = JToken.Parse(bodyAsText);
                                RemoveSensitiveFields(bodyAsJson);
                                bodyAsText = bodyAsJson.ToString(Formatting.None);
                            }
                            catch (JsonReaderException ex)
                            {
                                _logger.LogWarning(
                                    $"Failed to deserialize request body as JSON while handling HTTP request {analyticAdded.Request}",
                                    ex);

                                if (ContainsSensitiveContent(bodyAsText))
                                {
                                    bodyAsText = string.Empty;
                                }
                            }                            
                        }
                    }

                    context.Request.Body.Position = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    $"Failed to read request body while handling HTTP request {analyticAdded.Request}",
                    ex);
            }

            // don't save blob in db
            if (bodyAsText.Contains("filename=", StringComparison.OrdinalIgnoreCase))
                bodyAsText = string.Empty;

            var forwardedIp = string.Empty;

            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedIps))
            {
                forwardedIp = forwardedIps.FirstOrDefault();
            }

            var remoteIpAddress = context.Connection.RemoteIpAddress?.ToString();

            var ip = !string.IsNullOrWhiteSpace(forwardedIp) ? forwardedIp : remoteIpAddress;

            analyticAdded.Id = Guid.NewGuid().ToString();
            analyticAdded.Date = DateTime.UtcNow;
            analyticAdded.Ip = ip ?? "";
            analyticAdded.Login = context.User.GetUserId() ?? string.Empty;
            analyticAdded.Request = analyticAdded.Request ?? "";
            analyticAdded.Referer = context.Request.Headers["Referer"].ToString();
            analyticAdded.UserAgent = context.Request.Headers["User-Agent"].ToString();
            analyticAdded.Body = bodyAsText ?? "";

            try
            {
                var stringObject = JsonConvert.SerializeObject(analyticAdded);

                _logger.LogDebug($"ANALYTIC OBJECT AS STRING: {stringObject}");

            }
            catch
            {
                _logger.LogWarning("Fail while trying JsonConvert.SerializeObject() on analyticAdded object for _logger.LogDebug() input message");
            }
           
            // Add new analytic
            await _financeDbContext.Analytics.AddAsync(analyticAdded, ct);

            await _financeDbContext.SaveChangesAsync(ct);

            return true;
        }

        private static readonly string[] SensitiveKeywords = ["password", "token", "secret", "key"];

        private static void RemoveSensitiveFields(JToken token)
        {
            if (token is JObject jsonObject)
            {
                foreach (var property in jsonObject.Properties().ToList())
                {
                    if (IsSensitivePropertyName(property.Name) || string.Equals(property.Name, "PDFBytes", StringComparison.OrdinalIgnoreCase))
                    {
                        property.Remove();
                        continue;
                    }

                    RemoveSensitiveFields(property.Value);
                }

                return;
            }

            if (token is JArray jsonArray)
            {
                foreach (var child in jsonArray)
                {
                    RemoveSensitiveFields(child);
                }
            }
        }

        private static bool ContainsSensitiveContent(string bodyAsText)
        {
            return SensitiveKeywords.Any(keyword => bodyAsText.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsSensitivePropertyName(string propertyName)
        {
            return SensitiveKeywords.Any(keyword => propertyName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }
    }
}
