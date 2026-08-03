using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Repositories;

public sealed class TenantRegistrationTokenRepository : BaseRepository<TenantRegistrationToken>, ITenantRegistrationTokenRepository
{
    public TenantRegistrationTokenRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<TenantRegistrationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
    }

    public async Task<TenantRegistrationToken?> GetPendingByTenantIdAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.TenantId == tenantId && x.SubmittedAtUtc != null && x.UsedAtUtc == null)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
