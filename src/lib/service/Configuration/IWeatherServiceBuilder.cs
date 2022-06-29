using Microsoft.Extensions.DependencyInjection;

namespace SunshineExpress.Service.Configuration;

public interface IWeatherServiceBuilder
{
    IServiceCollection Services { get; }

    IWeatherServiceBuilder SetCacheDuration(int? seconds);

    IWeatherServiceBuilder SetCacheKey(string? key);
}