using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Application.Tenants.Dtos;

public sealed record TenantLicenseStatusDto(LicenseStatus Status);

public sealed record StartTrialRequestDto(DateTime TrialEndsAtUtc);

public sealed record SetLicenseExpiryRequestDto(DateTime? LicenseExpiresAtUtc);
