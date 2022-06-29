namespace SunshineExpress.Storage.Memory;

internal class SemaphoreWrapperLock : IAsyncDisposable
{
    private readonly SemaphoreSlim semaphore;

    private SemaphoreWrapperLock(SemaphoreSlim semaphore)
    {
        this.semaphore = semaphore;
    }

    public static async Task<SemaphoreWrapperLock> AcquireLock(SemaphoreSlim semaphore)
    {
        var wrapper = new SemaphoreWrapperLock(semaphore);
        await semaphore.WaitAsync();
        return wrapper;
    }

    public ValueTask DisposeAsync()
    {
        semaphore.Release();
        return ValueTask.CompletedTask;
    }
}