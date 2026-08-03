using ProjectNamePlaceholder.Domain.Common;

namespace ProjectNamePlaceholder.Domain.Entities;

public sealed class TenantModule : ITenantEntity
{
    public long TenantId { get; set; }
    public long ModuleId { get; set; }

    public Module Module { get; set; } = null!;
}
