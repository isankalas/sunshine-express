namespace SunshineExpress.Service.Contract;

public interface ISourceClient
{
    Task<IEnumerable<string>> FetchCities();

    Task<WeatherDto> FetchWeather(string city);
}