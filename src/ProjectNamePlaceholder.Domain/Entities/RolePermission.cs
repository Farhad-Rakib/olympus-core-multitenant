using ProjectNamePlaceholder.Domain.Common;

namespace ProjectNamePlaceholder.Domain.Entities;

public sealed class RolePermission : ITenantEntity
{
    public long TenantId { get; set; }
    public long RoleId { get; set; }
    public long PermissionId { get; set; }

    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
