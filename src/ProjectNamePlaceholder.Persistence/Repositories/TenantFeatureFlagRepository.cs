using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Repositories;

public sealed class TenantFeatureFlagRepository : ITenantFeatureFlagRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TenantFeatureFlagRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantFeatureFlag> AddAsync(TenantFeatureFlag entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.TenantFeatureFlags.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task<TenantFeatureFlag?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantFeatureFlags.FirstOrDefaultAsync(flag => flag.Id == id, cancellationToken);
    }

    public async Task<TenantFeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantFeatureFlags.FirstOrDefaultAsync(flag => flag.Key == key, cancellationToken);
    }

    public async Task<IReadOnlyList<TenantFeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantFeatureFlags
            .AsNoTracking()
            .OrderBy(flag => flag.Key)
            .ToListAsync(cancellationToken);
    }

    public void Update(TenantFeatureFlag entity)
    {
        _dbContext.TenantFeatureFlags.Update(entity);
    }

    public void Delete(TenantFeatureFlag entity)
    {
        _dbContext.TenantFeatureFlags.Remove(entity);
    }
}
