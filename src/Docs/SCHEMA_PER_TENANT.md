# Schema-per-tenant isolation tier — design

Status: **designed, not implemented.** It needs a live PostgreSQL/SQL Server to verify migration
routing and `search_path` behaviour end to end; that was not available when this was written, and
shipping the entity/config half without the runtime half would be inert scaffolding.

## Goal

Offer a per-tenant `IsolationMode`:

| Mode | Where a tenant's rows live | For |
|---|---|---|
| `Shared` (default, today) | shared tables, `TenantId` column + global filter | everyone |
| `Schema` | a dedicated schema `t_<slug>` with the same tables | compliance / noisy-neighbour isolation |
| `Database` | a dedicated database (connection string per tenant) | later; same routing seam |

Platform-global tables (`tenants`, `modules`, `subscription_plans`, `audit_logs`,
`processed_billing_events`, `translations`? — no, translations are tenant-owned) stay in the
default schema regardless of mode.

## Building blocks already in place

- `ModuleAwareModelCacheKeyFactory` — extend the key with the tenant schema. EF then builds and
  caches one model per (module set, schema), which is the standard EF pattern for this.
- `ICurrentTenantService.BeginScope` — 15 call sites (middleware, provisioning, offboarding,
  resync, plan assignment, seeding). The schema name is derived from the tenant, so these do not
  change signature; the DbContext reads `_currentTenantService.TenantSchema`.
- `ITenantEntity` + reflection-applied filter — under `Schema` mode the filter stays (belt and
  braces; the cost is one indexed predicate).

## Work items

1. `Tenant.IsolationMode` (enum, default `Shared`) + `SchemaName` (`t_<slug>`, immutable).
   Migration for both providers via the script.
2. `ICurrentTenantService.TenantSchema` (string?) resolved alongside `TenantId`.
3. `ApplicationDbContext.OnModelCreating`: when `TenantSchema` is set, call
   `modelBuilder.HasDefaultSchema(schema)` and then pin every platform-global entity back to the
   default schema (`ToTable(name, schema: null)` — there are ~6). Module-owned entities keep their
   module schema **prefixed**: `t_acme_billing`? No — simpler: `t_acme` holds all tenant-owned
   tables including module ones; module schemas only apply in `Shared` mode. Decide and test.
4. Model cache key: `(contextType, designTime, moduleSignature, tenantSchema)`.
5. Migrations per schema: EF's `__EFMigrationsHistory` must be per schema
   (`MigrationsHistoryTable("__EFMigrationsHistory", schema)`), so `EfCoreDatabaseBootstrapper`
   runs `MigrateAsync()` once for the default schema and once per `Schema`-mode tenant. This is
   the step that must be verified against a real database: EF migrations are generated for the
   default schema and applied with `HasDefaultSchema` swapped — works on Postgres via
   `search_path`; on SQL Server the generated SQL hard-codes `[dbo]` unless the migration uses
   `schema: null` everywhere (it currently does).
6. Provisioning: `TenantProvisioningService` creates the schema (`CREATE SCHEMA IF NOT EXISTS`)
   and runs the tenant migration before seeding. Offboarding `Purge` drops the schema.
7. Connection: Postgres set `search_path` per request via `NpgsqlConnection` interceptor or
   `HasDefaultSchema` (preferred — no session state). SQL Server: `HasDefaultSchema` only.
8. Repositories: no change if step 3 is right. Rule 1 in HOW-TO-USE §5 still applies.
9. Tests: an integration test project with Testcontainers (Postgres + SQL Server) covering
   provision → write in tenant A → query as tenant B → zero rows; migration of a second tenant
   after a new migration is added.

## Explicitly out of scope for the first cut

Moving a live tenant between modes (data migration), cross-tenant reporting under `Schema` mode,
and `Database` mode (same seam, different connection factory).
