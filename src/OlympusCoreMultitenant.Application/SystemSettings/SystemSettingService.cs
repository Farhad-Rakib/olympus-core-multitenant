using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Application.SystemSettings.Dtos;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Application.SystemSettings;

public sealed class SystemSettingService : ISystemSettingService
{
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SystemSettingService(ISystemSettingRepository systemSettingRepository, IUnitOfWork unitOfWork)
    {
        _systemSettingRepository = systemSettingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _systemSettingRepository.GetAllAsync(cancellationToken);
        return settings.Select(MapToDto).ToList();
    }

    public async Task<SystemSettingDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var setting = await _systemSettingRepository.GetByKeyAsync(key, cancellationToken);
        return setting is null ? null : MapToDto(setting);
    }

    public async Task<SystemSettingDto> CreateOrUpdateAsync(SystemSettingDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await _systemSettingRepository.GetByKeyAsync(dto.Key, cancellationToken);
        if (existing is null)
        {
            var created = new SystemSetting
            {
                Key = dto.Key,
                Value = dto.Value,
                Description = dto.Description
            };

            await _systemSettingRepository.AddAsync(created, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToDto(created);
        }

        existing.Value = dto.Value;
        existing.Description = dto.Description;

        _systemSettingRepository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(existing);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var setting = await _systemSettingRepository.GetByIdAsync(id, cancellationToken);
        if (setting is null)
        {
            return;
        }

        _systemSettingRepository.Delete(setting);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static SystemSettingDto MapToDto(SystemSetting setting)
        => new(setting.Id, setting.Key, setting.Value, setting.Description);
}
