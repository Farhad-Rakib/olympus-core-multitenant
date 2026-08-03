namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface IDistributedBootLock
{
    // Blocks until this process holds the lock (waiting out any other instance's boot sequence
    // rather than racing it), then returns a handle that releases the lock on disposal.
    Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken = default);
}
