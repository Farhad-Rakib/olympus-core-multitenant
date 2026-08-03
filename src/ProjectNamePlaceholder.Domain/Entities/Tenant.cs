namespace ProjectNamePlaceholder.Domain.Entities;

public sealed class Tenant : BaseEntity
{
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public long? SubscriptionPlanId { get; private set; }
    public bool IsProvisioned { get; private set; }

    // Set when Disable() runs, cleared on Enable(). Used to gate hard-delete (purge) behind a
    // cooling-off period after soft-disable, rather than making purge immediately available -- see
    // ITenantService.PurgeAsync.
    public DateTime? DisabledAtUtc { get; private set; }

    // Raw licensing facts -- LicenseStatus is deliberately never stored directly, only derived from
    // these (plus a globally-configured grace period) by ILicenseService, so status can never drift
    // out of sync with the dates/flag that actually define it. Null TrialEndsAtUtc/LicenseExpiresAtUtc
    // means "not on a trial" / "no expiry set" (e.g. the bootstrap default tenant) respectively.
    public DateTime? TrialEndsAtUtc { get; private set; }
    public DateTime? LicenseExpiresAtUtc { get; private set; }
    public bool IsSuspended { get; private set; }

    private Tenant()
    {
    }

    public Tenant(string slug, string name)
    {
        Slug = !string.IsNullOrWhiteSpace(slug)
            ? slug.Trim().ToLowerInvariant()
            : throw new ArgumentException("Tenant slug is required.", nameof(slug));

        Name = !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : throw new ArgumentException("Tenant name is required.", nameof(name));
    }

    public void Update(string name)
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : throw new ArgumentException("Tenant name is required.", nameof(name));
    }

    public void Disable()
    {
        IsActive = false;
        DisabledAtUtc = DateTime.UtcNow;
    }

    public void Enable()
    {
        IsActive = true;
        DisabledAtUtc = null;
    }

    public void AssignSubscriptionPlan(long? subscriptionPlanId)
    {
        SubscriptionPlanId = subscriptionPlanId;
    }

    public void MarkProvisioned()
    {
        IsProvisioned = true;
    }

    public void StartTrial(DateTime trialEndsAtUtc)
    {
        TrialEndsAtUtc = trialEndsAtUtc;
    }

    public void SetLicenseExpiry(DateTime? licenseExpiresAtUtc)
    {
        LicenseExpiresAtUtc = licenseExpiresAtUtc;
    }

    public void Suspend()
    {
        IsSuspended = true;
    }

    public void Unsuspend()
    {
        IsSuspended = false;
    }
}
