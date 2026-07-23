using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OlympusCoreMultitenant.Application.Common.Exceptions;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Common;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Domain.Enums;
using Module = OlympusCoreMultitenant.Domain.Entities.Module;

namespace OlympusCoreMultitenant.Persistence.Context;

public sealed class ApplicationDbContext : DbContext
{
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ICurrentUserService _currentUserService;

    // Never written into an audit row's Changes payload, regardless of entity type.
    private static readonly HashSet<string> AuditSensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash", "TokenHash", "ReplacedByTokenHash"
    };

    // High-volume/low-audit-value entities excluded from capture entirely.
    private static readonly HashSet<Type> AuditExcludedTypes = new()
    {
        typeof(RefreshToken), typeof(PasswordResetToken), typeof(AuditLog)
    };

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantService currentTenantService, ICurrentUserService currentUserService)
        : base(options)
    {
        _currentTenantService = currentTenantService;
        _currentUserService = currentUserService;
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
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<SubscriptionPlanModule> SubscriptionPlanModules => Set<SubscriptionPlanModule>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

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
        var pending = CaptureAuditEntries();
        var result = base.SaveChanges(acceptAllChangesOnSuccess);

        if (pending.Count > 0)
        {
            FinalizeAuditEntries(pending);
            base.SaveChanges(true);
        }

        return result;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenantStamping();
        var pending = CaptureAuditEntries();
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        if (pending.Count > 0)
        {
            FinalizeAuditEntries(pending);
            await base.SaveChangesAsync(true, cancellationToken);
        }

        return result;
    }

    // Walks every tracked entity (not just ITenantEntity) so platform-global entities (Tenant,
    // Module, SubscriptionPlan) are covered too. Called BEFORE the real save so Modified/Deleted
    // OriginalValues are still intact; Added entities don't have their generated Id yet, so
    // EntityId is backfilled in FinalizeAuditEntries after the real save completes.
    private List<(AuditLog Audit, EntityEntry Entry)> CaptureAuditEntries()
    {
        var pending = new List<(AuditLog, EntityEntry)>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (AuditExcludedTypes.Contains(entry.Entity.GetType()))
            {
                continue;
            }

            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Modified => AuditAction.Updated,
                EntityState.Deleted => AuditAction.Deleted,
                _ => (AuditAction?)null
            };

            if (action is null)
            {
                continue;
            }

            var changes = new Dictionary<string, object?>();
            foreach (var property in entry.Properties)
            {
                var name = property.Metadata.Name;
                if (AuditSensitiveProperties.Contains(name))
                {
                    continue;
                }

                // Redundant with EntityId, and for Added entries this is still EF's temporary
                // in-memory placeholder key (real value isn't generated until after the real save).
                if (name == "Id")
                {
                    continue;
                }

                if (entry.State == EntityState.Modified && Equals(property.OriginalValue, property.CurrentValue))
                {
                    continue;
                }

                changes[name] = entry.State == EntityState.Deleted ? property.OriginalValue : property.CurrentValue;
            }

            var audit = new AuditLog
            {
                EntityName = entry.Entity.GetType().Name,
                EntityId = entry.State == EntityState.Added ? null : ReadIdIfPresent(entry),
                Action = action.Value,
                TenantId = (entry.Entity as ITenantEntity)?.TenantId,
                UserId = _currentUserService.UserId,
                Changes = JsonSerializer.Serialize(changes),
                CreatedAt = DateTime.UtcNow
            };

            pending.Add((audit, entry));
        }

        return pending;
    }

    // Calls base.SaveChanges(Async) directly (never this.SaveChanges/this.SaveChangesAsync), so this
    // second save does not recurse back into CaptureAuditEntries.
    private void FinalizeAuditEntries(List<(AuditLog Audit, EntityEntry Entry)> pending)
    {
        foreach (var (audit, entry) in pending)
        {
            audit.EntityId ??= ReadIdIfPresent(entry);
        }

        AuditLogs.AddRange(pending.Select(p => p.Audit));
    }

    private static long? ReadIdIfPresent(EntityEntry entry)
    {
        var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
        return idProperty?.CurrentValue is long id ? id : null;
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
