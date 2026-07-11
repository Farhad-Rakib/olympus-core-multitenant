using System.Reflection;
using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.Common.Exceptions;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Common;
using OlympusCoreMultitenant.Domain.Entities;
using Module = OlympusCoreMultitenant.Domain.Entities.Module;

namespace OlympusCoreMultitenant.Persistence.Context;

public sealed class ApplicationDbContext : DbContext
{
    private readonly ICurrentTenantService _currentTenantService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantService currentTenantService)
        : base(options)
    {
        _currentTenantService = currentTenantService;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<TenantModule> TenantModules => Set<TenantModule>();

    // Referenced as `this.CurrentTenantId` inside the query filter lambdas built in SetTenantFilter.
    // EF Core's model is cached across DbContext instances (OnModelCreating runs once per app
    // lifetime), so a filter closing over an injected SERVICE reference (e.g. a captured
    // ICurrentTenantService parameter) gets baked into the cached model's expression tree and is
    // NOT guaranteed to be re-evaluated per DbContext instance/request -- confirmed empirically: it
    // froze to whatever tenant was ambient the first time each query shape compiled. Referencing an
    // INSTANCE property via `this` is EF Core's documented pattern for dynamic global query filters
    // specifically because EF substitutes `this` with the actual DbContext instance being queried,
    // re-evaluating per instance rather than baking in a closure-captured value.
    // Internal (not private): repository methods that combine Include()/ThenInclude() with
    // FirstOrDefaultAsync/SingleOrDefaultAsync on an ITenantEntity must add an EXPLICIT
    // `.Where(x => x.TenantId == DbContext.CurrentTenantId)` -- confirmed empirically that EF Core's
    // automatic global query filter is silently dropped from the LIMIT-1 pushdown subquery it
    // generates for this exact query shape (Include + FirstOrDefault), independent of the fix above.
    internal long? CurrentTenantId => _currentTenantService.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        var setTenantFilterMethod = typeof(ApplicationDbContext)
            .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                setTenantFilterMethod
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(this, new object[] { modelBuilder });
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenantStamping();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenantStamping();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTenantStamping()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = _currentTenantService.TenantId
                    ?? throw new TenantContextMissingException();
            }
            else if (entry.State == EntityState.Modified)
            {
                var ambientTenantId = _currentTenantService.TenantId;
                if (ambientTenantId is not null && entry.Entity.TenantId != ambientTenantId)
                {
                    throw new InvalidOperationException("Cannot modify an entity belonging to a different tenant.");
                }
            }
        }
    }
}
