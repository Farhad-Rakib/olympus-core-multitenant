using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Repositories;

public sealed class TenantRepository : BaseRepository<Tenant>, ITenantRepository
{
    public TenantRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await DbContext.Tenants
            .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
    }
}
