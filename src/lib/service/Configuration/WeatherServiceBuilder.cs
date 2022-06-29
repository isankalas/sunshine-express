using Microsoft.Extensions.DependencyInjection;

namespace SunshineExpress.Service.Configuration;
internal class WeatherServiceBuilder : IWeatherServiceBuilder
{
    public IServiceCollection Services { get; }

    public int? CacheDurationMinutes { get; set; }

    public string? CacheKey { get; set; }

    public WeatherServiceBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IWeatherServiceBuilder SetCacheDuration(int? minutes)
    {
        if (minutes < 0)
            throw new ArgumentOutOfRangeException(nameof(minutes), "Minutes must be a positive integer.");
        
        CacheDurationMinutes = minutes;

        return this;
    }

    public IWeatherServiceBuilder SetCacheKey(string? key)
    {
        CacheKey = key;
        return this;
    }
}
