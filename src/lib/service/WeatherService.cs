using SunshineExpress.Service.Contract;
using SunshineExpress.Service.Contract.Storage;
using SunshineExpress.Service.Exceptions;
using SunshineExpress.Service.Util;

namespace SunshineExpress.Service;

public class WeatherService
{
    private readonly ISourceClient client;
    private readonly IStorageClient storage;
    private readonly ICache cache;

    private int citiesCacheDuration = 60; // seconds
    private string citiesCacheKey = "cities";

    public WeatherService(ISourceClient client, IStorageClient storage, ICache cache)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public virtual async Task<WeatherDto> FetchAndSave(string city)
    {
        IEntityId<Weather> entityId = storage.CreateEntityId<Weather>(city);
        await using var entityLock = await storage.AcquireLock(entityId);

        if (!await CityExists(city))
            throw new UnknownCityException(city);

        var weatherDto = await client.GetWeather(city);
        if (weatherDto is null)
            throw new UnknownWeatherException(city);

        var entity = Weather.FromDto(entityId, weatherDto);
        await storage.AddOrUpdate(entity);

        return weatherDto;
    }

    public virtual async Task<bool> CityExists(string city)
        => (await GetCitiesInternal()).ContainsKey(city.RemoveDiacritics());

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

    protected internal virtual async Task<Dictionary<string, string>> GetCitiesInternal()
    {
        using var _ = cache.AcquireLock(citiesCacheKey);
        if (cache.TryGetValue<Dictionary<string, string>>(citiesCacheKey, out var cities))
            return cities;

        cities = (await client.FetchCities())
            .ToDictionary(x => x.RemoveDiacritics(), x => x);

        if (citiesCacheDuration > 0)
            cache.Set(citiesCacheKey, cities, TimeSpan.FromSeconds(citiesCacheDuration));

        return cities;
    }
}