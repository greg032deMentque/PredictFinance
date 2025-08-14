using BackPredictFinance.Datas.Models;
using BackPredictFinance.Services.PythonServices.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BackPredictFinance.Services.PythonServices
{
    public interface IPythonApiService
    {
        Task<PredictOut> PredictAsync(AssetIn asset);
        Task<RecommendationOut> RecommendAsync(RecommendationIn rec);
        Task<bool> HealthCheckAsync();

        Task<TResponse> SendAsync<TRequest, TResponse>(
            string relativeUrl,
            TRequest? payload,
            HttpMethod method = null);

        Task<TResponse> GetAsync<TResponse>(string relativeUrl);
    }

    public class PythonApiService : BaseService, IPythonApiService
    {
        private readonly HttpClient _http;

        public PythonApiService(HttpClient http, IServiceProvider serviceProvider) : base(serviceProvider) 
        {
            _http = http;
        }

        public async Task<PredictOut> PredictAsync(AssetIn asset)
        {
            var resp = await _http.PostAsJsonAsync("predict", asset);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<PredictOut>()!;
        }

        public async Task<RecommendationOut> RecommendAsync(RecommendationIn rec)
        {
            var resp = await _http.PostAsJsonAsync("recommend", rec);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<RecommendationOut>()!;
        }

        public async Task<bool> HealthCheckAsync()
        {
            var resp = await _http.GetAsync("health");
            return resp.IsSuccessStatusCode;
        }

        public async Task<TResponse> SendAsync<TRequest, TResponse>(
            string relativeUrl,
            TRequest? payload,
            HttpMethod method = null)
        {
            method ??= HttpMethod.Post;
            var request = new HttpRequestMessage(method, relativeUrl);

            if (payload is not null && method != HttpMethod.Get)
            {
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            try
            {
                _logger.LogInformation($"Envoi vers Python API {method} {relativeUrl}");
                var response = await _http.SendAsync(request);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation($"Réponse reçue de Python API: {response.StatusCode}");
                var responseJson = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<TResponse>(
                    responseJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                )!;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de l'appel à l'API Python ({relativeUrl})", ex);
                throw;
            }
        }

        public Task<TResponse> GetAsync<TResponse>(string relativeUrl)
            => SendAsync<object, TResponse>(relativeUrl, null, HttpMethod.Get);
    }
}
