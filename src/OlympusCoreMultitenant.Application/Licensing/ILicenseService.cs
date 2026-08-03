using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Domain.Enums;

namespace OlympusCoreMultitenant.Application.Licensing;

public interface ILicenseService
{
    // Pure, no I/O -- callers that already have a loaded Tenant (e.g. TenantResolutionMiddleware,
    // which loads one per request anyway) should use this instead of GetStatusAsync to avoid a
    // second database round trip just to re-fetch the same row.
    LicenseStatus ComputeStatus(Tenant tenant);

    Task<LicenseStatus> GetStatusAsync(long tenantId, CancellationToken cancellationToken = default);

    // Active, Trial, and GracePeriod all mean "let the request through"; Expired and Suspended
    // mean "block it" -- the one place this Active/Trial/GracePeriod/Expired/Suspended -> allow/deny
    // mapping is decided, so callers never have to enumerate LicenseStatus themselves.
    bool IsAccessAllowed(LicenseStatus status);
}
