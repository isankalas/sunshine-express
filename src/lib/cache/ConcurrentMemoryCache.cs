using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SunshineExpress.Service.Contract;
using System.Collections.Concurrent;

namespace SunshineExpress.Cache;

/// <summary>
/// Implements the caching mechanism as a memory cache for <see cref="SunshineExpress.WeatherService" />.
/// </summary>
public class ConcurrentMemoryCache : ICache, IDisposable
{
    private readonly MemoryCache memoryCache = new(new MemoryCacheOptions());
    private readonly ConcurrentDictionary<string, CacheLock> locks = new();
    private readonly ILogger<ConcurrentMemoryCache> logger;
    private bool disposedValue;

    public ConcurrentMemoryCache(ILogger<ConcurrentMemoryCache> logger)
    {
        this.logger = logger;
    }

    private class CacheLock : IDisposable
    {
        private readonly string key;

        private readonly ILogger logger;

        public SemaphoreSlim Lock { get; private set; } = new SemaphoreSlim(1);

        public CacheLock(string key, ILogger logger)
        {
            this.key = key;
            this.logger = logger;
        }

        public void Dispose()
        {
            logger.LogDebug($"Releasing cache lock for {key}.");
            Lock.Release();
        }

        public void ActualDispose()
        {
            Lock.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<IDisposable> AcquireLock(string key)
    {
        logger.LogDebug($"Acquiring cache lock for \"{key}\"");
        var @lock = locks.GetOrAdd(key, key => new CacheLock(key, logger));

        await @lock.Lock.WaitAsync();
        logger.LogDebug($"Acquired cache lock for \"{key}\"");
        return @lock;
    }

    /// <inheritdoc />
    public void Set(string key, object value, TimeSpan expiration)
        => memoryCache.Set(key, value, expiration);

    /// <inheritdoc />
    public bool TryGetValue<TValue>(string key, out TValue value)
        => memoryCache.TryGetValue(key, out value);

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                foreach (var @lock in locks)
                    @lock.Value.ActualDispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}