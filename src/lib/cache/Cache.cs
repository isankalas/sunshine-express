using Microsoft.Extensions.Caching.Memory;
using SunshineExpress.Service.Contract;
using System.Collections.Concurrent;

namespace SunshineExpress.Cache;

public class Cache : ICache, IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly ConcurrentDictionary<string, CacheLock> locks = new ConcurrentDictionary<string, CacheLock>();
    private bool disposedValue;

    private class CacheLock : IDisposable
    {
        public SemaphoreSlim Lock { get; private set; } = new SemaphoreSlim(1);

        public void Dispose()
        {
            Lock.Release();
        }

        public void ActualDispose()
        {
            Lock.Dispose();
        }
    }

    public async Task<IDisposable> AcquireLock(string key)
    {
        var @lock = locks.GetOrAdd(key, key => new CacheLock());

        await @lock.Lock.WaitAsync();
        return @lock;
    }

    public void Set(string key, object value, TimeSpan expiration)
        => _memoryCache.Set(key, value, expiration);

    public bool TryGetValue<TValue>(string key, out TValue value)
        => _memoryCache.TryGetValue(key, out value);

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