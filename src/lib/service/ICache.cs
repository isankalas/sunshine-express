namespace SunshineExpress.Service;

public interface ICache
{
    bool TryGetValue<TValue>(string key, out TValue value);

    void Set(string key, object value, TimeSpan expiration);

    IDisposable AcquireLock(string key);
}