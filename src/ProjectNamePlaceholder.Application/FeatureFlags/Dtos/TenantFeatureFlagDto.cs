namespace ProjectNamePlaceholder.Application.FeatureFlags.Dtos;

public sealed record TenantFeatureFlagDto(long Id, string Key, bool IsEnabled, int RolloutPercentage);

public sealed record SetTenantFeatureFlagRequestDto(string Key, bool IsEnabled, int RolloutPercentage = 100);
