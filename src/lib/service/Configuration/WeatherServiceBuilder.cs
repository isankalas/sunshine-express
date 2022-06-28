using Microsoft.Extensions.DependencyInjection;

namespace SunshineExpress.Service.Configuration;
internal class WeatherServiceBuilder : IWeatherServiceBuilder
{
    public IServiceCollection Services { get; }

    public WeatherServiceBuilder(IServiceCollection services)
    {
        Services = services;
    }
}
