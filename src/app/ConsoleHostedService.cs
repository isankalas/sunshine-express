// See https://aka.ms/new-console-template for more information
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
                    var cities = _configuration.GetValue<string>("cities").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
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
                                    await Task.WhenAll(cities.Select(async city =>
                                    {
                                        var weather = await _weatherService.FetchAndSave(city);
                                        _logger.LogInformation($"Successfully fetched and saved weather data for {weather.City}");
                                        Console.WriteLine(JsonSerializer.Serialize(weather, serializerOptions));
                                    }).ToArray());
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