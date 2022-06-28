using Microsoft.Extensions.Logging;
using Polly;
using SunshineExpress.Client.Configuration;
using SunshineExpress.Service.Contract;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;

namespace SunshineExpress.Client;

/// <summary>
/// Implements the data source for the weather service which uses a REST API as its underlying data source.
/// </summary>
public class SourceApiClient : ISourceClient
{
    private readonly ILogger<SourceApiClient> logger;
    private readonly AsyncPolicy<HttpResponseMessage> httpRequestPolicy;
    private readonly AsyncPolicy<HttpResponseMessage> httpAuthorizePolicy;
    private readonly HttpClient httpClient;
    private readonly SourceApiClientConfiguration configuration;

    private string? authorization;

    public SourceApiClient(SourceApiClientConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<SourceApiClient> logger)
    {
        this.logger = logger;
        this.configuration = configuration;
        httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(configuration.BaseUri);

        // Create a Polly policy for a regular API request
        httpRequestPolicy = Policy.HandleResult<HttpResponseMessage>(
            r => r.StatusCode != HttpStatusCode.OK || r.Content.Headers.ContentLength == 0)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(retryAttempt), onRetryAsync: async (response, timespan) =>
            {
                if (response.Result.StatusCode == HttpStatusCode.Unauthorized)
                {
                    logger.LogInformation("Authentication has not been performed or expired.");
                    await Authenticate();
                }
                else
                {
                    logger.LogError("API request failed, retrying in a moment...");
                }
            });

        // Create a Polly policy for the authentication API request
        httpAuthorizePolicy = Policy.HandleResult<HttpResponseMessage>(
            r => r.StatusCode != HttpStatusCode.OK || r.Content.Headers.ContentLength == 0)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(retryAttempt), onRetry: (response, timespan) =>
            {
                logger.LogError("Authentication request to the API failed, retrying in a moment...");
            });
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> FetchCities()
        => SendRequestUsingPolicy<IEnumerable<string>>(httpRequestPolicy, HttpMethod.Get, "cities", requestData: null);

    /// <inheritdoc />
    public Task<WeatherDto> FetchWeather(string city)
        => SendRequestUsingPolicy<WeatherDto>(httpRequestPolicy, HttpMethod.Get, $"weathers/{HttpUtility.UrlEncode(city)}", requestData: null);

    // Called whenever 401 Unauthorized is returned by the API
    private async Task Authenticate()
    {
        var requestData = new
        {
            username = configuration.Username,
            password = configuration.Password
        };

        logger.LogDebug("Performing API authentication.");
        var response = await SendRequestUsingPolicy<AuthenticationResponse>(httpAuthorizePolicy, HttpMethod.Post, "authorize", requestData);
        if (string.IsNullOrEmpty(response.Token))
        {
            logger.LogError("API authentication failed.");
            throw new UnauthorizedAccessException("API authentication failed.");
        }
        authorization = response.Token;
        logger.LogInformation("Successfully authenticated and fetched the token from the API.");
    }

    internal record AuthenticationResponse(string Token);

    // Shorthand method to reuse the same logic of calling the API
    private async Task<TResponse> SendRequestUsingPolicy<TResponse>(AsyncPolicy<HttpResponseMessage> policy, HttpMethod method, string? requestUri, object? requestData)
    {
        var response = await policy.ExecuteAsync(async () =>
        {
            var requestMessage = new HttpRequestMessage(method, requestUri);
            if (!string.IsNullOrEmpty(authorization))
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue(authorization);

            if (requestData is not null)
            {
                var requestJson = JsonSerializer.Serialize(requestData);
                requestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            }

            var response = await httpClient.SendAsync(requestMessage);
            return response;
        });

        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<TResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        if (responseData is null)
        {
            logger.LogError("Received unexpected empty response from the API.");
            throw new Exception("Received unexpected empty response from the API.");
        }

        return responseData;
    }
}