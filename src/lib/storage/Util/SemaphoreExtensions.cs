namespace SunshineExpress.Storage.Util;

/// <summary>
/// Shorthand operations for running a piece of code within a <see cref="Semaphore"/> or <see cref="SemaphoreSlim"/> lock.
/// </summary>
internal static class SemaphoreExtensions
{
    public static void RunLocked(this SemaphoreSlim semaphore, Action action)
    {
        semaphore.Wait();
        try
        {
            action.Invoke();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static TResult RunLocked<TResult>(this SemaphoreSlim semaphore, Func<TResult> action)
    {
        semaphore.Wait();
        try
        {
            return action.Invoke();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static Task RunLockedAsync(this SemaphoreSlim semaphore, Action action)
        => semaphore.RunLockedAsync(Task.Run(action));

    public static async Task RunLockedAsync(this SemaphoreSlim semaphore, Task action)
    {
        await semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            await action.ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static Task<TResult> RunLockedAsync<TResult>(this SemaphoreSlim semaphore, Func<TResult> action)
        => semaphore.RunLockedAsync(Task.Run(action));

    public static async Task<TResult> RunLockedAsync<TResult>(this SemaphoreSlim semaphore, Task<TResult> action)
    {
        await semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            return await action.ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static async Task<TResult> RunLockedAsync<TResult>(this SemaphoreSlim semaphore, Func<Task<TResult>> action)
    {
        await semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            return await Task.Run(action).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
