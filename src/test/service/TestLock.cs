namespace SunshineExpress.Service.Test;

internal class TestLock : IDisposable, IAsyncDisposable
{
    private readonly CancellationToken? cancellationToken;

    public bool IsLocked { get; private set; } = false;

    public TestLock(bool isLocked = false, CancellationToken? cancellationToken = null)
    {
        IsLocked = isLocked;
        this.cancellationToken = cancellationToken;
    }

    public async Task<IDisposable> WaitDisposable()
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

    public async Task<IAsyncDisposable> WaitAsyncDisposable()
    {
        await WaitDisposable();

        return this;
    }

    public void Dispose()
    {
        IsLocked = false;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}