using Microsoft.Extensions.DependencyInjection;
using SunshineExpress.Service.Configuration;
using SunshineExpress.Service.Contract;

namespace SunshineExpress.Client.Configuration;
public static class ClientExtensions
{
    public static IWeatherServiceBuilder UseApiSource(this IWeatherServiceBuilder builder, string apiUri, string username, string password)
    {
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton(_ => new SourceApiClientConfiguration
        {
            BaseUri = apiUri,
            Username = username,
            Password = password
        });
        builder.Services.AddTransient<ISourceClient, SourceApiClient>();

        return builder;
    }
}
