using Microsoft.Extensions.DependencyInjection;

namespace SunshineExpress.Service.Configuration;

public interface IWeatherServiceBuilder
{
    IServiceCollection Services { get; }
}