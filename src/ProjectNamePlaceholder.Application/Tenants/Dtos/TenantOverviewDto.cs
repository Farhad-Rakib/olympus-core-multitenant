using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Application.Tenants.Dtos;

// What a tenant's own dashboard needs in one round trip: who they are, what they bought, how long
// it lasts, and which modules that entitles them to.
public sealed record TenantOverviewDto(
    string Name,
    string Slug,
    TenantPlanDto? Plan,
    LicenseStatus LicenseStatus,
    DateTime? TrialEndsAtUtc,
    DateTime? LicenseExpiresAtUtc,
    int UserCount,
    IReadOnlyList<TenantModuleSummaryDto> Modules);

public sealed record TenantPlanDto(string Key, string Name, string Description, int MaxUsers);

// IsEnabled distinguishes "in your plan and switched on" from "in your plan but currently off",
// which is what makes the module list actionable rather than decorative.
public sealed record TenantModuleSummaryDto(string Key, string Name, string Description, bool IsEnabled);
