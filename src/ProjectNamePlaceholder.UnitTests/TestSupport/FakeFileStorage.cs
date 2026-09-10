using ProjectNamePlaceholder.Application.Common.Interfaces;

namespace ProjectNamePlaceholder.UnitTests.TestSupport;

// Records which tenants had their files deleted, so a purge test can assert on the call rather than
// touching the real filesystem.
public sealed class FakeFileStorage : IFileStorage
{
    public List<long> DeletedTenantIds { get; } = [];

    public Task<int> DeleteAllForTenantAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        DeletedTenantIds.Add(tenantId);
        return Task.FromResult(1);
    }

    public Task<StoredFile> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken = default)
        => Task.FromResult(new StoredFile("1/x.png", "x.png", 0));

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
        => Task.FromResult<Stream?>(null);

    public Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
