namespace OlympusCoreMultitenant.Domain.Enums;

public enum AuditAction
{
    Created,
    Updated,
    Deleted,

    // Read-only access worth its own trail entry even though nothing changed -- e.g. a platform
    // admin viewing another tenant's data via the X-Tenant-Id override. Never emitted by the
    // generic SaveChanges interceptor, only by call sites that explicitly need an access record.
    Accessed
}
