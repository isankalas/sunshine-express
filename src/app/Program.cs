using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SunshineExpress.Cache.Configuration;
using SunshineExpress.Client.Configuration;
using SunshineExpress.Service.Configuration;
using SunshineExpress.Storage.Blob.Configuration;
using SunshineExpress.Storage.Memory.Configuration;

await Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var config = new ConfigurationBuilder()
            .AddCommandLine(args)
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appSettings.json", optional: false, reloadOnChange: false)
            .Build();

        context.Configuration = config;
        var serviceConfig = config.GetSection("Service");

        services.AddLogging(options =>
            options
                .SetMinimumLevel(LogLevel.Debug)
                .ClearProviders()
                .AddFile(config["LogFilePath"], minimumLevel: LogLevel.Debug)
                // Uncomment to enable all the logging to be output to the console
                // .AddConsole()
                );
        services.AddWeatherService(options =>
            options
                .SetCacheDuration(config.GetValue<int>("Service:CacheDuration"))
                .SetCacheKey(config["Service:CacheKey"])
                .UseConcurrentMemoryCache()
                .UseApiSource(
                    apiUri: config["DataSourceApi:Uri"],
                    username: config["DataSourceApi:Username"],
                    password: config["DataSourceApi:Password"])
                // Uncomment to use the Blob storage. Can only use one type of storage at a time!
                //.UseBlobStorage(
                //    connectionString: config["Storage:ConnectionString"],
                //    container: config["Storage:ContainerName"])
                .UseFileStorage(
                    basePath: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config["Storage:ContainerName"]))
                );
        services.AddHostedService<ConsoleHostedService>();
    })
    .RunConsoleAsync();
