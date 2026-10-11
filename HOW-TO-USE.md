# Olympus Multi-Tenant — How To Use

A multi-tenant SaaS starter in two repos:

| Repo | What it is |
|---|---|
| `olympus-core-multitenant` (this one) | .NET 8 API, Clean Architecture, EF Core. Ships as a `dotnet new` template. |
| `olympus-react-multitenant` | React 18 + Vite + TypeScript admin frontend. Expected as a sibling directory. |

One database, shared schema, `TenantId` on every tenant-owned row, enforced automatically. Read
[Tenant isolation](#5-tenant-isolation-read-this-first) before writing any data-access code — it is
the one part of this codebase you must not work around.

> **Which doc do I want?** This one covers the platform as a whole — both repos, the template, and
> the operational concerns. `GETTING-STARTED.md` is for someone who has already run
> `dotnet new olympus-multi` and wants to build their first feature in the generated project.
> `README.md` is the architecture overview and roadmap.

---

## 1. Prerequisites

- .NET 8 SDK
- Node.js 18+
- PostgreSQL 14+ *or* SQL Server 2019+ (both are fully implemented)
- Docker (optional — for Redis, RabbitMQ, MailHog)

---

## 2. Run the backend

> ⚠️ **This repo is template *source* and cannot be run directly.** `appsettings.json` contains
> unquoted `dotnet new` placeholders (`USE_REDIS_TOKEN`, `USE_RABBITMQ_TOKEN`, `DB_PROVIDER_TOKEN`)
> which are not valid JSON, so startup fails while loading configuration — before any environment
> variable or `appsettings.Development.json` override can apply. **Generate a project first (§4) and
> run that.** To run this repo in place anyway, replace those three tokens with real values
> (`true`/`false` and `postgres`/`sqlserver`) and do not commit the change.

In a generated project:

```bash
cd src
cp .env.template .env.local          # then fill in real values
dotnet restore
dotnet run --project MyProduct.Api
```

Migrations run automatically at startup, guarded by a distributed boot lock so concurrent replicas
in a rolling deploy can't race each other.

Swagger (development only): `https://localhost:5001/swagger`

### The bootstrap admin

On an empty database a single platform super admin is created from configuration:

```json
"Bootstrap": {
  "PlatformAdminEmail": "superadmin@localhost",
  "PlatformAdminPassword": ""
}
```

Leave the password blank and one is generated and **written to the startup log exactly once**.
Capture it then — it is never logged again. Set it explicitly to control it yourself.

This account is the only user seeded automatically. Tenants create their own users.

---

## 3. Run the frontend

```bash
cd ../olympus-react-multitenant
npm install
cp .env.example .env
npm run dev
```

```bash
VITE_API_BASE_URL=http://localhost:5000
VITE_API_TIMEOUT_MS=30000
```

Other commands: `npm run build`, `npm run typecheck`, `npm run lint`, `npm test`.

---

## 4. Generate a new project from the template

`ProjectNamePlaceholder` is replaced everywhere.

The name given with `-n` is used as-is for the output folder; inside it, project folders,
`.csproj`s, the `.sln` and namespaces all use the identifier-safe form (`my-product` → `my_product`),
so hyphenated names work. PascalCase (`MyProduct`) still gives the cleanest result.

```bash
dotnet new install .

dotnet new olympus-multi -n MyProduct \
  --database postgres \
  --cache redis \
  --messagebroker rabbitmq
```

| Option | Choices | Default |
|---|---|---|
| `--database` | `postgres`, `sqlserver` | `postgres` |
| `--cache` | `memory`, `redis` | `memory` |
| `--messagebroker` | `inprocess`, `rabbitmq` | `inprocess` |

Each choice is a working implementation, and picking one **excludes the other's** package
references, DI wiring and migrations — the generated solution carries no dead provider code.

---

## 5. Tenant isolation (read this first)

### How it works

Any entity implementing `ITenantEntity` automatically gets:

1. **A global query filter** — applied by reflection in `ApplicationDbContext.OnModelCreating`, so a
   new entity is covered the moment it implements the interface. You cannot forget to add it.
2. **Write stamping** — `TenantId` is set on insert; a cross-tenant update throws.
3. **Fail-closed reads** — with no ambient tenant, `TenantId == null` matches nothing. The failure
   mode is *zero rows*, never *everyone's rows*.

```csharp
public sealed class Invoice : BaseEntity, ITenantEntity
{
    public long TenantId { get; set; }   // that's the whole opt-in
    public decimal Amount { get; set; }
}
```

### Two rules you must follow

**Rule 1 — `Include()` + `FirstOrDefaultAsync()` needs an explicit filter.**

EF Core silently drops the global filter from the LIMIT-1 subquery it generates for this exact
shape. This is confirmed behavior, not caution:

```csharp
// WRONG — the tenant filter is dropped from the generated SQL
return await DbContext.Users
    .Include(u => u.UserRoles)
    .FirstOrDefaultAsync(u => u.Email == email, ct);

// RIGHT
return await DbContext.Users
    .Where(u => u.TenantId == DbContext.CurrentTenantId)   // required
    .Include(u => u.UserRoles)
    .FirstOrDefaultAsync(u => u.Email == email, ct);
```

**Rule 2 — `IgnoreQueryFilters()` is a last resort.** There are exactly four uses in the codebase,
each deliberate (cross-tenant login lookup, platform admin seeding). Keep that number auditable.

### Entities that are *not* tenant-scoped

`Tenant`, `Module`, `SubscriptionPlan`, `AuditLog` and `ProcessedBillingEvent` are platform-global.
`AuditLog` and `ProcessedBillingEvent` have **no** global query filter (their tenant column is
nullable or written before any tenant scope exists), so their repositories scope manually. If you
add a query path to either, scope it yourself.

---

## 6. How a request finds its tenant

`TenantResolutionMiddleware` resolves in this order:

1. **`tenant_id` JWT claim** — normal authenticated requests
2. **Custom domain** — an exact `TenantDomain` match on the request host
3. **Subdomain** — `acme.olympus.app` → slug `acme`, when `Tenancy:BaseDomain` is set
4. **`X-Tenant-Id` header** — anonymous requests (login, registration)

**Platform admins** may override the ambient tenant with `X-Tenant-Id` to administer another tenant.
For everyone else that header is a **no-op** — a regular user cannot spoof into another tenant.
Every override writes an `Accessed` audit row recording who viewed which tenant, from where.

### Impersonating a user ("view as")

`POST /api/v1/users/{id}/impersonate` (permission `users.impersonate` — in `CoreModule` but
**not** auto-granted to the Admin role; hand it out deliberately) returns a token that *is* the
target user for `Auth:ImpersonationMinutes` (default 30) with an `act_by` claim naming the caller.
No refresh token is issued; when it expires the frontend restores the support user's own session.
Chaining is refused, tenant admins cannot impersonate platform admins, `change-password` is
blocked under impersonation, and an `Accessed` audit row (`UserImpersonation`) is written.
Frontend: "View as" on the Users page, a non-dismissible amber banner, "Stop impersonating".

The frontend exposes this as the **tenant switcher** in the header (platform admins only). Picking a
tenant stores `actingTenantSlug` in the auth store, every request sends it as `X-Tenant-Id`, the
query cache is dropped, and a violet "You are administering tenant X" banner stays up until you exit.

Endpoint opt-outs: `[SkipTenantResolution]`, `[TenantResolutionOptional]`, `[SkipLicenseCheck]`.

---

## 7. Tenant lifecycle

```
Create → Provision → Active → (Trial → Grace → Expired) → Suspend → Offboard → Purge
```

**Create + provision** (platform admin) — `POST /api/v1/tenants`, then
`POST /api/v1/tenants/{id}/provision`. Provisioning seeds module entitlements, RBAC roles, menus,
and site settings **branded with the tenant's own name**.

**Self-service signup** — generate a registration link with
`POST /api/v1/tenants/{id}/registration-link`, the invitee submits anonymously, and an admin calls
`approve-registration`, which auto-provisions and creates their admin user.

---

## 8. Billing and non-payment lockout

### How a non-paying tenant is restricted

The mechanism is **time-based, not event-based**, which is what makes it safe:

```
paid → LicenseExpiresAtUtc extended  → Active
     → date passes                   → GracePeriod (Licensing:GracePeriodDays, default 7)
     → grace passes                  → Expired    → 402 on every request
     → cancelled                     → Suspended  → 402 on every request
```

`TenantResolutionMiddleware` returns **402 Payment Required** once `IsAccessAllowed` is false. Two
deliberate carve-outs: `[SkipLicenseCheck]` on `/tenant/license` and `/tenant/overview` so the
screens that *explain* the lockout still load, and platform admins bypass entirely so they can
always get in to fix it. The frontend shows escalating banners (dismissible in grace,
non-dismissible when expired).

> **Non-payment needs no "revoke" event.** The licence simply stops being extended and lapses on its
> own. Even if a webhook never fires, a non-paying tenant is locked out automatically — the
> dangerous direction (a missed webhook granting free access) cannot happen.

### Payment gateway configuration

Provider-agnostic by design; nothing references Stripe or Paddle.

```json
"Payment": {
  "Provider": "manual",
  "Enabled": false,
  "WebhookSecret": "",
  "ApiKey": "",
  "DefaultCurrency": "USD",
  "CheckoutSuccessUrl": "",
  "CheckoutCancelUrl": "",
  "RenewalSkewHours": 12
}
```

`"manual"` is a real mode, not a stub: licences are managed by a platform admin through the existing
tenant licence endpoints. The app **refuses to start** if `Enabled` is true without a
`WebhookSecret`.

### Adopting a provider

1. Implement `IPaymentGateway` (verify signature → return a neutral `BillingEvent`; create hosted
   checkout/portal URLs)
2. Uncomment its line in the provider `switch` in `Program.cs`
3. Set `Payment:Provider` and `Payment:Enabled`

Nothing else changes. `BillingEventProcessor` maps neutral events onto licence actions:

| `BillingEventType` | Action |
|---|---|
| `SubscriptionRenewed` | `SetLicenseExpiry(periodEnd + RenewalSkewHours)` + `Unsuspend()` |
| `PaymentFailed` | **Nothing** — the grace period is the dunning window while the card is retried |
| `SubscriptionCancelled` | `Suspend()` |
| `PlanChanged` | `AssignSubscriptionPlan()` |

### Self-service checkout and portal

`TenantBillingController` (`/api/v1/tenant/billing/*`, all `[SkipLicenseCheck]` — an expired tenant
must be able to reach the thing that fixes it):

| Endpoint | Who | What |
|---|---|---|
| `GET config` | any member | `{ provider, checkoutAvailable, defaultCurrency }` — `checkoutAvailable` is false under `manual` or while `Payment:Enabled` is off |
| `GET plans` | any member | Active plans with a price, interval **and** `ExternalPriceId`, plus the tenant's current plan even if it has none of those |
| `POST checkout` `{ planKey }` | `billing.manage` | Hosted checkout URL from `IPaymentGateway.CreateCheckoutUrlAsync`; 409 for a plan that is not sellable, 501 when checkout is unavailable |
| `POST portal` | `billing.manage` | Hosted billing-portal URL |

`billing.manage` lives in `CoreModule`, so a tenant's Admin role holds it by default. The frontend's
`/billing` page lists the plans, and the licence banner links to it once the licence lapses.

### Webhook endpoint

`POST /api/v1/billing/webhook` — anonymous and `[SkipTenantResolution]`, because it is authenticated
by **signature**, and the tenant comes from the verified payload, never a header.

Three things that must stay true if you modify it:

- **Read the raw body.** Signature verification hashes exact bytes; model binding would invalidate it.
- **Idempotency.** `processed_billing_events` has a unique index on `(Provider, ExternalEventId)`,
  written in the **same transaction** as the licence change — concurrent duplicate deliveries race
  on the index and the loser rolls back rather than double-extending a licence.
- **Answer 2xx for anything a retry cannot fix** (unknown tenant, unhandled event type), or the
  provider redelivers forever. Only a bad signature returns 400.

### Choosing a provider

- **Stripe** — best APIs and control, but you are the merchant of record: you own tax registration
  and remittance everywhere you sell. Stripe Tax calculates; it does not absolve you.
- **Paddle / Lemon Squeezy** — merchant of record, handles global VAT/sales tax as the legal seller.
  Higher fee, less control.

For a small team selling internationally the tax burden usually dominates the fee difference.

---

## 9. Permissions and modules

Permissions live in **modules**, and modules have a `Kind`:

- **`ModuleKind.System`** (`PlatformModule`) — never entitled to any tenant, never delegable to a
  tenant admin. Enforced in seven independent places. `tenants.manage`, `system-settings.manage`,
  `audit-logs.read` live here.
- **Everything else** (`CoreModule`, your own) — grantable to tenants, gated by their subscription
  plan.

A tenant's Admin role automatically receives every permission from an entitled non-System module,
minus a few destructive ones.

### Adding a module

```csharp
public sealed class BillingModule : IOlympusModule
{
    public string Key => "billing";
    public string Name => "Billing";
    public ModuleKind Kind => ModuleKind.Business;   // NOT System
    public IReadOnlyList<string> Dependencies => ["core"];

    public IReadOnlyList<PermissionDefinition> Permissions =>
    [
        new("billing.read", "View invoices"),
    ];

    public IReadOnlyList<MenuDefinition> Menus =>
    [
        new("Invoices", "/invoices", "receipt", "billing.read", "Billing"),
    ];
}
```

Modules are discovered by reflection at startup — no registration call. Then run
`POST /api/v1/tenants/resync-catalog-all` to push the new permissions and menus to existing tenants.

> Put a permission in `PlatformModule` only if **no tenant** should ever hold it.

> **Persistence:** mark a module's entities `[OwnedByModule("billing")]` and register the module's
> assembly with `AddPersistence(cfg, m => m.ModuleAssemblies.Add(...))`. Its tables land in schema
> `billing`, and any core→module or module→module foreign key fails at startup. See
> `src/Docs/MODULE_PERSISTENCE.md`. Schema-per-*tenant* isolation is designed but not built:
> `src/Docs/SCHEMA_PER_TENANT.md`.

---

## 10. Per-tenant branding

Every tenant gets its own copy of these on provisioning (`SiteSetting` is tenant-scoped):

| Key | Purpose |
|---|---|
| `Site.Title` | App title + browser tab. **Seeded from the tenant's own name.** |
| `Site.LogoUrl` | Logo on the login screen |
| `Site.Tagline` | Subtitle under the title |
| `Site.FaviconUrl` | Browser tab icon (blank by default — no file is shipped) |
| `Site.SupportEmail` | Tenant's own support contact |
| `UI.BrandColor` | Applied at runtime as the `--brand-color` CSS variable |
| `Smtp.*` | Tenant sends mail from **their own** server |

**The login page is branded too.** `GET /api/v1/auth/login-config` is anonymous and resolves the
tenant from the host, so a tenant reaching the app on their own domain sees their name and logo
*before* signing in. On the bare platform host, `branding` returns `null` and the frontend falls
back to defaults.

When adding a branding key, add it in **both** places or it will silently not apply:
`DefaultSiteSettingsSeeder` (backend) and `BRANDING_KEYS` in `site-settings.store.ts` (frontend).

### Secrets in site settings

Settings are a free-form key/value store, so secrets share a table with cosmetic values. Any key
matching `password|secret|token|credential|apikey` is **masked server-side** as `********`. Saving a
form containing the mask leaves the stored secret untouched.

---

## 10b. File uploads

`POST /api/v1/Files/upload` (authenticated, multipart field `file`) returns `{ url, fileName }`.
`GET /api/v1/Files/{tenantId}/{fileName}` serves it back.

```json
"FileStorage": {
  "Provider": "local",
  "RootPath": "App_Data/uploads",
  "PublicBaseUrl": "",
  "MaxSizeBytes": 5242880,
  "AllowedExtensions": [ ".jpg", ".jpeg", ".png", ".gif", ".webp" ]
}
```

Files are written to `{RootPath}/{tenantId}/{random}{ext}` — outside `wwwroot`, so nothing is served
merely by existing on disk. `PublicBaseUrl` is for a CDN; left blank the URL is built from the
incoming request, which is what makes it work in development where the API and frontend are on
different origins.

**Four things that must stay true if you change this:**

- **The client filename is never used on disk.** A fresh random name is generated, which eliminates
  path traversal, collisions and encoding tricks as a class rather than sanitising them.
- **Content is validated by magic bytes**, not the declared `Content-Type` or extension — both are
  client-controlled. A `.png` whose bytes aren't a PNG is rejected.
- **Extensions are an allowlist**, never a blocklist.
- **Storage keys are pattern-matched and confined to the storage root** before touching the
  filesystem.

> ⚠️ **The download endpoint is anonymous.** The URL is consumed by a plain `<img src>`, which cannot
> carry the bearer token (it lives in web storage, not a cookie). Access therefore rests on the key
> being unguessable — a 122-bit random name. That is appropriate for avatars and **not** appropriate
> for confidential documents. If you reuse this endpoint for those, switch to authenticated reads
> with a tenant check, or time-limited signed URLs.

### What a tenant purge does with its files

```json
"TenantOffboarding": { "FileHandling": "Delete" }
```

`Delete` (default) removes `{RootPath}/{tenantId}/` entirely; `Retain` keeps it. There is no
"anonymize" middle ground as there is for audit rows (§11) — uploads are pure tenant content with
none of the audit trail's integrity value, so the file either goes or stays.

File deletion runs **after** the database commit, deliberately. The database purge is atomic; file
deletion is not and cannot be rolled back. Deleting first would mean a failed commit leaves the
tenant intact but its uploads already destroyed. This ordering fails the safer way — the worst case
is orphaned files, which can be swept up later.

---

## 10c. Localization

**Backend** — `UseRequestLocalization` runs right after tenant resolution and sets
`CultureInfo.CurrentCulture`/`CurrentUICulture` per request from, in order:

1. `?culture=` query string
2. the tenant's `UI.Locale` site setting (`TenantLocaleRequestCultureProvider`, cached 5 min)
3. `Accept-Language`

clamped to `Localization:SupportedCultures` (default `en-US`, `en-GB`). API message strings stay
English; this makes number/date parsing, `IStringLocalizer` in your modules, and FluentValidation's
built-in messages agree with what the frontend renders. The response carries `Content-Language`.

**UI translations live in the database, per tenant.** English ships bundled with the frontend
(`src/core/i18n/locales/en.json`) and is the fallback for every key; every other language is a set
of overrides a tenant admin enters on the **Translations** page (`/translations`, permission
`translations.manage`, in `CoreModule`). Rows are `(TenantId, Language, Key) → Value` in
`translations`, keys mirror the English bundle's dotted paths (`common.save`), and a blank value
deletes the override rather than storing an empty string.

| Endpoint | Auth | Notes |
|---|---|---|
| `GET /api/v1/translations/{lang}` | anonymous, `[TenantResolutionOptional]`, `[SkipLicenseCheck]` | Flat map for the tenant resolved from host / `X-Tenant-Id`; empty on the bare platform host. Cached 1 h per tenant+language, evicted on write. |
| `GET /api/v1/translations/languages` | anonymous | Languages the tenant has any rows for |
| `PUT /api/v1/translations/{lang}` | `translations.manage` | Bulk upsert `{ entries: [{ key, value }] }` |
| `DELETE /api/v1/translations/{lang}` | `translations.manage` | Remove the language entirely |

On the client, `loadLanguage(lang)` fetches the bundle, nests it into i18next's resource shape and
switches language; any key the tenant has not translated resolves to the English string, so a
10 %-translated language is fully usable. A failed fetch still switches language (English fills
in) rather than blocking. The active language is: the viewer's own choice from the header
language switcher (persisted per browser) → otherwise the language part of the tenant's
`UI.Locale`. Date/number formatting keeps the full tenant locale regardless (`format.ts`).

New and recently touched components use `useTranslation`; most older pages still hardcode
English — migrate them screen by screen as you touch them, adding keys to `en.json` (the
Translations page picks new keys up automatically because it reads the bundled file).

## 11. Audit logging

Every write is captured automatically in `SaveChanges` — no per-service opt-in:

- Updates record **only changed properties**
- `PasswordHash`, `TokenHash`, `ReplacedByTokenHash` are never written
- `RefreshToken`, `PasswordResetToken`, `AuditLog` itself are excluded entirely
- `Accessed` rows record cross-tenant admin views

**Who sees what:** a tenant admin holds `audit-logs.read-own` and is hard-pinned to their own
tenant's rows — a hostile `?tenantId=` is ignored at the repository. Platform admins hold
`audit-logs.read` and get the cross-tenant view.

### Retention

`audit_logs` is the highest-volume table here. Beyond a row per write, an `Accessed` row is written
per **request** whenever a platform admin uses the `X-Tenant-Id` override, and `Changes` is an
uncapped JSON blob over three indexes.

```json
"AuditLog": {
  "RetentionDays": 0,
  "CleanupIntervalHours": 24,
  "CleanupBatchSize": 1000,
  "MaxBatchesPerRun": 50
}
```

`RetentionDays: 0` **disables cleanup and keeps everything** — the historical behaviour, kept for
deployments with a retention obligation. **Set a real value before production.**

`AuditLogRetentionWorker` sweeps on the interval (first run 5 minutes after startup, so it does not
compete with migrations and seeding). Deletes are batched and capped per run: a single unbounded
DELETE against a large, triple-indexed, actively-written table holds locks long enough to stall
ordinary requests. Whatever a run does not clear is picked up by the next.

### What a tenant purge does with the trail

Audit rows have no FK to `Tenant`, so before this was made explicit a purge left **orphaned rows
still holding user names and email addresses** — satisfying nobody's definition of erasure.

```json
"TenantOffboarding": { "AuditHandling": "Anonymize" }
```

| Value | Behaviour |
|---|---|
| `Anonymize` (default) | Keeps the row; nulls `UserId` and replaces `Changes` with a redaction marker. The history's shape survives without the personal data. |
| `Retain` | Keeps everything including PII. Only where a retention obligation outranks erasure. |
| `Delete` | Removes the rows outright. Cleanest erasure, total loss of history. |

`TenantId` is deliberately kept under `Anonymize`: once the tenant row is gone it is an opaque
identifier that resolves to nothing, and keeping it lets the trail still be grouped.

---

## 12. Deploying behind a proxy

The app reads the caller's IP (for rate-limit partitioning) and host (for tenant resolution), so
behind a load balancer you **must** configure forwarded headers:

```json
"Proxy": {
  "TrustAllProxies": false,
  "KnownProxies": ["10.0.0.5"],
  "KnownNetworks": ["10.0.0.0/8"]
}
```

Forwarded headers are only honored when `KnownProxies`/`KnownNetworks` is non-empty or
`TrustAllProxies` is `true`. This is deliberate: trusting `X-Forwarded-For` from any peer lets
anyone forge the rate-limiter partition key.

**Skip this and two things break silently:** all anonymous traffic collapses into one shared
rate-limit bucket, and host-based tenant resolution fails behind any proxy that rewrites Host.

Also set `Tenancy:BaseDomain` if you use subdomain routing, and a real `Jwt:SecretKey` (32+ chars).

---

## 12b. Rate limiting per tenant and plan

`RateLimiting` partitions **authenticated** traffic per tenant (`tenant_id` claim) and
**anonymous** traffic per IP with its own, tighter budget:

```json
"RateLimiting": {
  "Enabled": true,
  "PermitLimit": 300,           // per tenant per window, unless the plan says otherwise
  "WindowSeconds": 60,
  "AnonymousPermitLimit": 60,   // per IP: login, registration, login-config, translations
  "AnonymousWindowSeconds": 60,
  "QueueLimit": 0
}
```

A `SubscriptionPlan.RequestsPerMinute` above 0 overrides `PermitLimit` for that plan's tenants.
The value is stamped into the access token as `plan_rpm` at login/refresh, so the limiter never
reads the database — and a plan change takes effect at the next token refresh
(`Jwt:ExpiryMinutes`), not instantly. One tenant exhausting its bucket gets 429s; nobody else does.
Per-IP partitioning needs `Proxy:KnownNetworks` behind a load balancer (§12).

## 13. Middleware pipeline

Order matters; this is the shipped order:

```
UseForwardedHeaders (conditional) → UseExceptionHandler → UseStatusCodePages
→ UseRequestId → UseSerilogRequestLogging(+TenantSlug) → UseRouting → UseCors
→ [Swagger, dev only] → UseSecurityHeaders → UseHsts (non-dev) → UseHttpsRedirection
→ UseAuthentication → UseRateLimiter → UseTenantResolution → UseTokenValidation → UseAuthorization
```

- Tenant resolution sits **after** authentication (it needs the `tenant_id` claim) and **before**
  authorization (so permission checks run tenant-scoped).
- Security headers sit **after** the Swagger branch — the strict CSP would otherwise blank Swagger UI.
- Every request carries an `X-Request-Id` (echoed back) and logs its `TenantSlug`, so
  "find this customer's failing request" is one query.

---

## 14. Project layout

```
src/
├── ProjectNamePlaceholder.Domain/          Entities, enums. No dependencies.
├── ProjectNamePlaceholder.Application/     Services, DTOs, interfaces, modules, billing.
├── ProjectNamePlaceholder.Persistence/     EF Core, repositories, seeding, migrations.
├── ProjectNamePlaceholder.Infrastructure/  Email, caching, messaging, JWT.
├── ProjectNamePlaceholder.Api/             Controllers, middleware, DI.
└── ProjectNamePlaceholder.UnitTests/       79 tests. Layering rules enforced.
```

Layer boundaries are enforced by `Architecture/LayeringTests.cs` — violations fail the build.
Note the Application layer takes **no logging dependency**; services return result types and the Api
layer logs them.

---

## 15. Common tasks

**Add a tenant-scoped entity**
1. Implement `ITenantEntity` (filter is automatic)
2. Add an `IEntityTypeConfiguration<T>` — picked up by assembly scan
3. Add a migration (see below)
4. Follow Rule 1 (§5) in any repository method using `Include` + `FirstOrDefault`

**Add a migration** — one per provider, always through the script:

```bash
scripts/ef-migration.sh add postgres  AddMyThing
scripts/ef-migration.sh add sqlserver AddMyThing
scripts/ef-migration.sh check sqlserver     # exit 1 if the model has drifted (CI runs both)
```

> ⚠️ **Do not run `dotnet ef migrations add` by hand for SQL Server.** Both providers' migrations
> live in one assembly and only the Postgres set is compiled in the template source. Worse, EF
> writes the updated `ApplicationDbContextModelSnapshot.cs` to the *first* file of that name it
> finds under the project — `Migrations/Postgres/` — silently overwriting the other provider's
> snapshot. The script flips the compile exclusion, runs EF with the matching design-time provider,
> then puts the snapshot back where it belongs and restores the csproj.

**Add an endpoint** — `[Authorize(Policy = Permissions.YourPermission)]`. Platform admins pass any
policy automatically, so gate tenant features on the *tenant-scoped* permission.

**Run tests** — `dotnet test` (125 tests). Frontend: `npm test` (vitest, 38 tests) and `npm run e2e` (Playwright, mocked API, 7 flows). `npm run lint` includes `jsx-a11y` rules as errors.

**Docker services** — `docker compose up mailhog rabbitmq` (MailHog UI on `:8025`).

---

## 16. Before production

- [ ] Set a real `Jwt:SecretKey` and rotate the seeded admin password
- [ ] Configure `Proxy:KnownNetworks` (§12)
- [ ] Set `Tenancy:BaseDomain` if using subdomains
- [ ] Set `AuditLog:RetentionDays` — it defaults to 0, meaning keep forever (§11)
- [ ] Replace default connection-string credentials
- [ ] Set `Payment:CheckoutSuccessUrl` / `CheckoutCancelUrl` and a provider price id on every plan
      you intend to sell (§8) — without the id, the plan is admin-managed only
- [ ] Set `Localization:SupportedCultures` to the locales you actually serve, and translate the UI on the Translations page for each (§10c)
