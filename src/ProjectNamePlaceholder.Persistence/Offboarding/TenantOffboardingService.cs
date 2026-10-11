using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Tenants.Dtos;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Offboarding;

public sealed class TenantOffboardingService : ITenantOffboardingService
{
    // Written into Changes in place of the original payload, so an anonymised row is visibly
    // redacted rather than looking like an event that genuinely had no data.
    private const string RedactedChangesMarker = "{\"redacted\":\"tenant purged\"}";

    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IFileStorage _fileStorage;
    private readonly TenantOffboardingOptions _options;

    public TenantOffboardingService(
        ApplicationDbContext dbContext,
        ICurrentTenantService currentTenantService,
        IFileStorage fileStorage,
        IOptions<TenantOffboardingOptions> options)
    {
        _dbContext = dbContext;
        _currentTenantService = currentTenantService;
        _fileStorage = fileStorage;
        _options = options.Value;
    }

    public async Task<TenantExportDto> ExportAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        using var scope = _currentTenantService.BeginScope(tenant.Id, tenant.Slug);

        var users = await _dbContext.Users.AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Select(u => new TenantExportUserDto(
                u.Id,
                u.FullName,
                u.Email,
                u.IsActive,
                u.UserRoles.Select(ur => ur.Role.Name).ToList()))
            .ToListAsync(cancellationToken);

        var roles = await _dbContext.Roles.AsNoTracking()
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Select(r => new TenantExportRoleDto(
                r.Id,
                r.Name,
                r.Description,
                r.RolePermissions.Select(rp => rp.Permission.Name).ToList()))
            .ToListAsync(cancellationToken);

        var moduleKeys = await _dbContext.TenantModules.AsNoTracking()
            .Include(tm => tm.Module)
            .Select(tm => tm.Module.Key)
            .ToListAsync(cancellationToken);

        var featureFlags = await _dbContext.TenantFeatureFlags.AsNoTracking()
            .Select(f => new TenantExportFeatureFlagDto(f.Key, f.IsEnabled, f.RolloutPercentage))
            .ToListAsync(cancellationToken);

        var siteSettings = await _dbContext.SiteSettings.AsNoTracking()
            .Select(s => new TenantExportSettingDto(s.Key, s.Value))
            .ToListAsync(cancellationToken);

        var customDomains = await _dbContext.TenantDomains.AsNoTracking()
            .Where(d => d.TenantId == tenant.Id)
            .Select(d => d.Domain)
            .ToListAsync(cancellationToken);

        return new TenantExportDto(
            tenant.Id,
            tenant.Slug,
            tenant.Name,
            DateTime.UtcNow,
            users,
            roles,
            moduleKeys,
            featureFlags,
            siteSettings,
            customDomains);
    }

    public async Task PurgeAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        using var scope = _currentTenantService.BeginScope(tenant.Id, tenant.Slug);

        // Deletion order respects the Restrict FK constraints back to Tenant/User/Role -- children
        // before the rows they reference. All removals are tracked and committed in a single
        // SaveChangesAsync so the purge is atomic (all-or-nothing) and EF orders the actual DELETE
        // statements within it.
        _dbContext.RefreshTokens.RemoveRange(await _dbContext.RefreshTokens.ToListAsync(cancellationToken));
        _dbContext.PasswordResetTokens.RemoveRange(await _dbContext.PasswordResetTokens.ToListAsync(cancellationToken));
        _dbContext.Notifications.RemoveRange(await _dbContext.Notifications.ToListAsync(cancellationToken));
        _dbContext.UserRoles.RemoveRange(await _dbContext.UserRoles.ToListAsync(cancellationToken));
        _dbContext.RolePermissions.RemoveRange(await _dbContext.RolePermissions.ToListAsync(cancellationToken));
        _dbContext.Users.RemoveRange(await _dbContext.Users.ToListAsync(cancellationToken));
        _dbContext.Roles.RemoveRange(await _dbContext.Roles.ToListAsync(cancellationToken));
        _dbContext.TenantModules.RemoveRange(await _dbContext.TenantModules.ToListAsync(cancellationToken));
        _dbContext.SiteSettings.RemoveRange(await _dbContext.SiteSettings.ToListAsync(cancellationToken));
        _dbContext.Menus.RemoveRange(await _dbContext.Menus.ToListAsync(cancellationToken));
        _dbContext.TenantFeatureFlags.RemoveRange(await _dbContext.TenantFeatureFlags.ToListAsync(cancellationToken));

        // Not ITenantEntity (looked up before any tenant context exists), so query explicitly by
        // TenantId rather than relying on the ambient scope's query filter.
        _dbContext.TenantRegistrationTokens.RemoveRange(
            await _dbContext.TenantRegistrationTokens.Where(t => t.TenantId == tenant.Id).ToListAsync(cancellationToken));
        _dbContext.TenantDomains.RemoveRange(
            await _dbContext.TenantDomains.Where(d => d.TenantId == tenant.Id).ToListAsync(cancellationToken));

        // AuditLog is the third non-ITenantEntity in this method and needs the same explicit
        // TenantId filter as the two above. Unlike them it is not simply deleted: see
        // PurgedTenantAuditHandling for why this is a configured choice. Handled BEFORE the tenant
        // row goes, while tenant.Id is still meaningful.
        await ApplyAuditHandlingAsync(tenant.Id, cancellationToken);

        _dbContext.Remove(tenant);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Deliberately AFTER the commit. The database purge is atomic; file deletion is not, and it
        // cannot be rolled back. Deleting files first would mean a failed commit leaves the tenant
        // intact but its uploads already destroyed. This ordering fails the safer way: the worst
        // case is orphaned files on disk, which can be swept up later.
        if (_options.FileHandling == PurgedTenantFileHandling.Delete)
        {
            await _fileStorage.DeleteAllForTenantAsync(tenant.Id, cancellationToken);
        }
    }

    private async Task ApplyAuditHandlingAsync(long tenantId, CancellationToken cancellationToken)
    {
        if (_options.AuditHandling == PurgedTenantAuditHandling.Retain)
        {
            return;
        }

        var auditLogs = await _dbContext.AuditLogs
            .Where(log => log.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        if (auditLogs.Count == 0)
        {
            return;
        }

        if (_options.AuditHandling == PurgedTenantAuditHandling.Delete)
        {
            _dbContext.AuditLogs.RemoveRange(auditLogs);
            return;
        }

        foreach (var log in auditLogs)
        {
            // TenantId is deliberately kept: it is an opaque identifier that no longer resolves to
            // anything once the tenant row is gone, and keeping it lets the trail still be grouped
            // by the tenant it belonged to. UserId and Changes are the parts that identify people.
            log.UserId = null;
            log.Changes = RedactedChangesMarker;
        }
    }
}
