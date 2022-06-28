namespace SunshineExpress.Service.Contract;

public interface ICache
{
    bool TryGetValue<TValue>(string key, out TValue value);

    void Set(string key, object value, TimeSpan expiration);

    Task<IDisposable> AcquireLock(string key);
}