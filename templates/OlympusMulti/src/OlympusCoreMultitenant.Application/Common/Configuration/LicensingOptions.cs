namespace OlympusCoreMultitenant.Application.Common.Configuration;

public sealed class LicensingOptions
{
    // How long a tenant stays reachable (LicenseStatus.GracePeriod) after LicenseExpiresAtUtc has
    // passed, before flipping to Expired. Global rather than per-tenant to keep this simple; revisit
    // only if a real customer needs a different grace window.
    public int GracePeriodDays { get; set; } = 7;
}
