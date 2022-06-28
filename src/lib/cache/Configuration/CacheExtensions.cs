using Microsoft.Extensions.DependencyInjection;
using SunshineExpress.Service.Configuration;
using SunshineExpress.Service.Contract;

namespace SunshineExpress.Cache.Configuration;
public static class CacheExtensions
{
    public static IWeatherServiceBuilder UseConcurrentMemoryCache(this IWeatherServiceBuilder builder)
    {
        builder.Services.AddSingleton<ICache, ConcurrentMemoryCache>();

        return builder;
    }
}
