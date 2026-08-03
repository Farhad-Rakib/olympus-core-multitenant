# Getting Started

You just generated this project from the Olympus multi-tenant platform template
(`dotnet new olympus-multi`). This doc is your first stop — what you got, how to run it, and how
to build your first feature on top of it. Every project/namespace/file name below already reads as
your actual project name — that substitution happened automatically at generation time.

---

## 1. First run

1. **Database connection.** Set `ConnectionStrings:PostgresConnection` in
   `src/ProjectNamePlaceholder.Api/appsettings.json` (or an environment-specific
   `appsettings.{Environment}.json` / environment variables) to a real PostgreSQL instance.
   Postgres is the only database provider implemented today.
2. **JWT secret.** Set `Jwt:SecretKey` to a real secret, 32+ characters. Never ship the
   placeholder value.
3. **Bootstrap admin.** Optionally set `Bootstrap:PlatformAdminEmail` /
   `Bootstrap:PlatformAdminPassword` — leave the password blank to have one generated and logged
   to the console on first boot instead.
4. Run it:

   ```bash
   cd src
   dotnet run --project ProjectNamePlaceholder.Api
   ```

On first boot the app migrates the database, discovers and syncs the module/permission/menu
catalog (see §3), and seeds a bootstrap platform-admin account plus a default tenant. Log in with
that account and you have a working, empty multi-tenant admin panel — Swagger is available at
`/swagger` in Development.

---

## 2. What you actually have

| Capability | Where |
|---|---|
| Multi-tenancy (row-level isolation, auto-scoped queries, auto-stamped writes) | `Persistence/Context/ApplicationDbContext.cs` |
| JWT auth + refresh tokens | `Infrastructure/Authentication/`, `AuthController` |
| RBAC/PBAC (permission-per-policy, never role-name checks) | `Application/Security/Permissions.cs`, `Api/Authorization/` |
| Per-tenant module entitlement + subscription plans | `TenantModule`, `SubscriptionPlan` |
| Per-tenant feature flags (deterministic rollout %) | `Application/FeatureFlags/`, `FeatureFlagsController` |
| Per-tenant licensing (trial/grace/expiry/suspension, enforced as `402`) | `Application/Licensing/`, `TenantResolutionMiddleware` |
| Module framework (`IOlympusModule` + `ModuleHost` discovery) | `Application/ModuleSystem/` |
| In-process event bus (DI-resolved handlers) | `Application/Integration/`, `Api/Startup/InProcessEventBus.cs` |
| Audit trail (every entity Create/Update/Delete, automatic) | `ApplicationDbContext.CaptureAuditEntries` |
| Per-tenant API rate limiting | `Program.cs` (`AddRateLimiter`) |
| Automated tests (xUnit + EF InMemory + NetArchTest layering checks) | `ProjectNamePlaceholder.UnitTests/` |

None of this is aspirational — it's the actual code you're looking at, not a plan.

## 3. What you don't have yet

No Product/Order/Inventory/Accounting/CRM modules, no background job runner, no message broker, no
file storage, no search engine, no payment gateway. This template is the multi-tenancy/auth/module
scaffolding layer, not a finished business application — see §5 for the recommended build order.

---

## 4. Add your first module

Business logic belongs in a module. This needs the `olympus-module`/`olympus-feature` templates
installed once, from the **source template repo** (not this generated project):

```bash
git clone https://github.com/Farhad-Rakib/olympus-core-multitenant.git
dotnet new install ./olympus-core-multitenant
```

Then, from inside *this* project:

```bash
cd src/ProjectNamePlaceholder.Application
dotnet new olympus-module -n Product --rootNamespace ProjectNamePlaceholder
```

This generates `Modules/Product/ProductModule.cs` implementing `IOlympusModule` — declare its
permissions and menu there, and `ModuleHost` picks it up automatically on next boot. No other file
needs editing: not `Program.cs`, not a permissions file, not a menu seeder.

Add a feature (vertical slice) inside it:

```bash
dotnet new olympus-feature --module Product -n CreateProduct --rootNamespace ProjectNamePlaceholder
```

Generates a Request/Response/Command/Validator/Handler under `Modules/Product/Features/CreateProduct/`,
with the two remaining manual wiring steps (DI registration + controller action) spelled out in
that folder's own `README.md`.

> Always pass `--rootNamespace` explicitly, matching this project's actual name. Its default value
> is only correct if you're scaffolding directly into the raw, un-generated template repo, which
> this project isn't.

---

## 5. Recommended build order

Add infrastructure when a real module's real requirements demand it, not speculatively:

1. **One real module first** (Product is a reasonable start — most things reference it). Prove it
   end-to-end before starting a second.
2. **A real Integration Hub**, once two modules need to talk reliably. The event bus here has no
   delivery guarantees (no outbox, no retries) — fine for one handler, not for "Order placed must
   reliably notify Inventory, Accounting, and Courier."
3. **Background jobs + a message broker**, once you have anything that isn't pure request/response.
4. **Physical module boundaries** (separate projects per module), once you have enough real modules
   that "don't reach into another module's internals" needs compiler enforcement, not just
   discipline. Two modules aren't enough to justify this — attempting it early risks a circular
   dependency with no clean way out.
5. Whatever domain-specific infrastructure you actually need (storage, search, payments, an
   accounting engine) — build each as its own module, the same way.

---

## 6. Reference

- Full narrative guide (multi-tenancy deep dive, module framework rationale, every design
  trade-off explained): `templates/GUIDE.md` in the
  [source template repo](https://github.com/Farhad-Rakib/olympus-core-multitenant).
- Configuration keys, layering rules, editing guidance for this codebase: `claude.md` at this
  project's root.
