using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using SunshineExpress.Service.Contract.Storage;
using SunshineExpress.Service.Exceptions;
using SunshineExpress.Service.Util;

[assembly: InternalsVisibleTo("SunshineExpress.Service.Test")]

namespace SunshineExpress.Service;

public class WeatherService
{
    private readonly ISourceClient client;
    private readonly IStorageClient storage;
    private readonly ICache cache;
    private readonly ILogger<WeatherService> logger;
    private int citiesCacheDuration = 60; // seconds
    private string citiesCacheKey = "cities";

    public WeatherService(ISourceClient client, IStorageClient storage, ICache cache, ILogger<WeatherService> logger)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.logger = logger;
    }

    /// <summary>
    /// Fetches the weather data for the specified <paramref name="city"/> and persists it in the storage.
    /// </summary>
    /// <param name="city">City to fetch the weather for. It must be one of the supported cities.</param>
    /// <returns>The current weather data for the city.</returns>
    /// <exception cref="UnknownCityException">Invalid <paramref name="city"/> name was specified.</exception>
    /// <exception cref="UnknownWeatherException">Failed fetching the weather data for the <paramref name="city"/>.</exception>
    public virtual async Task<WeatherDto> GetWeather(string city)
    {
        logger.LogDebug($"Fetching the weather data for {city}");
        var validCities = await GetCitiesInternal();
        if (!validCities.TryGetValue(city.RemoveDiacritics().ToLowerInvariant(), out var realCity))
        {
            logger.LogError($"Canot fetch weather data for {city} because the city is not recognized");
            throw new UnknownCityException(city);
        }

        logger.LogInformation($"Fetching the weather data for {city} from the data source");
        var weatherDto = await client.FetchWeather(realCity);
        if (weatherDto is null)
            throw new UnknownWeatherException(city);

        await PersistWeather(weatherDto);

        logger.LogDebug($"Successfully fetched and saved weather data for {city}.");
        return weatherDto;
    }

    /// <summary>
    /// Checks if the specified <paramref name="city"/> is supported.
    /// </summary>
    /// <param name="city">City to check if it is supported by the service.</param>
    /// <returns>A flag indicating whether the city is supported.</returns>
    public virtual async Task<bool> CityExists(string city)
        => (await GetCitiesInternal()).ContainsKey(city.RemoveDiacritics().ToLowerInvariant());

    /// <summary>
    /// Gets the list of all the cities supported by the service.
    /// </summary>
    /// <returns>The list of supported cities.</returns>
    public virtual async Task<IEnumerable<string>> GetCities()
        => (await GetCitiesInternal()).Values;

    /// <summary>
    /// Sets the duration in seconds for the caching of the list of cities.
    /// </summary>
    /// <param name="seconds">Duration in seconds</param>
    public void SetCacheDuration(int seconds)
        => citiesCacheDuration = seconds;

    /// <summary>
    /// Sets the key to be used for the list of cities in the cache.
    /// </summary>
    /// <param name="cacheKey">The key for the list of cities in the cache.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cacheKey"/> is <c>null</c>.</exception>
    public void SetCacheKey(string cacheKey)
        => citiesCacheKey = cacheKey ?? throw new ArgumentNullException(nameof(cacheKey));

    /// <summary>
    /// Persists the weather data into storage.
    /// </summary>
    /// <param name="weather">Data to persist.</param>
    protected internal virtual async Task PersistWeather(WeatherDto weather)
    {
        var city = weather.City;
        logger.LogDebug($"Acquiring entity lock for {city}");
        IEntityId<Weather> entityId = storage.CreateEntityId<Weather>(city);
        await using var entityLock = await storage.AcquireLock(entityId);
        var entity = await storage.Get(entityId);

        logger.LogDebug($"Saving weather data for {city} into the storage.");
        entity = Weather.FromDto(entityId, weather);
        await storage.AddOrUpdate(entity);
    }

    /// <summary>
    /// Fetches and saves or takes from the cahce the list of normalized cities names along with their original names.
    /// </summary>
    /// <returns>A dictionary with key values representing city names without diacritics and in lower case and, values representing the original city names.</returns>
    protected internal virtual async Task<Dictionary<string, string>> GetCitiesInternal()
    {
        using var _ = await cache.AcquireLock(citiesCacheKey);
        if (cache.TryGetValue<Dictionary<string, string>>(citiesCacheKey, out var cities))
            return cities;

        cities = (await client.FetchCities())
            .ToDictionary(x => x.RemoveDiacritics().ToLowerInvariant(), x => x);

        if (citiesCacheDuration > 0)
            cache.Set(citiesCacheKey, cities, TimeSpan.FromSeconds(citiesCacheDuration));

        return cities;
    }
}