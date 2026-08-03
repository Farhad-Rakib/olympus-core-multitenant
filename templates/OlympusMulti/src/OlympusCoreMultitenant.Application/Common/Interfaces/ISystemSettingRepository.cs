using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Application.Common.Interfaces;

public interface ISystemSettingRepository
{
    Task<SystemSetting> AddAsync(SystemSetting entity, CancellationToken cancellationToken = default);
    Task<SystemSetting?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default);
    void Update(SystemSetting entity);
    void Delete(SystemSetting entity);
}
