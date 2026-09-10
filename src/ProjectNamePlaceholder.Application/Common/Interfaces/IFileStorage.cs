namespace ProjectNamePlaceholder.Application.Common.Interfaces;

// StorageKey is the opaque identifier a later read passes back -- never a client-supplied path.
public sealed record StoredFile(string StorageKey, string FileName, long SizeBytes);

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken = default);

    // Null when the key does not resolve, rather than throwing: "not found" is an ordinary outcome
    // for a caller-supplied key and the controller turns it into a 404.
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default);

    // Removes every file belonging to a tenant. Takes the id explicitly rather than reading ambient
    // scope: the caller is an offboarding routine acting ON a tenant, and depending on ambient state
    // for a bulk delete is how you delete the wrong tenant's files. Returns the number removed.
    Task<int> DeleteAllForTenantAsync(long tenantId, CancellationToken cancellationToken = default);
}
