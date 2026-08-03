using OlympusCoreMultitenant.Domain.Common;

namespace OlympusCoreMultitenant.Domain.Entities;

public sealed class TenantFeatureFlag : BaseEntity, ITenantEntity
{
    public long TenantId { get; set; }
    public string Key { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }

    // Deterministic gradual rollout across tenants sharing the same flag key (0-100). A tenant
    // rolls in/out consistently across requests since the bucket is derived from TenantId, not
    // re-randomized per check. 100 (default) means "fully on for this tenant whenever IsEnabled".
    public int RolloutPercentage { get; set; } = 100;
}
