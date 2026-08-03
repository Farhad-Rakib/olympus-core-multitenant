namespace ProjectNamePlaceholder.Application.Tenants.Dtos;

// Deliberately excludes PasswordHash/RefreshTokens/PasswordResetTokens (credentials, not data the
// tenant needs back) and AuditLogs (retained separately as the platform's own compliance record of
// what happened to this tenant, including of the export/purge itself -- bundling them into the
// thing being exported/purged would defeat that purpose).
public sealed record TenantExportDto(
    long TenantId,
    string Slug,
    string Name,
    DateTime ExportedAtUtc,
    IReadOnlyList<TenantExportUserDto> Users,
    IReadOnlyList<TenantExportRoleDto> Roles,
    IReadOnlyList<string> ModuleKeys,
    IReadOnlyList<TenantExportFeatureFlagDto> FeatureFlags,
    IReadOnlyList<TenantExportSettingDto> SiteSettings,
    IReadOnlyList<string> CustomDomains);

public sealed record TenantExportUserDto(long Id, string FullName, string Email, bool IsActive, IReadOnlyList<string> Roles);

public sealed record TenantExportRoleDto(long Id, string Name, string Description, IReadOnlyList<string> Permissions);

public sealed record TenantExportFeatureFlagDto(string Key, bool IsEnabled, int RolloutPercentage);

public sealed record TenantExportSettingDto(string Key, string Value);
