using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.Persistence.Repositories;

public sealed class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    // NOTE: explicit `.Where(x => x.TenantId == DbContext.CurrentTenantId)` is required here even
    // though User has an automatic global query filter -- confirmed empirically that EF Core drops
    // the filter from the LIMIT-1 pushdown subquery it generates for Include()+FirstOrDefaultAsync
    // on this exact shape. See ApplicationDbContext.CurrentTenantId for details.
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbContext.Users
            .Where(u => u.TenantId == DbContext.CurrentTenantId)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByEmailAcrossTenantsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbContext.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByIdWithRolesAsync(long id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Users
            .Where(u => u.TenantId == DbContext.CurrentTenantId)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(role => role.RolePermissions)
                        .ThenInclude(rolePermission => rolePermission.Permission)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<int> CountByTenantAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Users
            .Where(u => u.TenantId == DbContext.CurrentTenantId)
            .CountAsync(cancellationToken);
    }

    public override async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Users
            .AsNoTracking()
            .Where(u => u.TenantId == DbContext.CurrentTenantId)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .ToListAsync(cancellationToken);
    }
}
