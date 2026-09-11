namespace ProjectNamePlaceholder.Application.Common.Configuration;

public sealed class RateLimitingOptions
{
    public bool Enabled { get; set; } = true;

    // Default per-TENANT budget within the window, used when the tenant's plan does not set
    // RequestsPerMinute. Every authenticated request is partitioned by tenant_id, so one tenant's
    // burst can only exhaust its own bucket.
    public int PermitLimit { get; set; } = 300;
    public int WindowSeconds { get; set; } = 60;

    // Anonymous traffic (login, registration, login-config, translations) is partitioned by remote
    // IP with its own, tighter budget: it is the surface a credential-stuffing script hits, and it
    // must not share a bucket with -- or be sized like -- a paying tenant's API use.
    public int AnonymousPermitLimit { get; set; } = 60;
    public int AnonymousWindowSeconds { get; set; } = 60;

    // Requests allowed to wait for a slot instead of being rejected immediately once the limit is hit.
    public int QueueLimit { get; set; } = 0;
}
