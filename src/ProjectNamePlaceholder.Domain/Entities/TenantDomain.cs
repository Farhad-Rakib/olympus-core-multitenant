namespace ProjectNamePlaceholder.Domain.Entities;

// Deliberately not ITenantEntity: resolving a tenant from an inbound Host header happens before
// any tenant context exists, so this lookup must never be subject to the ambient-tenant query
// filter the way ITenantEntity rows are.
public sealed class TenantDomain : BaseEntity
{
    public long TenantId { get; private set; }
    public string Domain { get; private set; } = string.Empty;

    private TenantDomain()
    {
    }

    public TenantDomain(long tenantId, string domain)
    {
        TenantId = tenantId;
        Domain = !string.IsNullOrWhiteSpace(domain)
            ? domain.Trim().ToLowerInvariant()
            : throw new ArgumentException("Domain is required.", nameof(domain));
    }
}
