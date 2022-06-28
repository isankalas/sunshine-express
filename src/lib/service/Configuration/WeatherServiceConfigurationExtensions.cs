using Microsoft.Extensions.DependencyInjection;

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
        services.AddTransient<WeatherService>();

        configure(new WeatherServiceBuilder(services));

        return services;
    }
}
