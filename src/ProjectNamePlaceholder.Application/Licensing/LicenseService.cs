using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Application.Licensing;

public sealed class LicenseService : ILicenseService
{
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly LicensingOptions _options;

    public LicenseService(IRepository<Tenant> tenantRepository, IOptions<LicensingOptions> options)
    {
        _tenantRepository = tenantRepository;
        _options = options.Value;
    }

    public LicenseStatus ComputeStatus(Tenant tenant)
    {
        // Suspension is a manual admin action (e.g. non-payment) and overrides everything else,
        // even an otherwise-active trial or unexpired license.
        if (tenant.IsSuspended)
        {
            return LicenseStatus.Suspended;
        }

        var now = DateTime.UtcNow;

        if (tenant.TrialEndsAtUtc is not null && now <= tenant.TrialEndsAtUtc.Value)
        {
            return LicenseStatus.Trial;
        }

        // No expiry set means an unmanaged/perpetual license (e.g. the bootstrap default tenant) --
        // never expires on its own.
        if (tenant.LicenseExpiresAtUtc is null)
        {
            return LicenseStatus.Active;
        }

        if (now <= tenant.LicenseExpiresAtUtc.Value)
        {
            return LicenseStatus.Active;
        }

        var gracePeriodEndsAtUtc = tenant.LicenseExpiresAtUtc.Value.AddDays(_options.GracePeriodDays);
        return now <= gracePeriodEndsAtUtc ? LicenseStatus.GracePeriod : LicenseStatus.Expired;
    }

    public async Task<LicenseStatus> GetStatusAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404);

        return ComputeStatus(tenant);
    }

    public bool IsAccessAllowed(LicenseStatus status) =>
        status is LicenseStatus.Active or LicenseStatus.Trial or LicenseStatus.GracePeriod;
}
