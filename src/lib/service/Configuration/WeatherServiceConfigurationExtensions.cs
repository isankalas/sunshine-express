using Microsoft.Extensions.DependencyInjection;

namespace SunshineExpress.Service.Configuration;
public static class WeatherServiceConfigurationExtensions
{
    public static IServiceCollection AddWeatherService(this IServiceCollection services, Action<IWeatherServiceBuilder> configure)
    {
        services.AddTransient<WeatherService>();

        configure(new WeatherServiceBuilder(services));

        return services;
    }
}
