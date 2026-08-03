using ProjectNamePlaceholder.Application.SystemSettings.Dtos;

namespace ProjectNamePlaceholder.Application.SystemSettings;

public interface ISystemSettingService
{
    Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SystemSettingDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<SystemSettingDto> CreateOrUpdateAsync(SystemSettingDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
