namespace SunshineExpress.Service.Contract;

/// <summary>
/// Declares data source methods necessary for the service to run.
/// </summary>
public interface ISourceClient
{
    /// <summary>
    /// Fetches all the supported cities.
    /// </summary>
    /// <returns>The list of supported cities.</returns>
    Task<IEnumerable<string>> FetchCities();

    /// <summary>
    /// Fetches the weather for the specified <paramref name="city"/>.
    /// </summary>
    /// <param name="city">City to fetch the weather for.</param>
    /// <returns>The weather data for the city.</returns>
    Task<WeatherDto> FetchWeather(string city);
}