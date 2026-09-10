namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface IAuditLogRetentionService
{
    // Returns how many rows were deleted, so the caller can log it and tell whether a run hit its
    // batch ceiling and left a backlog.
    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default);
}
