using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SunshineExpress.Service;
using System.Text.Json;

internal sealed class ConsoleHostedService : IHostedService
{
    private readonly WeatherService _weatherService;
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IConfiguration _configuration;
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public ConsoleHostedService(
        WeatherService weatherService,
        ILogger<ConsoleHostedService> logger,
        IHostApplicationLifetime appLifetime,
        IConfiguration configuration)
    {
        _weatherService = weatherService;
        _logger = logger;
        _appLifetime = appLifetime;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _appLifetime.ApplicationStarted.Register(() =>
        {
            Task.Run(async () =>
            {
                try
                {
                    var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    var cities = _configuration.GetValue<string>("cities")?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList()
                        ?? Enumerable.Empty<string>();

                    _logger.LogInformation($"Starting with the following cities: {string.Join(", ", cities)}");
                    if (!cities.Any())
                    {
                        _logger.LogError("Must provide at least one city.");
                        Console.WriteLine("Must provide at least one city using the argument --cities and a comma-separated list.");
                    }
                    else
                    {
                        var invalidCities = (await Task.WhenAll(cities.Select(async city =>
                        {
                            var exists = await _weatherService.CityExists(city);
                            return (City: city, Exists: exists);
                        }))).Where(x => !x.Exists).Select(x => x.City);

                        if (invalidCities.Any())
                        {
                            var errorMessage = $"The following cities are invalid: {string.Join(", ", invalidCities)}";
                            _logger.LogError(errorMessage);
                            Console.WriteLine(errorMessage);
                        }
                        else
                        {
                            var serializerOptions = new JsonSerializerOptions
                            {
                                WriteIndented = true
                            };
                            while (!linkedTokenSource.IsCancellationRequested)
                            {
                                try
                                {
                                    Console.Clear();
                                    foreach (var weather in await Task.WhenAll(cities.Select(city => _weatherService.GetWeather(city))))
                                    {
                                        Console.WriteLine($"Current weather in {weather.City,-10} is {weather.Summary,-10}: " +
                                            $"{weather.Temperature,3}°C, {weather.WindSpeed,3}m/s, {weather.Precipitation,3}%");
                                    }

                                }
                                catch (Exception exception)
                                {
                                    _logger.LogError(exception, "Failed fetching weather for the given cities.");
                                }

                                _logger.LogDebug("Waiting for the next cycle...");
                                await Task.Delay(15000, linkedTokenSource.Token);
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Unhandled exception!");
                }
                finally
                {
                    _appLifetime.StopApplication();
                }
            });
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        return Task.CompletedTask;
    }
}