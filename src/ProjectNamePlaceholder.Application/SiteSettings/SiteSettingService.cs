using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.SiteSettings.Dtos;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.SiteSettings;

public sealed class SiteSettingService : ISiteSettingService
{
    // Site settings are a free-form key/value store, so secrets share a table with cosmetic values.
    // Matched by key NAME rather than an allow-list, so a secret added later is masked by default
    // instead of leaking until someone remembers to register it. The frontend masks too, but that
    // is cosmetic only -- the real value was still on the wire until this moved server-side.
    private static readonly System.Text.RegularExpressions.Regex SecretKeyPattern =
        new("(password|secret|token|credential|apikey|api_key)", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

    public const string MaskedPlaceholder = "********";

    private const string TitleKey = "Site.Title";
    private const string LogoUrlKey = "Site.LogoUrl";
    private const string TaglineKey = "Site.Tagline";
    private const string FaviconUrlKey = "Site.FaviconUrl";
    private const string BrandColorKey = "UI.BrandColor";
    private const string SupportEmailKey = "Site.SupportEmail";

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

        // A client that read a masked secret and saved the form back would otherwise overwrite the
        // real value with the mask itself. Treat "unchanged mask" as "leave the secret alone".
        var isUnchangedSecret = IsSecret(dto.Key) && dto.Value == MaskedPlaceholder;
        if (!isUnchangedSecret)
        {
            existing.Value = dto.Value;
        }

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

    public async Task<TenantBrandingDto> GetTenantBrandingAsync(CancellationToken cancellationToken = default)
    {
        var title = await _siteSettingRepository.GetByKeyAsync(TitleKey, cancellationToken);
        var logoUrl = await _siteSettingRepository.GetByKeyAsync(LogoUrlKey, cancellationToken);
        var tagline = await _siteSettingRepository.GetByKeyAsync(TaglineKey, cancellationToken);
        var faviconUrl = await _siteSettingRepository.GetByKeyAsync(FaviconUrlKey, cancellationToken);
        var brandColor = await _siteSettingRepository.GetByKeyAsync(BrandColorKey, cancellationToken);
        var supportEmail = await _siteSettingRepository.GetByKeyAsync(SupportEmailKey, cancellationToken);

        return new TenantBrandingDto(
            title?.Value,
            logoUrl?.Value,
            tagline?.Value,
            faviconUrl?.Value,
            brandColor?.Value,
            supportEmail?.Value);
    }

    private static SiteSettingDto MapToDto(SiteSetting setting)
        => new(setting.Id, setting.Key, IsSecret(setting.Key) ? MaskedPlaceholder : setting.Value, setting.Description);

    private static bool IsSecret(string key) => SecretKeyPattern.IsMatch(key);
}
