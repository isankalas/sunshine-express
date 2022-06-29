using Microsoft.Extensions.DependencyInjection;
using SunshineExpress.Service.Configuration;
using SunshineExpress.Service.Contract;

namespace SunshineExpress.Storage.Memory.Configuration;
public static class FileStorageExtensions
{
    public static IWeatherServiceBuilder UseFileStorage(this IWeatherServiceBuilder builder, string basePath)
    {
        builder.Services.AddSingleton(_ => new FileStorageConfiguration
        {
            BasePath = basePath
        });
        builder.Services.AddTransient<IStorageClient, FileStorage>();

        return builder;
    }
}
