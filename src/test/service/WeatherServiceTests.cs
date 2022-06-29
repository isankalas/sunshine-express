using Microsoft.Extensions.Logging;
using SunshineExpress.Service.Contract;

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
        cache.AcquireLock(cacheKey).Returns(_ => new TestLock().Wait());
    }

    private class TestLock : IDisposable
    {
        private readonly CancellationToken? cancellationToken;

        public bool IsLocked { get; private set; } = false;

        public TestLock(bool isLocked = false, CancellationToken? cancellationToken = null)
        {
            IsLocked = isLocked;
            this.cancellationToken = cancellationToken;
        }

        public async Task<IDisposable> Wait()
        {
            if (IsLocked && cancellationToken.HasValue)
            {
                try
                {
                    while (!cancellationToken.Value.IsCancellationRequested)
                        await Task.Delay(100, cancellationToken.Value);
                }
                catch (OperationCanceledException) { }
            }

            return this;
        }

        public void Dispose()
        {
            IsLocked = false;
        }
    }

    [Fact]
    public async Task WeatherService_GetInternalCities_LocksTheCache()
    {
        // Arrange
        var @lock = new TestLock(isLocked: true, cancellationTokenSource.Token);
        service.When(x => x.GetCitiesInternal()).CallBase();
        cache.AcquireLock(cacheKey).Returns(_ => @lock.Wait());

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
}