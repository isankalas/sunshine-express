using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SunshineExpress.Service.Configuration;
public static class WeatherServiceConfigurationExtensions
{
    /// <summary>
    /// Configures the <see cref="WeatherService"/> and its dependencies.
    /// </summary>
    /// <param name="services">The services collection to register the dependencies in.</param>
    /// <param name="configure">Additional actions to be performed for the service registration.</param>
    public static IServiceCollection AddWeatherService(this IServiceCollection services, Action<IWeatherServiceBuilder> configure)
    {
        var builder = new WeatherServiceBuilder(services);
        configure(builder);

        services.AddTransient(services =>
        {
            var service = new WeatherService(
                services.GetRequiredService<ISourceClient>(),
                services.GetRequiredService<IStorageClient>(),
                services.GetRequiredService<ICache>(),
                services.GetRequiredService<ILogger<WeatherService>>());

            if (builder.CacheDurationMinutes.HasValue)
                service.SetCacheDuration(builder.CacheDurationMinutes.Value);

            if (builder.CacheKey is not null)
                service.SetCacheKey(builder.CacheKey);

            return service;
        });

        return services;
    }
}
