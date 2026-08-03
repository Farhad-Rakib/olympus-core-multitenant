using OlympusCoreMultitenant.Domain.Enums;

namespace OlympusCoreMultitenant.Application.Tenants.Dtos;

public sealed record TenantLicenseStatusDto(LicenseStatus Status);

public sealed record StartTrialRequestDto(DateTime TrialEndsAtUtc);

public sealed record SetLicenseExpiryRequestDto(DateTime? LicenseExpiresAtUtc);
