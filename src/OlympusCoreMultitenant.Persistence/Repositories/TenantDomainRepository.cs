using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.Persistence.Repositories;

public sealed class TenantDomainRepository : ITenantDomainRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TenantDomainRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantDomain> AddAsync(TenantDomain entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.TenantDomains.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task<TenantDomain?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantDomains.FirstOrDefaultAsync(domain => domain.Id == id, cancellationToken);
    }

    public async Task<TenantDomain?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantDomains.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Domain == domain, cancellationToken);
    }

    public async Task<IReadOnlyList<TenantDomain>> GetByTenantIdAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantDomains.AsNoTracking()
            .Where(d => d.TenantId == tenantId)
            .OrderBy(d => d.Domain)
            .ToListAsync(cancellationToken);
    }

    public void Delete(TenantDomain entity)
    {
        _dbContext.TenantDomains.Remove(entity);
    }
}
