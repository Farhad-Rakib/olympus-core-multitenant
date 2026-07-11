using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Application.SiteSettings.Dtos;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Application.SiteSettings;

public sealed class SiteSettingService : ISiteSettingService
{
    private readonly ISiteSettingRepository _siteSettingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SiteSettingService(ISiteSettingRepository siteSettingRepository, IUnitOfWork unitOfWork)
    {
        _siteSettingRepository = siteSettingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<SiteSettingDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _siteSettingRepository.GetAllAsync(cancellationToken);
        return settings.Select(MapToDto).ToList();
    }

    public async Task<SiteSettingDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var setting = await _siteSettingRepository.GetByKeyAsync(key, cancellationToken);
        return setting is null ? null : MapToDto(setting);
    }

    public async Task<SiteSettingDto> CreateOrUpdateAsync(SiteSettingDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await _siteSettingRepository.GetByKeyAsync(dto.Key, cancellationToken);
        if (existing is null)
        {
            var created = new SiteSetting
            {
                Id =  dto.Id,
                Key = dto.Key,
                Value = dto.Value,
                Description = dto.Description
            };

            await _siteSettingRepository.AddAsync(created, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToDto(created);
        }

        existing.Value = dto.Value;
        existing.Description = dto.Description;

        _siteSettingRepository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(existing);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var setting = await _siteSettingRepository.GetByIdAsync(id, cancellationToken);
        if (setting is null)
        {
            return;
        }

        _siteSettingRepository.Delete(setting);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static SiteSettingDto MapToDto(SiteSetting setting)
        => new(setting.Id, setting.Key, setting.Value, setting.Description);
}
