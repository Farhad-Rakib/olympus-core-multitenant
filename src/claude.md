# Claude Notes

## Project Snapshot

This repository is a layered ASP.NET Core API built around `ProjectNamePlaceholder` namespaces.

- `ProjectNamePlaceholder.Api` hosts HTTP endpoints, middleware, Swagger, SignalR, auth, caching, and startup bootstrap.
- `ProjectNamePlaceholder.Application` contains use-case services and FluentValidation validators.
- `ProjectNamePlaceholder.Infrastructure` contains JWT, password hashing, email, and other external integrations.
- `ProjectNamePlaceholder.Persistence` contains the database context, repositories, seeding, and database bootstrap logic.
- `ProjectNamePlaceholder.Domain` contains entities, enums, common models, and value objects.

## Runtime And Composition

- Target framework is .NET 8.
- Authentication uses JWT bearer tokens.
- Authorization is policy-based and includes permission requirements plus self-or-permission checks.
- The API exposes Swagger in development and enables SignalR notifications.
- CORS allows `http://localhost:5173` in development.
- Caching can use Redis when `Caching:UseRedis` is enabled; otherwise memory cache is used.
- Database provider is `postgres` or `sqlserver`, fixed at `dotnet new olympus-multi --database <postgres|sqlserver>` generation time (only the chosen provider's packages, DI wiring, boot-lock implementation, and migrations ship — see `templates/GUIDE.md`). `Database:Provider` in `appsettings.json` reflects that choice; the raw, un-generated template repo itself only compiles Postgres migrations (`Persistence/Migrations/Postgres/`) — SQL Server migrations exist only in a `--database sqlserver`-generated solution's own `Persistence/Migrations/SqlServer/`.
- Startup performs database bootstrap, menu-permission validation, and permission sync.

## Important Configuration Keys

- `ConnectionStrings:PostgresConnection`
- `ConnectionStrings:SqlServerConnection`
- `ConnectionStrings:Redis`
- `ConnectionStrings:RabbitMq`
- `Database:Provider`
- `Integration:UseRabbitMq` (RabbitMQ-backed `IEventBus` if enabled; in-process, same-transaction otherwise. Both implementations always ship — like `Caching:UseRedis`, `dotnet new olympus-multi --messagebroker <inprocess|rabbitmq>` just seeds this config value's default)
- `Integration:RabbitMq:ExchangeName`
- `Caching:UseRedis`
- `Caching:RedisInstanceName`
- `Caching:PermissionsTtlMinutes`
- `Caching:MenusTtlMinutes`
- `Caching:RolesTtlMinutes`
- `Caching:RolePermissionsTtlMinutes`
- `Auth:EmailUniquenessScope` (`PerTenant` requires `TenantSlug` at login; `Global` looks up users by email alone)
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:SecretKey`
- `Jwt:ExpiryMinutes`
- `Jwt:RefreshTokenExpiryDays`
- `Email:Provider` (`smtp` default, or `log` to write mail to the log instead of sending it — useful locally, where the placeholder SMTP host does not resolve. Anything unrecognised falls back to `smtp`)
- `Smtp:*` (the **platform-wide fallback**. A tenant overrides any of these at runtime via its own `SiteSetting` rows — `Smtp.Host`, `Smtp.Port`, `Smtp.Username`, `Smtp.Password`, `Smtp.FromEmail`, `Smtp.FromName`, `Smtp.UseSsl` — resolved per key by `TenantSmtpSettingsProvider`, so a tenant that overrides only `Smtp.FromName` still inherits the platform host)
- `Serilog:*`
- `Bootstrap:PlatformAdminEmail` / `Bootstrap:PlatformAdminPassword` (the one-time global platform-admin account seeded on first run; leave the password empty to have one generated and logged instead — see `PlatformAdminSeeder`)

## Editing Guidance

- Keep changes consistent with the existing layered architecture.
- Prefer updating the owning layer instead of pushing logic into `Api`.
- When touching auth, check `Api/Authorization`, `Infrastructure/Authentication`, and `Application/Security` together.
- When touching persistence, verify the selected database provider and the matching connection string.
- Avoid renaming the placeholder project names unless the whole solution is being rebranded.