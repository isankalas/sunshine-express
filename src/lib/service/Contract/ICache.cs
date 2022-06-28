namespace SunshineExpress.Service.Contract;

/// <summary>
/// Declares the necessary caching methods required for the service to run.
/// </summary>
public interface ICache
{
    /// <summary>
    /// Tries to fetch the value from the cache if it exists.
    /// </summary>
    /// <typeparam name="TValue">Type of the value in the cache.</typeparam>
    /// <param name="key">The identifier of the <paramref name="value"/></param>
    /// <param name="value">Value is set if the cache item with the given <paramref name="key"/> exists.</param>
    /// <returns>A flag indicating whether the item was found in the cache.</returns>
    bool TryGetValue<TValue>(string key, out TValue value);

    /// <summary>
    /// Puts an item into the cache.
    /// </summary>
    /// <param name="key">The identifier of the <paramref name="value"/></param>
    /// <param name="value">A value to store in the cache.</param>
    /// <param name="expiration">Absolute expiration duration relative to the current time.</param>
    void Set(string key, object value, TimeSpan expiration);

    /// <summary>
    /// Gets an object helping synchronize concurrent operations on the same cache item.
    /// The lock is released when the return value is disposed.
    /// </summary>
    /// <param name="key">A key in the cache to lock.</param>
    /// <returns>A disposable object which releases the lock when disposed.</returns>
    /// <remarks>The cache item should not actually get locked in the cache. The method should only return an object to help synchronize concurrent operations.</remarks>
    Task<IDisposable> AcquireLock(string key);
}