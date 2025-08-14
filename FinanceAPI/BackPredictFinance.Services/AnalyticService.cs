using BackPredictFinance.Datas.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace BackPredictFinance.Services
{
    public class AnalyticService: BaseService
    {
          public AnalyticService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<bool> AddAnalytic(HttpContext context)
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
                                var bodyAsJson = JObject.Parse(bodyAsText);

                                // remove all found password keys
                                bodyAsJson.Remove("password");
                                bodyAsJson.Remove("Password");
                                bodyAsJson.Remove("PASSWORD");

                                // remove DocuSign PDF files
                                bodyAsJson.Remove("PDFBytes");

                                bodyAsText = bodyAsJson.ToString(Formatting.None);
                            }
                            catch (JsonReaderException ex)
                            {
                                _logger.LogWarning(
                                    $"couldn't de-serialize request body as Json object while handling HTTP request: \"{analyticAdded.Request}\"", ex);
                            }                            
                        }
                    }

                    context.Request.Body.Position = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"while handling HTTP request: \"{analyticAdded.Request}\"", ex);
            }

            // don't save blob in db
            if (bodyAsText.ToLower().Contains("filename="))
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
            analyticAdded.Login = context.User.Identity?.Name ?? "";
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
            await FinanceDbContext.Analytics.AddAsync(analyticAdded);

            await FinanceDbContext.SaveChangesAsync();

            return true;
        }
    }
}
