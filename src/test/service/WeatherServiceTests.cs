using Microsoft.Extensions.Logging;
using SunshineExpress.Service.Contract;
using SunshineExpress.Service.Contract.Storage;
using SunshineExpress.Service.Exceptions;
using SunshineExpress.Service.Util;

namespace SunshineExpress.Service.Test;

public class WeatherServiceTests
{
    private ICache cache;
    private ISourceClient dataSourceClient;
    private IStorageClient storageClient;
    private ILogger<WeatherService> logger;
    private WeatherService service;

    private readonly string cacheKey = "citiesTest";
    private readonly int cacheDuration = 123; // seconds

    private readonly IEnumerable<string> cities = new[] { "Vilnius", "Klaipėda" };
    private readonly CancellationTokenSource cancellationTokenSource = new();

    public WeatherServiceTests()
    {
        cache = Substitute.For<ICache>();
        dataSourceClient = Substitute.For<ISourceClient>();
        storageClient = Substitute.For<IStorageClient>();
        logger = Substitute.For<ILogger<WeatherService>>();

        service = Substitute.For<WeatherService>(dataSourceClient, storageClient, cache, logger);
        service.SetCacheKey(cacheKey);
        service.SetCacheDuration(cacheDuration);

        dataSourceClient.FetchCities().Returns(Task.FromResult(cities));
        cache.AcquireLock(cacheKey).Returns(_ => new TestLock().WaitAsyncDisposable());
    }

    [Fact]
    public async Task WeatherService_GetInternalCities_LocksTheCache()
    {
        // Arrange
        var @lock = new TestLock(isLocked: true, cancellationTokenSource.Token);
        service.When(x => x.GetCitiesInternal()).CallBase();
        cache.AcquireLock(cacheKey).Returns(_ => @lock.WaitDisposable());

        // Act + Assert
        var task = service.GetCitiesInternal();

        // wait for the background task to acquire the lock
        while (!@lock.IsLocked)
            await Task.Delay(10);

        // extra delay to avoid a task race
        await Task.Delay(100);
        await dataSourceClient.DidNotReceive().FetchCities();

        // releases the lock
        cancellationTokenSource.Cancel();

        // the task can now complete
        await task;
        await dataSourceClient.Received(1).FetchCities();
        cache.Received(1).Set(cacheKey, Arg.Any<object>(), TimeSpan.FromSeconds(cacheDuration));
        @lock.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task WeatherService_GetInternalCities_FetchesAndCachesTheValue()
    {
        // Arrange
        service.When(x => x.GetCitiesInternal()).CallBase();

        // Act
        var result = await service.GetCitiesInternal();

        // Assert
        await dataSourceClient.Received(1).FetchCities();
        cache.Received(1).Set(cacheKey, result, TimeSpan.FromSeconds(cacheDuration));
        result.Should().ContainKeys("vilnius", "klaipeda");
    }

    [Fact]
    public async Task WeatherService_GetInternalCities_UsesCachedValue()
    {
        // Arrange
        var expectedResult = cities.ToDictionary(y => y.ToLowerInvariant(), y => y);
        cache.TryGetValue(cacheKey, out Arg.Any<Dictionary<string, string>>()).Returns(x =>
        {
            x[1] = expectedResult;
            return true;
        });
        service.When(x => x.GetCitiesInternal()).CallBase();

        // Act
        var result = await service.GetCitiesInternal();

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        await dataSourceClient.DidNotReceive().FetchCities();
        cache.DidNotReceive().Set(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<TimeSpan>());
    }

    [Theory]
    [InlineData("KLAIPEDA")]
    [InlineData("klaipėda")]
    [InlineData("Klaipėdą")]
    [InlineData("Klaipėda")]
    public async Task WeatherService_GetWeather_FetchesTheWeatherCorrectly(string cityName)
    {
        // Arrange
        var weather = new WeatherDto(City: "Klaipėda", Temperature: 12, Precipitation: 23, WindSpeed: 34, Summary: "Chilly");
        dataSourceClient.FetchWeather("Klaipėda").Returns(weather);
        service.GetCitiesInternal().Returns(cities.ToDictionary(x => x.RemoveDiacritics().ToLowerInvariant(), x => x));
        service.When(x => x.GetWeather(Arg.Any<string>())).CallBase();

        // Act
        var result = await service.GetWeather(cityName);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(weather);
        await service.Received(1).PersistWeather(Arg.Any<WeatherDto>());
    }

    [Fact]
    public async Task WeatherService_GetWeather_ThrowsForInvalidCity()
    {
        // Arrange
        service.GetCitiesInternal().Returns(cities.ToDictionary(x => x.RemoveDiacritics().ToLowerInvariant(), x => x));
        service.When(x => x.GetWeather(Arg.Any<string>())).CallBase();

        // Act + Assert
        await ((Func<Task>)(async () => await service.GetWeather("Non existing city"))).Should().ThrowExactlyAsync<UnknownCityException>();
    }

    [Fact]
    public async Task WeatherService_GetCities_ReturnsTheListOfCities()
    {
        // Arrange
        service.GetCitiesInternal().Returns(cities.ToDictionary(x => x.RemoveDiacritics().ToLowerInvariant(), x => x));
        service.When(x => x.GetCities()).CallBase();

        // Act
        var result = await service.GetCities();

        // Assert
        result.Should().BeEquivalentTo(cities);
    }

    [Theory]
    [InlineData("KLAIPEDA", true)]
    [InlineData("klaipėda", true)]
    [InlineData("Klaipėdą", true)]
    [InlineData("Klaipėda", true)]
    [InlineData("Plungė", false)]
    public async Task WeatherService_CityExists_CorrectlyChecksTheCityName(string cityName, bool shouldExist)
    {
        // Arrange
        service.GetCitiesInternal().Returns(cities.ToDictionary(x => x.RemoveDiacritics().ToLowerInvariant(), x => x));
        service.When(x => x.CityExists(Arg.Any<string>())).CallBase();

        // Act
        var result = await service.CityExists(cityName);

        // Asssert
        result.Should().Be(shouldExist);
    }

    [Fact]
    public async Task WeatherService_GetWeather_PersistsWeatherInStorage()
    {
        // Arrange
        var weather = new WeatherDto(City: "Klaipėda", Temperature: 12, Precipitation: 23, WindSpeed: 34, Summary: "Chilly");
        var entityId = Substitute.For<IEntityId<Weather>>();
        entityId.ToString().Returns("Klaipėda");
        service.When(x => x.PersistWeather(weather)).CallBase();
        storageClient.CreateEntityId<Weather>("Klaipėda").Returns(entityId);
        storageClient.AcquireLock(entityId).Returns(new TestLock().WaitAsyncDisposable());

        // Act
        await service.PersistWeather(weather);

        // Assert
        await storageClient.Received(1).AcquireLock(entityId);
        await storageClient.Received(1).Get(entityId);
        await storageClient.ReceivedWithAnyArgs(1).AddOrUpdate(new Weather());
    }
}