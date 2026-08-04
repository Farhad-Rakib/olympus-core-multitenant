# Building an ERP / Multi-Tenant Product on Olympus

A practical guide for using the `olympus-multi` template to start a new multi-tenant SaaS product —
ERP, CRM, LMS, or anything else with the same shape: many tenants, each with their own users,
roles, licensed modules, and data.

This guide is deliberately honest about scope. Olympus gives you the multi-tenancy, auth, and
module-registration plumbing — the undifferentiated, easy-to-get-wrong 20% every product like this
needs. It does not give you Product/Inventory/Accounting/Order modules, background jobs, file
storage, or search. Section 8 covers exactly what's real today and what you'll need to add
yourself, and in what order.

---

## 1. What you actually get

| Capability | What it is |
|---|---|
| Multi-tenancy | Row-level isolation: every tenant-owned entity implements `ITenantEntity`, a global EF Core query filter (`TenantId == CurrentTenantId`) enforces it on every query, and writes are auto-stamped. Tenant is resolved from the JWT `tenant_id` claim, an `X-Tenant-Id` header (anonymous requests, or a platform-admin override), or an optional subdomain/custom-domain (opt-in via `Tenancy:BaseDomain` config) |
| Auth | JWT bearer + refresh tokens, `PerTenant` or `Global` email-uniqueness scope |
| RBAC/PBAC | Roles are named bundles of permissions; every endpoint checks a permission (`[Authorize(Policy = "products.read")]`), never a role name |
| Module entitlement | `TenantModule` — which modules a tenant is actually licensed for. `SubscriptionPlan` bundles a set of modules + a seat limit |
| Feature flags | Per-tenant, with deterministic rollout-percentage bucketing (`/feature-flags`) |
| Licensing | Trial / grace-period / expiry / suspension per tenant, enforced as `402 Payment Required` on every request once a tenant's license lapses (`/tenant/license` self-service status) |
| Module framework | `IOlympusModule` + `ModuleHost` — implement one interface, get discovered and wired automatically (permissions, menus, catalog rows) — no manual registration anywhere |
| Event bus | `IEventBus`/`IIntegrationEvent`/`IIntegrationEventHandler<T>` — in-process by default, or RabbitMQ-backed via `--messagebroker rabbitmq` (durable exchange, quorum queues, dead-lettering). Exists so one module can react to another's fact without a hard reference |
| Audit trail | Every entity Create/Update/Delete captured automatically via a `SaveChanges` interceptor — tenant-scoped, actor-attributed, no per-feature wiring needed |
| Rate limiting | Per-tenant, partitioned by JWT claim |
| Scaffolding | None — add a business module or feature by hand, copying `Core`/`Platform` (see §5/§6). Kept deliberately to one `dotnet new` command instead of three |

Everything above is real, working code, verified end-to-end (build, test, and — for the module
template — actual runtime discovery) as of this template's current state.

---

## 2. Prerequisites

- .NET 8 SDK
- PostgreSQL, or SQL Server if you generate with `--database sqlserver`
- Redis, only if you want `--cache redis` instead of the in-memory default

---

## 3. Create your solution

```bash
dotnet new install .   # from the repo root
dotnet new olympus-multi -n MyErpApp --database postgres --cache redis --messagebroker inprocess -o ./MyErpApp
```

> The live repo is itself the template (`sourceName: "ProjectNamePlaceholder"` in the root
> `.template.config/template.json`) — there's no separate copy to keep in sync. One consequence:
> a few `src/ProjectNamePlaceholder.Api/appsettings.json` values are literal, unresolved template
> tokens in the raw repo (`UseRedis` is `USE_REDIS_TOKEN`, `Integration:UseRabbitMq` is
> `USE_RABBITMQ_TOKEN`, `Database:Provider` is `DB_PROVIDER_TOKEN`) — that's how template generation
> substitutes real values into valid JSON. Set them to real values yourself if you ever want to
> `dotnet run` this repo directly instead of generating from it. `Database:Provider` falls back to
> `postgres` automatically if you don't (so that one only matters if you want to exercise the SQL
> Server path from the raw repo); `UseRedis`/`UseRabbitMq` being unresolved leaves the raw
> `appsettings.json` invalid JSON until fixed.

`-n` renames everything — project files, namespaces, assembly names, the solution file — from
`ProjectNamePlaceholder` to `MyErpApp`. `--database` is `postgres` (default) or `sqlserver` —
picking one excludes the other provider's packages, DI wiring, boot-lock implementation, and EF
Core migrations from the generated solution entirely. `--cache` is `memory` (default) or `redis`.
These are the only solution-level parameters exposed, because they're the only ones with two real,
working implementations behind them.

```bash
cd MyErpApp/src
dotnet build src.sln          # confirm it compiles as-is
```

Then, before first run:

1. Set `ConnectionStrings:PostgresConnection` (or `ConnectionStrings:SqlServerConnection`, if you
   generated with `--database sqlserver`) in `appsettings.json` (or environment-specific overrides)
   to a real database instance.
2. Set `Jwt:SecretKey` to a real secret (32+ characters) — never ship the placeholder.
3. Optionally set `Bootstrap:PlatformAdminEmail`/`Bootstrap:PlatformAdminPassword` — leave the
   password blank to have one generated and logged on first boot.

```bash
dotnet run --project MyErpApp.Api
```

On first boot the app migrates the database, syncs the module/permission catalog, and seeds a
bootstrap platform-admin account and a default tenant. Log in with that account, and you have a
working, empty multi-tenant admin panel.

---

## 4. The solution you just generated

```
MyErpApp/src/
├── MyErpApp.Domain/            Entities, enums, value objects — no framework dependencies
├── MyErpApp.Application/       Services, DTOs, validators, and:
│   └── ModuleSystem/           IOlympusModule, ModuleHost, CoreModule, PlatformModule
├── MyErpApp.Infrastructure/    JWT, password hashing, email, multi-tenancy plumbing
├── MyErpApp.Persistence/       DbContext, EF configurations, repositories, migrations, seeding
├── MyErpApp.Api/                Controllers, middleware, Program.cs composition root
└── MyErpApp.UnitTests/          xUnit + EF InMemory + NetArchTest layering checks
```

`Core` and `Platform` (in `Application/ModuleSystem/`) are the two modules that ship with the
template — user/role/permission/menu management, and platform administration (tenants,
subscriptions, system settings) respectively. They're the reference implementation for every
module you add next.

---

## 5. Add your first business module

Business logic goes in a module. There's no generator for this — create the file yourself,
following `Core`/`Platform` (`Application/ModuleSystem/CoreModule.cs`, `PlatformModule.cs`) as the
pattern. Create `MyErpApp.Application/Modules/Product/ProductModule.cs`:

```csharp
namespace MyErpApp.BusinessModules.Product;

public sealed class ProductModule : IOlympusModule
{
    public string Key => "product";
    public ModuleKind Kind => ModuleKind.Business;
    public IReadOnlyList<string> Dependencies => ["core"];
    public IReadOnlyList<string> OptionalDependencies => [];

    public IReadOnlyList<PermissionDefinition> Permissions =>
    [
        new("product.read", "View Product records"),
        new("product.create", "Create Product records"),
        // ...
    ];

    public IReadOnlyList<MenuDefinition> Menus =>
    [
        new("Product", "/product", "package", "product.read", "Configuration"),
    ];
}
```

That's it — **this is the entire registration step.** Restart the app and `ModuleHost` picks it up:
the permissions get synced into the database, the menu item appears (gated by the permission you
declared), and `TenantModule` entitlement/licensing checks work against it automatically. Nothing
else needs editing — not `Program.cs`, not a permissions file, not a menu seeder.

> **Note the namespace: `BusinessModules`, not `Modules`.** The framework's own
> `Application.Security.Modules` static class (module *key* constants like `Modules.Core`) collides
> with any namespace literally named `Modules`, via C#'s enclosing-namespace lookup rules. This bit
> us once already — the template is already fixed, just don't rename it back if you're refactoring.

### Add real behavior

Flesh out the module the same way `Core`/`Platform` are built:
1. Domain entities in `MyErpApp.Domain/Entities/` — implement `ITenantEntity` if the data is
   per-tenant (almost everything in a business module is).
2. A repository interface in `Application/Common/Interfaces/`, an EF configuration + repository
   implementation in `Persistence/`.
3. A service (`IProductService`/`ProductService`), registered in `Application/DependencyInjection.cs`.
4. A controller in `Api/Controllers/`, gated by the permissions your module declared
   (`[Authorize(Policy = "product.read")]`).

Update your module's `Dependencies` list if it genuinely needs another module to exist first
(`ModuleHost` throws at boot if a declared dependency isn't discovered — fail fast, not a silent
runtime surprise).

---

## 6. Add a feature (vertical slice) inside a module

Same deal — no generator, create the files under
`MyErpApp.Application/Modules/Product/Features/CreateProduct/` yourself:

```
CreateProductRequest.cs      # what the client sends
CreateProductResponse.cs     # what you send back
CreateProductCommand.cs      # the internal instruction
CreateProductRequestValidator.cs   # FluentValidation
CreateProductHandler.cs      # ICommandHandler<CreateProductCommand, CreateProductResponse>
```

Then two wiring steps:

```csharp
// Application/DependencyInjection.cs
services.AddScoped<ICommandHandler<CreateProductCommand, CreateProductResponse>, CreateProductHandler>();
```

```csharp
// Api/Controllers/ProductController.cs
[HttpPost]
[Authorize(Policy = "product.create")]
public async Task<IActionResult> CreateProduct(
    [FromBody] CreateProductRequest request,
    [FromServices] IValidator<CreateProductRequest> validator,
    [FromServices] ICommandHandler<CreateProductCommand, CreateProductResponse> handler,
    CancellationToken cancellationToken)
{
    await validator.ValidateAndThrowAsync(request, cancellationToken);
    var result = await handler.HandleAsync(new CreateProductCommand(request.Name), cancellationToken);
    return Ok(ApiResponse<CreateProductResponse>.SuccessResponse(result, "Created successfully"));
}
```

**Why `ICommandHandler<TCommand, TResult>` and not a command bus:** a bus buys you dispatch without
knowing the concrete handler type — valuable once a module needs to fire a command into another
module it doesn't reference. Nothing in a single module needs that; direct DI resolution is simpler
and just as testable. Build a real bus only once you have a concrete case for it (see §8).

---

## 7. Multi-tenancy, in practice

**Every query is automatically scoped.** If your controller action runs inside an authenticated
request, `dbContext.Set<YourEntity>()` already only returns the caller's tenant's rows — the global
query filter handles it. You never write `.Where(x => x.TenantId == currentTenantId)` yourself.

**Writes are automatically stamped.** `TenantId` on a newly-added `ITenantEntity` is set from the
ambient tenant context at `SaveChanges` time, not when you call `.Add()`.

**Cross-tenant operations** (provisioning a tenant, resyncing its catalog, a platform admin
managing another tenant) explicitly open a scope:

```csharp
using var scope = _currentTenantService.BeginScope(tenant.Id, tenant.Slug);
// queries and writes in here are scoped to `tenant`, regardless of the caller's own tenant
```

**Gating a feature per tenant** — three independent layers, know which one you actually want:

| Layer | Question it answers | How to check it |
|---|---|---|
| Module entitlement | Did this tenant buy the module at all? | Automatic — enforced via the permission policy your module declared |
| Feature flag | Is this specific feature on for this tenant (maybe a gradual rollout)? | `IFeatureFlagService.IsEnabledAsync("your-flag-key")` |
| License status | Is the tenant's subscription currently valid? | Automatic — a `402` before your code even runs, for ordinary tenant members |

---

## 8. What's missing, and what order to build it in

Building an actual ERP-scale product (many interdependent modules: Order touching Inventory,
Accounting, Courier, and Payment in one transaction) needs more than what ships here. Add
infrastructure **when a real module's real requirements demand it**, not speculatively:

1. **One real module first.** Prove the pattern end-to-end (Product is a reasonable first choice —
   almost everything else references it) before building a second. Let its actual pain points tell
   you what's next, rather than guessing upfront.
2. **A real Integration Hub, once two modules need to talk.** `--messagebroker rabbitmq` gets you
   `RabbitMqEventBus`/`RabbitMqEventConsumer` (`Infrastructure/Messaging/`) — a durable topic
   exchange, one quorum queue per discovered `IIntegrationEventHandler<T>`, at-least-once with
   automatic dead-lettering after a few failed attempts (RabbitMQ's own `x-delivery-limit`, not a
   hand-rolled retry counter). The default is still `--messagebroker inprocess`
   (`InProcessEventBus`) — same-transaction, no cross-process delivery guarantees, fine until a
   second module needs to react reliably. Neither is a transactional outbox: a publish that races a
   crash between your DB commit and the broker ack can still be lost either way. Add
   `ICommandBus`/`IQueryBus` the same way: when a concrete cross-module call needs one, not before.
3. **Background jobs (Hangfire/Quartz)**, once you have anything that isn't pure request/response —
   courier polling, scheduled campaigns, recurring billing, async AI/content workflows. None of this
   exists in the template; it's a from-scratch addition.
4. **Physical module boundaries**, once you have enough real modules that "no module reaches into
   another's internals" needs compiler enforcement, not just discipline. Two modules aren't enough
   to justify it — we tried splitting `Core`/`Platform` into separate projects with only two modules
   and hit a real circular dependency (`AuthService` needs `Tenant`; tenant-approval needs to create
   a `User`) that only the Integration Hub in step 2 resolves cleanly. Don't attempt this before you
   have that, or before you have a second real module to actually inform where the boundary goes.
5. **Whatever your domain needs that genuinely doesn't exist yet**: file/blob storage, a search
   engine, a payment gateway abstraction, an accounting engine, an AI provider abstraction. Build
   each as its own module using the same `IOlympusModule` pattern — the framework doesn't care what
   your module does, only that it declares itself.

---

## 9. Reference

```bash
# Install (once per machine, from the repo root)
dotnet new install .

# Solution
dotnet new olympus-multi -n <Name> [--database postgres|sqlserver] [--cache memory|redis] [--messagebroker inprocess|rabbitmq] -o <path>

# Uninstall
dotnet new uninstall .
```

See `templates/README.md` for the short version of the same commands.
