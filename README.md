# OlympusCore Multitenant

A production-oriented, multi-tenant Clean Architecture Web API built on .NET 8 — JWT auth, row-level tenant isolation, permission-based (RBAC) authorization, dynamic menus, real-time notifications, and distributed caching.

## Architecture

```
src/
  OlympusCoreMultitenant.Api             HTTP endpoints, middleware, Swagger, SignalR, auth wiring, startup bootstrap
  OlympusCoreMultitenant.Application     Use-case services, DTOs, FluentValidation validators
  OlympusCoreMultitenant.Infrastructure  JWT, password hashing, email, other external integrations
  OlympusCoreMultitenant.Persistence     EF Core DbContext, repositories, seeding, migrations
  OlympusCoreMultitenant.Domain          Entities, enums, common models, value objects
```

Layered dependency direction: `Api → Application → Domain`, with `Infrastructure` and `Persistence` implementing interfaces defined in `Application`/`Domain`.

## Key Features

- **Multi-tenancy** — row-level tenant isolation enforced by EF Core global query filters, not per-tenant schemas/databases.
- **JWT authentication** with access + refresh token rotation.
- **Permission-based authorization** (RBAC) — a global permission catalog with per-tenant role → permission assignment, plus self-or-permission policies (e.g. a user can always read their own profile).
- **Dynamic, permission-filtered navigation menus**, seeded per tenant and served via API.
- **Real-time notifications** over SignalR, plus a REST notification API.
- **Distributed caching** — Redis or in-memory, tenant-scoped where relevant (roles, menus) and global where relevant (the permission catalog).
- **Site settings** (per-tenant, e.g. branding/SMTP) and **system settings** (global, e.g. auth policy toggles).
- **Startup self-checks** — database bootstrap, permission-catalog sync from code constants, and menu→permission integrity validation.

## Multi-Tenancy Model

**Tenant isolation** is enforced centrally in `ApplicationDbContext`:
- Any entity implementing `ITenantEntity` (currently `User`, `Role`, `RolePermission`, `UserRole`, `Menu`) automatically gets an EF Core global query filter scoping every query to the ambient tenant, and gets its `TenantId` auto-stamped on insert. No repository ever needs to add a manual `.Where(x => x.TenantId == ...)`.
- The ambient tenant is tracked via `ICurrentTenantService`, backed by `AsyncLocal`, set per-request by `TenantResolutionMiddleware` — from the `tenant_id` claim in the JWT for authenticated calls, or an `X-Tenant-Id` header for anonymous calls (e.g. login).
- `Permission` is **not** tenant-scoped — it's a single global catalog shared by all tenants (see below).

**Registering a tenant** is a two-step admin flow (`TenantsController`, requires `tenants.manage`):

```
POST /api/v1/tenants                    # creates the Tenant row only
POST /api/v1/tenants/{id}/provision      # seeds roles, permissions grants, a SuperAdmin user, site settings, and the menu tree for that tenant
```

Provisioning (`TenantProvisioningService`) opens a tenant-scoped context and idempotently runs:

```csharp
await _rbacSeeder.SeedAsync(ct);              // roles + permission catalog sync + default role grants
await DefaultUserSeeder.SeedAsync(...);        // a SuperAdmin user for this tenant
await DefaultSiteSettingsSeeder.SeedAsync(...);
await DefaultMenuSeeder.SeedAsync(...);        // this tenant's nav tree
```

The same pipeline runs automatically at app startup for a hardcoded `"default"` tenant, which is how a fresh database bootstraps itself into a usable state.

**Permission model — global catalog, per-tenant assignment:**

| Concept | Tenant-scoped? |
|---|---|
| `Permission` (e.g. `users.read`) | No — one shared catalog of permission names |
| `Role` (e.g. "Admin") | Yes — each tenant has its own physical role rows |
| `RolePermission` (role → permission grant) | Yes — independent per tenant |
| `UserRole` (user → role) | Yes |
| `Menu` (nav item) | Yes — per-tenant tree, each item optionally gated by a `RequiredPermission` string |

Permissions are defined once in `Application/Security/Permissions.cs` (`Permissions.All`). That list drives ASP.NET authorization policy registration, a startup DB sync (`PermissionSyncService`, adds any missing permission rows), and default grants during provisioning (`RbacSeeder`). Adding a new permission constant makes it available everywhere immediately; whether a given tenant's roles actually hold it is a separate, per-tenant `RolePermission` decision — made via the seeder for new tenants, or the Roles admin API (`PUT/POST /roles/{roleId}/permissions/...`) for existing ones, with no redeploy required.

## Getting Started

**Prerequisites**
- .NET SDK 8.0+
- PostgreSQL or SQL Server (local or remote)
- Optional: Redis (distributed cache), Seq (log aggregation)

```sh
dotnet --version
```

**Configure**

Edit `src/OlympusCoreMultitenant.Api/appsettings.json` (or an environment-specific override / environment variables):

- `Database:Provider` — `postgres` or `sqlserver`
- `ConnectionStrings:PostgresConnection` / `ConnectionStrings:SqlServerConnection`
- `ConnectionStrings:Redis` (only used if `Caching:UseRedis` is `true`)
- `Jwt:SecretKey` (32+ chars), `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes`, `Jwt:RefreshTokenExpiryDays`
- `Auth:EmailUniquenessScope` — `PerTenant` (default, requires a tenant slug at login) or `Global` (email alone identifies the user across tenants)
- `Smtp:*`
- `Serilog:*`

**Run**

```sh
dotnet restore
dotnet build
dotnet run --project src/OlympusCoreMultitenant.Api
```

On first run against a fresh database, the app applies EF Core migrations and provisions the `"default"` tenant automatically — including a default SuperAdmin user (see [Default User and Site Settings](#default-user-and-site-settings)). Swagger UI is available in development mode.

## Configuration Reference

| Key | Purpose |
|---|---|
| `Database:Provider` | `postgres` or `sqlserver` |
| `ConnectionStrings:PostgresConnection` / `SqlServerConnection` | DB connection strings |
| `ConnectionStrings:Redis` | Redis connection string |
| `Caching:UseRedis` | Redis vs in-memory cache |
| `Caching:RedisInstanceName` | Redis key prefix |
| `Caching:PermissionsTtlMinutes` / `MenusTtlMinutes` / `RolesTtlMinutes` / `RolePermissionsTtlMinutes` | Cache TTLs |
| `Auth:EmailUniquenessScope` | `PerTenant` or `Global` login resolution |
| `Jwt:Issuer` / `Jwt:Audience` / `Jwt:SecretKey` / `Jwt:ExpiryMinutes` / `Jwt:RefreshTokenExpiryDays` | Token settings |
| `Smtp:*` | Outbound email (password reset, notifications) |
| `Serilog:*` | Logging sinks, levels, optional Seq |

## Tenant Onboarding Example

```sh
curl -X POST https://localhost:5001/api/v1/tenants \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"slug": "acme", "name": "Acme Corp"}'

curl -X POST https://localhost:5001/api/v1/tenants/{id}/provision \
  -H "Authorization: Bearer $TOKEN"
```

Subsequent logins for that tenant pass `tenantSlug: "acme"` (when `Auth:EmailUniquenessScope` is `PerTenant`) or the `X-Tenant-Id: acme` header pre-auth.

## Adding a New Module (e.g. Employee Management)

1. Add permission constants to `Application/Security/Permissions.cs` and its `All` array — auto-registers authorization policies and gets synced into the DB on next boot.
2. Model new entities as `: BaseEntity, ITenantEntity` to get automatic tenant filtering/stamping for free.
3. Build the service/controller following the `Roles`/`Users` module pattern, decorating actions with `[Authorize(Policy = Permissions.X)]`.
4. Grant the new permissions to default roles via `RbacSeeder` (for new tenants) or the Roles admin API (for existing tenants).
5. Optionally add a `Menu` entry via `DefaultMenuSeeder` or the Menu API, gated by the new `RequiredPermission`.

See `src/Docs/RBAC_PBAC_MENU_GUIDE.md` and `src/Docs/ASSIGN_ROLE_PERMISSION.md` for more detail.

## Notifications

Real-time delivery via SignalR plus a REST API:

- SignalR hub: `/hubs/notifications` — connect with the user's JWT as `accessTokenFactory`, listen for `ReceiveNotification`.
- `GET /api/v1/notifications` — list for the current user
- `POST /api/v1/notifications` — create (admin/system)
- `POST /api/v1/notifications/{id}/read` — mark as read
- `DELETE /api/v1/notifications/{id}` — delete

See `NOTIFICATIONS_GUIDE.md` for a full client example.

## Default User and Site Settings

Each newly provisioned tenant seeds:

- A **SuperAdmin** user — `superadmin@localhost` / `SuperAdmin@123!` (change immediately in any non-local environment).
- Default **site settings** (title, logo, sidebar/color-scheme prefs, SMTP placeholders) — per tenant, editable via `GET/POST/DELETE /api/v1/sitesettings`.
- A default **menu tree** (Dashboard, Configuration > Users/Roles/Permissions/Menus/Site Settings/System Settings, plus platform-only items like Tenants which are visible to SuperAdmin only).

Global (not per-tenant) **system settings** — e.g. `Auth:EmailUniquenessScope` override — are managed separately and require `system-settings.manage`.

## Database & Seeding

On startup the API: applies pending EF Core migrations, seeds global system settings, ensures the `"default"` tenant exists, and runs full provisioning for it. All seeders are idempotent (check-then-insert), so re-running them — including re-provisioning an existing tenant — is safe and additive-only (it will never remove a grant you've since customized).

```sh
dotnet ef migrations add <Name> --project src/OlympusCoreMultitenant.Persistence --startup-project src/OlympusCoreMultitenant.Api
dotnet ef database update --project src/OlympusCoreMultitenant.Persistence --startup-project src/OlympusCoreMultitenant.Api
```

## Further Reading

`src/Docs/` contains focused guides: `AUTHORIZATION.md`, `RBAC_PBAC_MENU_GUIDE.md`, `ASSIGN_ROLE_PERMISSION.md`, `ADD_MENU.md`, `CACHING.md` / `CACHING_GUIDE.md`, `REDIS_DEPLOYMENT.md`, `PALETTE_API.md` / `PALETTE_SEED.md`. Root-level `NOTIFICATIONS_GUIDE.md` and `API_TESTING.md` cover notifications and manual API testing respectively.

## Troubleshooting

**JWT auth fails**
1. Verify `Jwt:SecretKey` is set and 32+ characters.
2. Verify `Issuer`/`Audience` match the token validation configuration.
3. Ensure the system clock is accurate (token expiry/`nbf` checks).

**PostgreSQL connection fails**
1. Validate host, port, database, username, and password.
2. Confirm the PostgreSQL service is running and reachable (firewall/network rules).

**A tenant can't see an expected menu item or permission**
- Confirm the tenant has actually been provisioned (`POST /tenants/{id}/provision`), not just created.
- Remember permission grants only take effect on the user's *next* token issuance — have them log in again after a role's permissions change.
- For an existing tenant, newly added default seed data (new menu items, new default role grants) only appears after re-running provisioning for that tenant.

---

## Roadmap: What's Missing for a Complete Multi-Tenant Framework

The current implementation covers the RBAC/tenant-isolation core well. Gaps identified while reviewing the system, roughly in priority order:

1. **No FE-driven tenant provisioning trigger.** The companion frontend can create a tenant but never calls `POST /tenants/{id}/provision` — an operator must trigger it manually today (Swagger/curl). Needs a "Provision" action in the Tenants admin UI, or auto-provisioning on create.
2. **No tenant deletion or data export.** `DELETE /tenants/{id}` only flips `IsActive` to false (soft-disable) — there's no hard-delete, data purge, or tenant data export path for offboarding/GDPR-style requests.
3. **No self-serve tenant signup.** Tenant creation is an authenticated admin-only operation; there's no public registration flow for a new organization to sign itself up.
4. **No re-provisioning automation.** When `RbacSeeder`/`DefaultMenuSeeder` gain new defaults, existing tenants don't pick them up until someone manually re-runs provisioning per tenant — there's no bulk "re-provision all tenants" job or migration-time backfill.
5. **No tenant-level resource quotas or rate limiting.** Nothing throttles or caps usage (API calls, storage, seats) per tenant — needed for a SaaS-grade offering.
6. **No tenant-level feature flags.** System settings are global-only; there's no mechanism to enable/disable features per tenant (e.g. gradual rollout, plan tiers).
7. **No FE route-level permission enforcement.** The frontend has a `PermissionGuard` component but doesn't apply it to any route — any authenticated user can navigate to admin pages regardless of their actual permissions (server-side authorization still blocks the underlying API calls, but the UI itself isn't gated).
8. **No automated test suite.** There is no test project in the solution — multi-tenant isolation (the query-filter/stamping logic in `ApplicationDbContext`) is exactly the kind of invariant that should have regression coverage.
9. **Single shared database only.** Isolation is row-level (`TenantId` + query filters) with no option for schema-per-tenant or database-per-tenant, which some compliance-sensitive customers require.
10. **No tenant-aware audit logging.** There's no structured audit trail of cross-cutting tenant-admin actions (tenant created/provisioned/disabled, role permission changes) for compliance or support.
11. **No custom domain / subdomain-based tenant resolution.** Tenant resolution is JWT-claim or header based only — no support for resolving tenant from a subdomain or custom domain, which is standard for SaaS products.

Items 1, 4, 7, and 8 are the most impactful near-term (they affect correctness/usability of what already exists); the rest are larger platform investments to consider based on product direction.
