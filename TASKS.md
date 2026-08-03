# Olympus — Current Tasks

Consolidated backlog for `OlympusCoreMultitenant`, grouped by area. Compiled from a requirements gap-check against the live codebase and the platform architecture review (`docs/` or private artifact — see architect review for full rationale/diagrams behind the "Platform Foundation" and "Module Framework" groups).

FE items live in the companion repo `olympus-react-multitenant`, not here — noted below where relevant.

---

## Tenant Lifecycle

- [x] Self-serve tenant signup — registration link + anonymous submission + admin approval (`TenantRegistrationController`, `TenantsController.ApproveRegistration`)
- [ ] FE-driven / automatic provisioning trigger — `ApproveRegistration` auto-provisions, but the plain admin "Create tenant" path still requires a separate manual `POST /tenants/{id}/provision` call; unclear whether the FE ever calls it (verify in `olympus-react-multitenant`)
- [x] Tenant hard-delete / data export — `GET /tenants/{id}/export` (users/roles/permissions/settings/flags/domains as JSON) and `DELETE /tenants/{id}/purge?confirmSlug=...`; purge requires the tenant already disabled, past a configurable cooling-off period (`TenantOffboarding:PurgeCoolingOffDays`, default 7 days), and an exact slug confirmation. Audit logs are deliberately retained, not purged
- [x] Bulk re-provisioning automation — `ITenantProvisioningService.ResyncCatalogAsync` re-runs the idempotent `RbacSeeder`/`DefaultSiteSettingsSeeder`/`DefaultMenuSeeder` per tenant (entitlements untouched); exposed via `POST /tenants/{id}/resync-catalog` and `POST /tenants/resync-catalog-all`

## Multi-Tenancy & Isolation

- [x] Tenant-level feature flags — new `TenantFeatureFlag` entity + `IFeatureFlagService` (cached, deterministic rollout-percentage bucketing) + `FeatureFlagsController` (`GET/POST /feature-flags`, `GET /feature-flags/{key}/enabled`), gated by new `feature-flags.read`/`feature-flags.manage` permissions
- [x] Subdomain / custom-domain tenant resolution — new `TenantDomain` entity (`GET/POST /tenants/{id}/domains`, `DELETE /tenants/{id}/domains/{domainId}`) plus `Tenancy:BaseDomain`-driven subdomain matching in `TenantResolutionMiddleware` for anonymous requests only (authenticated requests stay pinned to their JWT claim)
- [ ] Optional schema-per-tenant / database-per-tenant isolation tier — **reassessed, not attempted this pass.** Investigated during this session's implementation round: a real version needs EF's per-schema `IModelCacheKeyFactory` rework plus threading a schema name through `ICurrentTenantService`/`BeginScope` across every one of its ~10 call sites (middleware, provisioning, offboarding, resync, subscription-plan assignment), none of which can be verified end-to-end without a live Postgres instance (not available in this environment). Shipping just the "mark tenant + create an empty schema" half without runtime query routing would be inert scaffolding, not a real isolation tier. Treat this as a third large initiative, scoped and designed on its own before implementation — same footing as Module Framework and the Template Ecosystem below.

## Quotas & Licensing

- [x] Seat quota enforcement — `SubscriptionPlan.MaxUsers` checked at registration (`AuthService.cs`)
- [x] API rate limiting per tenant — global `PartitionedRateLimiter` keyed by JWT `tenant_id` claim (falls back to remote IP for anonymous requests), fixed window, configurable via `RateLimiting:*` in `appsettings.json`
- [ ] Storage/usage quotas per tenant — blocked: no file/blob storage subsystem exists in this codebase to meter (only a `ProfileImageUrl` string field), so there's nothing real to enforce a quota against yet. Revisit once a storage feature exists.
- [x] `ILicenseService` — distinct from module *entitlement*: `Tenant` now carries raw facts (`TrialEndsAtUtc`, `LicenseExpiresAtUtc`, `IsSuspended`); `LicenseService.ComputeStatus` derives `Active/Trial/GracePeriod/Expired/Suspended` (grace window via `Licensing:GracePeriodDays`) rather than storing status redundantly. Enforced in `TenantResolutionMiddleware` for ordinary tenant members only (never the platform-admin override branch, so admins can still reach a suspended tenant to fix it) — `402 Payment Required` when not allowed. Managed via `GET/POST /tenants/{id}/license*`

## Security & Compliance

- [x] Tenant-aware audit logging — generic `SaveChanges` interceptor in `ApplicationDbContext`, covers every entity Create/Update/Delete, tenant-scoped, actor-attributed
- [x] Explicit audit entry for the platform-admin `X-Tenant-Id` cross-tenant override — `TenantResolutionMiddleware.RecordOverrideAccessAsync` now writes an `AuditAction.Accessed` row (home tenant, method, path) on every overridden request
- [x] FE route-level permission enforcement (**olympus-react-multitenant**) — `PermissionGuard` now wraps every permission-gated route in `router/index.tsx`, matching each backend controller's policy exactly. Fixed a bug in the guard itself first: it didn't account for the `platform_admin` JWT-claim bypass `PermissionAuthorizationHandler` uses server-side, which would have locked platform admins out of pages the API still lets them call (tenant roles never get Platform-module permissions, so a platform admin's `permissions` claim array alone isn't enough)
- [x] Self-service license visibility for tenant admins — new backend endpoint `GET /tenant/license` (`TenantSelfController`, `[Authorize]` only, no `tenants.manage` needed, resolves the caller's own ambient tenant). Needed a `[SkipLicenseCheck]` exemption in `TenantResolutionMiddleware` too: without it, the one endpoint that explains "why am I locked out" would itself be blocked by the same 402 it exists to explain. FE: `tenantSelfApi` + a dismissible `LicenseStatusBanner` in `DashboardLayout`, shown tenant-wide for Trial/GracePeriod/Expired/Suspended (silent when Active)

## Platform Foundation

- [x] Automated test suite — new `OlympusCoreMultitenant.UnitTests` project (xUnit + EF Core InMemory), 18 tests: tenant-isolation query-filter behavior, `BeginScope` nesting/restore, the audit interceptor (capture + exclusions), `Tenant` domain behavior, `FeatureFlagService` rollout logic
- [x] Architecture tests (NetArchTest) enforcing dependency direction — `Architecture/LayeringTests.cs` asserts Domain/Application/Persistence layering, including a regression guard for the exact Persistence→Infrastructure crack fixed above
- [x] Fix `Persistence → Infrastructure` design-time reference — `ApplicationDbContextFactory` now uses local no-op `ICurrentTenantService`/`ICurrentUserService` stubs; the `ProjectReference` to `Infrastructure` is removed from `Persistence.csproj`
- [x] Distributed boot lock for multi-instance deploys — `IDistributedBootLock`/`PostgresAdvisoryBootLock` wraps the migration/catalog-sync sequence in a Postgres session-level advisory lock (`pg_advisory_lock`), so a second replica waits instead of racing migrations

## Module Framework (larger initiative)

- [x] `IOlympusModule` contract + `ModuleHost` discovery — `CoreModule`/`PlatformModule` (`Application/ModuleSystem/`) declare their own Key/Name/Description/Kind/Permissions/Menus; `ModuleHost.DiscoverModules` assembly-scans, validates dependencies (throws on missing, detects cycles), and topologically orders them. `ModuleSyncService`, `PermissionSyncService`, `RbacSeeder`, `DefaultMenuSeeder`, and the `AddAuthorization` policy loop in `Program.cs` all consume the discovered list now instead of the old static `Modules.All`/`Permissions.All`/`Modules.PermissionModuleMap`. Metadata-only — **no files were physically moved**; see note below for why
- [x] Integration Hub — `IEventBus`/`IIntegrationEvent`/`IIntegrationEventHandler<T>` (`Application/Integration/`) + `InProcessEventBus` (DI-resolved handlers, in-process, synchronous so it still participates in the caller's transaction). Built the piece that was actually justified, not the full trio speculatively: retrofitted the one real coupling found below — `TenantService.ApproveRegistrationAsync` now publishes `TenantApprovedIntegrationEvent` instead of directly constructing `User`/`Role`/`UserRole`; `CreateAdminUserOnTenantApprovedHandler` (in `Users/EventHandlers/`) reacts to it. `TenantService` no longer references `IUserRepository`/`IRoleRepository`/`User`/`Role`/`SystemRoles` at all. `ICommandBus`/`IQueryBus` intentionally not built — nothing needs them yet; add each only when a real caller does
- [ ] Physical module folder restructure (`Modules/Core/*`, `Modules/Platform/*` projects) — **attempted and reverted earlier this session**, for the coupling reason the Integration Hub above now resolves. Worth reattempting: the `TenantService`/`User`/`Role` circular dependency that blocked it is gone now that the tenant-approval flow goes through `IEventBus` instead of a direct reference. Still a large, ~160-file undertaking with no live Postgres in this environment to fully verify — revisit deliberately, not opportunistically.

## .NET Template Ecosystem (future initiative)

- [x] `dotnet new olympus-multi` solution template (`templates/OlympusMulti/`) — `-n` renames the whole solution (project names, namespaces, files) via `sourceName`; `--cache memory|redis` is the only parameter exposed, because it's the only one with two real, working implementations today. **Deliberately not parameterized**: `--database`/`--orm`/`--auth`/`--messagebroker`/`--jobs`/`--frontend`/`--api` — none of those have more than one real implementation in this codebase (`mssql`, Dapper, Auth0/Keycloak, any message broker, Hangfire/Quartz, and any frontend don't exist at all; only `postgres`+EF+JWT+controllers are real). Verified end-to-end: installed, generated with `--cache redis`, built, and its own 40-test suite passed under the renamed identity
- [x] `olympus-module` / `olympus-feature` item templates (`templates/OlympusModule/`, `templates/OlympusFeature/`) — `olympus-module -n Product` scaffolds an `IOlympusModule` implementation with zero manual wiring, **verified to actually get discovered by `ModuleHost`** (not just compile) via a throwaway test asserting the scaffolded module appears alongside Core/Platform. `olympus-feature --module Product -n CreateProduct` scaffolds a Request/Response/Command/Validator/Handler using the new `ICommandHandler<TCommand,TResult>` (DI-resolved, no bus — same "build only what's justified" call as the Integration Hub). `olympus-aggregate`/`olympus-event`/`olympus-integration` not built — thin wrappers around the same mechanics, add when a real need for the distinction shows up
- [ ] Extract `Olympus.Abstractions` / `Olympus.Sdk` as standalone NuGet packages — not attempted; requires the physical module split above first
- [ ] CI smoke-test matrix across template parameter combinations — only one real parameter (`--cache`) exists today, so there's just a 2-cell matrix; add real automation once a second real parameter exists

---

## Recently completed (context, not open work)

- Cross-tenant `platform_admin` flag replacing per-tenant `SuperAdmin` role, with header-based tenant-context switching for platform admins
- Tenant branding returned from login (title/logo/tagline)
- Tenant activation gated behind Assign Plan
