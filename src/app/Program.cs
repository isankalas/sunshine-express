// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SunshineExpress.Cache.Configuration;
using SunshineExpress.Client.Configuration;
using SunshineExpress.Service.Configuration;
using SunshineExpress.Storage.Configuration;

using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
    .SetMinimumLevel(LogLevel.Trace)
    .AddConsole());

await Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddLogging(options => options.SetMinimumLevel(LogLevel.Debug).AddConsole());
        services.AddWeatherService(options =>
            options
                .UseConcurrentMemoryCache()
                .UseApiSource(
                    apiUri: "https://weather-api.isun.ch/api/",
                    username: "isun",
                    password: "passwrod")
                .UseBlobStorage(
                    connectionString: "UseDevelopmentStorage=true",
                    container: "entities"));
        services.AddHostedService<ConsoleHostedService>();
    })
    .RunConsoleAsync();
