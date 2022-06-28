using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SunshineExpress.Cache.Configuration;
using SunshineExpress.Client.Configuration;
using SunshineExpress.Service.Configuration;
using SunshineExpress.Storage.Blob.Configuration;

await Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddLogging(options =>
            options
                .SetMinimumLevel(LogLevel.Debug)
                .ClearProviders()
                .AddFile("logs\\{Date}.log", minimumLevel: LogLevel.Debug)
                // .AddConsole()
                );
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
