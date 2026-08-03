namespace OlympusCoreMultitenant.Application.Common.Configuration;

public sealed class TenantOffboardingOptions
{
    // A tenant must have been soft-disabled for at least this long before it can be hard-deleted,
    // so a purge can never be the very next action after a disable -- there's always a window to
    // notice and reverse it via Enable() first.
    public int PurgeCoolingOffDays { get; set; } = 7;
}
