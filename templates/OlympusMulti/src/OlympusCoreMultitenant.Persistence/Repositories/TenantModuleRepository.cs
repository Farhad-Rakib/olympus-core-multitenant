using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.Persistence.Repositories;

public sealed class TenantModuleRepository : ITenantModuleRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TenantModuleRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<long>> GetEntitledModuleIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantModules
            .AsNoTracking()
            .Select(tenantModule => tenantModule.ModuleId)
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantModule?> GetAsync(long moduleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantModules
            .FirstOrDefaultAsync(tenantModule => tenantModule.ModuleId == moduleId, cancellationToken);
    }

    public async Task AddAsync(TenantModule tenantModule, CancellationToken cancellationToken = default)
    {
        await _dbContext.TenantModules.AddAsync(tenantModule, cancellationToken);
    }

    public void Remove(TenantModule tenantModule)
    {
        _dbContext.TenantModules.Remove(tenantModule);
    }
}
