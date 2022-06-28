namespace SunshineExpress.Service.Contract;

public interface ISourceClient
{
    Task<IEnumerable<string>> FetchCities();

    Task<WeatherDto?> GetWeather(string city);
}