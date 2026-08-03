using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.Persistence.Repositories;

public sealed class SystemSettingRepository : ISystemSettingRepository
{
    private readonly ApplicationDbContext _dbContext;

    public SystemSettingRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SystemSetting> AddAsync(SystemSetting entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.SystemSettings.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task<SystemSetting?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SystemSettings.FirstOrDefaultAsync(setting => setting.Id == id, cancellationToken);
    }

    public async Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SystemSettings.FirstOrDefaultAsync(setting => setting.Key == key, cancellationToken);
    }

    public async Task<IReadOnlyList<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SystemSettings
            .AsNoTracking()
            .OrderBy(setting => setting.Key)
            .ToListAsync(cancellationToken);
    }

    public void Update(SystemSetting entity)
    {
        _dbContext.SystemSettings.Update(entity);
    }

    public void Delete(SystemSetting entity)
    {
        _dbContext.SystemSettings.Remove(entity);
    }
}
