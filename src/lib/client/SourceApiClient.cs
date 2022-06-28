using Microsoft.Extensions.Logging;
using Polly;
using SunshineExpress.Service.Contract;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;

namespace SunshineExpress.Client;

public class SourceApiClient : ISourceClient
{
    private readonly ILogger<SourceApiClient> _logger;
    private readonly AsyncPolicy<HttpResponseMessage> _httpRequestPolicy;
    private readonly AsyncPolicy<HttpResponseMessage> _httpAuthorizePolicy;
    private readonly HttpClient _httpClient;
    private readonly SourceApiClientConfiguration _configuration;

    private string? _authorization;

    public SourceApiClient(SourceApiClientConfiguration configuration, ILogger<SourceApiClient> logger)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = new HttpClient { BaseAddress = new Uri(configuration.BaseUri) };

        _httpRequestPolicy = Policy.HandleResult<HttpResponseMessage>(
            r => r.StatusCode != HttpStatusCode.OK || r.Content.Headers.ContentLength == 0)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(retryAttempt), onRetry: async (response, timespan) =>
            {
                if (response.Result.StatusCode == HttpStatusCode.Unauthorized)
                    await PerformReauthorization();
            });

        _httpAuthorizePolicy = Policy.HandleResult<HttpResponseMessage>(
            r => r.StatusCode != HttpStatusCode.OK || r.Content.Headers.ContentLength == 0)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(retryAttempt));
    }

    public Task<IEnumerable<string>> FetchCities()
        => SendRequestUsingPolicy<IEnumerable<string>>(_httpRequestPolicy, HttpMethod.Get, "cities", requestData: null);

    public Task<WeatherDto> FetchWeather(string city)
        => SendRequestUsingPolicy<WeatherDto>(_httpRequestPolicy, HttpMethod.Get, $"weathers/{HttpUtility.UrlEncode(city)}", requestData: null);

    private async Task PerformReauthorization()
    {
        var requestData = new
        {
            username = _configuration.Username,
            password = _configuration.Password
        };

        _authorization = string.Empty;
        var response = await SendRequestUsingPolicy<AuthenticationResponse>(_httpAuthorizePolicy, HttpMethod.Post, "authorize", requestData);
        if (string.IsNullOrEmpty(response.Token))
        {
            _logger.LogError("API authentication failed.");
            throw new UnauthorizedAccessException("API authentication failed.");
        }
    }

    internal record AuthenticationResponse(string Token);

    private async Task<TResponse> SendRequestUsingPolicy<TResponse>(AsyncPolicy<HttpResponseMessage> policy, HttpMethod method, string? requestUri, object? requestData)
    {
        var response = await policy.ExecuteAsync(async () =>
        {
            var requestMessage = new HttpRequestMessage(method, requestUri);
            if (!string.IsNullOrEmpty(_authorization))
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue(_authorization);

            if (requestData is not null)
            {
                var requestJson = JsonSerializer.Serialize(requestData);
                requestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(requestMessage);
            return response;
        });

        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<TResponse>(responseJson);
        if (responseData is null)
        {
            _logger.LogError("Received unexpected empty response from the API.");
            throw new Exception("Received unexpected empty response from the API.");
        }

        return responseData;
    }
}