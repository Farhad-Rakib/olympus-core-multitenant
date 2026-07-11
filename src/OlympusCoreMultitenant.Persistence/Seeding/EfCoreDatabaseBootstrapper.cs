using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.Persistence.Seeding;

public sealed class EfCoreDatabaseBootstrapper : IDatabaseBootstrapper
{
    private const string DefaultTenantSlug = "default";

    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly ILogger<EfCoreDatabaseBootstrapper> _logger;

    public EfCoreDatabaseBootstrapper(
        ApplicationDbContext dbContext,
        ITenantProvisioningService tenantProvisioningService,
        ILogger<EfCoreDatabaseBootstrapper> logger)
    {
        _dbContext = dbContext;
        _tenantProvisioningService = tenantProvisioningService;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        _logger?.Log(LogLevel.Debug, "Attempting database connection and migration...");
        // Always use MigrateAsync - it handles both initial creation and applies pending migrations
        // while properly tracking them in __EFMigrationsHistory table. The AddTenantSupport migration
        // also creates the "default" tenant row as part of its backfill.
        await _dbContext.Database.MigrateAsync(cancellationToken);
        _logger?.Log(LogLevel.Debug, "Database migration applied successfully.");
    }

    public async Task SeedDefaultTenantAsync(CancellationToken cancellationToken = default)
    {
        await DefaultSystemSettingsSeeder.SeedAsync(_dbContext);

        var defaultTenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Slug == DefaultTenantSlug, cancellationToken);

        if (defaultTenant is null)
        {
            defaultTenant = new Tenant(DefaultTenantSlug, "Default");
            _dbContext.Tenants.Add(defaultTenant);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger?.Log(LogLevel.Debug, "Provisioning default tenant...");
        await _tenantProvisioningService.ProvisionAsync(defaultTenant, moduleKeys: null, cancellationToken);
        _logger?.Log(LogLevel.Debug, "Default tenant provisioning complete.");
    }
}
