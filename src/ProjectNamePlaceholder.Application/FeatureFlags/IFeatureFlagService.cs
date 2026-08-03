using ProjectNamePlaceholder.Application.FeatureFlags.Dtos;

namespace ProjectNamePlaceholder.Application.FeatureFlags;

public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantFeatureFlagDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TenantFeatureFlagDto> SetAsync(SetTenantFeatureFlagRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
