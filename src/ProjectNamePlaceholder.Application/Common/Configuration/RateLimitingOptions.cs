namespace ProjectNamePlaceholder.Application.Common.Configuration;

public sealed class RateLimitingOptions
{
    public bool Enabled { get; set; } = true;

    // Requests allowed per partition (tenant, or remote IP when no tenant claim is present) within
    // the sliding window below.
    public int PermitLimit { get; set; } = 300;
    public int WindowSeconds { get; set; } = 60;

    // Requests allowed to wait for a slot instead of being rejected immediately once the limit is hit.
    public int QueueLimit { get; set; } = 0;
}
