using OlympusCoreMultitenant.Domain.Common;

namespace OlympusCoreMultitenant.Domain.Entities;

public sealed class UserRole : ITenantEntity
{
    public long TenantId { get; set; }
    public long UserId { get; set; }
    public long RoleId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
