using Microsoft.Extensions.DependencyInjection;
using SunshineExpress.Service.Configuration;
using SunshineExpress.Service.Contract;
using SunshineExpress.Storage.Blob;

namespace SunshineExpress.Storage.Blob.Configuration;
public static class StorageExtensions
{
    public static IWeatherServiceBuilder UseBlobStorage(this IWeatherServiceBuilder builder, string connectionString, string container)
    {
        builder.Services.AddSingleton(_ => new BlobStorageConfiguration
        {
            ContainerName = container,
            ConnectionString = connectionString
        });
        builder.Services.AddTransient<IStorageClient, BlobStorage>();

        return builder;
    }
}
