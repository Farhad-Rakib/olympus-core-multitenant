using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProjectNamePlaceholder.Api.Authorization;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Api.Middleware;
using ProjectNamePlaceholder.Application;
using ProjectNamePlaceholder.Application.Billing;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Security;
using ProjectNamePlaceholder.Infrastructure;
using ProjectNamePlaceholder.Infrastructure.Authentication;
using ProjectNamePlaceholder.Infrastructure.Logging;
using ProjectNamePlaceholder.Persistence;
using ProjectNamePlaceholder.Persistence.Seeding;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Mirrors the same tolerance GetProvider() already applies to Database:Provider. In a generated
// solution these are real booleans substituted by the `cache`/`messagebroker` template parameters;
// running the raw, un-generated template repo directly they are still the literal placeholder text,
// which is not convertible to bool and would throw on the first IOptions<CachingOptions> resolve.
// Both placeholders mean "feature not selected", so normalise them to false.
foreach (var placeholderKey in new[] { "Caching:UseRedis", "Integration:UseRabbitMq" })
{
    if (!bool.TryParse(builder.Configuration[placeholderKey], out _))
    {
        builder.Configuration[placeholderKey] = "false";
    }
}

// Auto-generate and trust development certificate for HTTPS
if (builder.Environment.IsDevelopment())
{
    try
    {
        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "dev-certs https --trust",
            UseShellExecute = true,
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
        });
        process?.WaitForExit();
    }
    catch
    {
        // Certificate generation failed, but application can still run
    }
}

builder.Host.UseSerilog((context, _, configuration) =>
    configuration.AddApplicationSinks(context.Configuration));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("x-api-version"),
        new QueryStringApiVersionReader("api-version"));
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Resolved and validated OUT HERE, not inside the AddCors callback below: that callback is an
// Action<CorsOptions> registered via Configure<>, so it does not run at startup -- it runs lazily
// the first time the CORS middleware resolves IOptions<CorsOptions>, i.e. on the first request.
// A guard inside it would not fail the build-up, which is the entire point of validating.
string[] corsAllowedOrigins = Array.Empty<string>();
if (!builder.Environment.IsDevelopment())
{
    // Previously this fell back to a hard-coded "https://yourdomain.com" -- a domain this project
    // does not own, combined with AllowCredentials(). Refuse to start instead: a misconfigured CORS
    // origin either silently breaks the real frontend or trusts somebody else's domain.
    corsAllowedOrigins = (builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .ToArray();

    if (corsAllowedOrigins.Length == 0)
    {
        throw new InvalidOperationException(
            "Cors:AllowedOrigins must list at least one origin outside Development. " +
            "Set it in appsettings.{Environment}.json, or supply Cors__AllowedOrigins__0.");
    }
}

builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy
            .WithOrigins(
                "http://localhost:5173"            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
    }
    else
    {
        // Already validated non-empty above, before the container was built.
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy
                .WithOrigins(corsAllowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    }
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Emporio.Api",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });

    options.DocInclusionPredicate((docName, apiDesc) =>
        string.Equals(apiDesc.GroupName, docName, StringComparison.OrdinalIgnoreCase));
});
builder.Services.AddHealthChecks();


builder.Services.AddSignalR();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);

// Auth: bind options controlling email-uniqueness scope (PerTenant requires TenantSlug at login, Global does not)
builder.Services.Configure<ProjectNamePlaceholder.Application.Common.Configuration.AuthOptions>(builder.Configuration.GetSection("Auth"));

// Tenancy: BaseDomain enables subdomain-based tenant resolution (e.g. acme.olympus.app -> slug "acme")
builder.Services.Configure<TenancyOptions>(builder.Configuration.GetSection("Tenancy"));

// Behind a reverse proxy / ingress the socket peer is the proxy, not the caller. Two things in
// this app depend on getting the real values: the rate limiter partitions anonymous traffic by
// RemoteIpAddress (without this every anonymous caller shares one bucket), and tenant resolution
// falls back to Request.Host for custom-domain/subdomain routing. Proxies are read from
// Proxy:KnownProxies / Proxy:KnownNetworks -- when BOTH are empty the forwarded headers are
// trusted from any peer, which is only safe when the app is not directly reachable from the
// internet, so it is opt-in via Proxy:TrustAllProxies.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownProxies.Clear();
    options.KnownNetworks.Clear();

    foreach (var proxy in builder.Configuration.GetSection("Proxy:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
    {
        if (System.Net.IPAddress.TryParse(proxy, out var address))
        {
            options.KnownProxies.Add(address);
        }
    }

    foreach (var network in builder.Configuration.GetSection("Proxy:KnownNetworks").Get<string[]>() ?? Array.Empty<string>())
    {
        var parts = network.Split('/');
        if (parts.Length == 2 && System.Net.IPAddress.TryParse(parts[0], out var prefix) && int.TryParse(parts[1], out var length))
        {
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, length));
        }
    }
});
builder.Services.Configure<ProjectNamePlaceholder.Application.Common.Configuration.TenantOffboardingOptions>(builder.Configuration.GetSection("TenantOffboarding"));
builder.Services.Configure<AuditLogOptions>(builder.Configuration.GetSection("AuditLog"));
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));
builder.Services.AddScoped<IFileStorage, ProjectNamePlaceholder.Infrastructure.Storage.LocalFileStorage>();
builder.Services.AddHostedService<ProjectNamePlaceholder.Api.Startup.AuditLogRetentionWorker>();
builder.Services.Configure<ProjectNamePlaceholder.Application.Common.Configuration.LicensingOptions>(builder.Configuration.GetSection("Licensing"));

builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection("Payment"));

// Fail fast rather than at the first webhook: an enabled gateway with no signing secret would
// accept unverified traffic that can extend a tenant's licence indefinitely.
var paymentOptions = builder.Configuration.GetSection("Payment").Get<PaymentOptions>() ?? new PaymentOptions();
if (paymentOptions.Enabled && string.IsNullOrWhiteSpace(paymentOptions.WebhookSecret))
{
    throw new InvalidOperationException(
        "Payment:Enabled is true but Payment:WebhookSecret is not set. Billing webhooks cannot be verified without it.");
}

// Provider-agnostic by design: register the IPaymentGateway matching Payment:Provider here when a
// provider is chosen. "manual" is a real mode -- licences are managed by a platform admin through
// the existing tenant licence endpoints.
builder.Services.AddScoped<IBillingEventProcessor, BillingEventProcessor>();
switch (paymentOptions.Provider?.Trim().ToLowerInvariant())
{
    // case "stripe": builder.Services.AddScoped<IPaymentGateway, StripePaymentGateway>(); break;
    // case "paddle": builder.Services.AddScoped<IPaymentGateway, PaddlePaymentGateway>(); break;
    case "manual":
    case null:
    case "":
        builder.Services.AddScoped<IPaymentGateway, ManualPaymentGateway>();
        break;
    default:
        throw new InvalidOperationException($"Unknown Payment:Provider '{paymentOptions.Provider}'.");
}

// Caching: bind options and register distributed cache (Redis if configured)
var cachingSection = builder.Configuration.GetSection("Caching");
builder.Services.Configure<ProjectNamePlaceholder.Application.Common.Configuration.CachingOptions>(cachingSection);
var useRedis = cachingSection.GetValue<bool>("UseRedis", false);
if (useRedis)
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
        options.InstanceName = cachingSection.GetValue<string>("RedisInstanceName", "olympus:");
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddScoped<ProjectNamePlaceholder.Application.Permissions.IPermissionCache, ProjectNamePlaceholder.Api.Startup.DistributedPermissionCache>();
builder.Services.AddScoped<ProjectNamePlaceholder.Application.Common.Interfaces.IAppCache, ProjectNamePlaceholder.Api.Startup.DistributedAppCache>();
builder.Services.AddScoped<ProjectNamePlaceholder.Api.Startup.RedisCacheAdminService>();
builder.Services.AddSingleton<ProjectNamePlaceholder.Api.Startup.CacheKeyRegistry>();
builder.Services.AddScoped<ProjectNamePlaceholder.Api.Startup.DistributedCacheAdminService>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

// Validated here rather than left to SymmetricSecurityKey: an empty secret throws an opaque
// ArgumentException, and a merely SHORT one is worse -- it constructs fine and only fails later,
// at the first token signing. HS256 requires a key of at least 256 bits.
const int minimumSecretKeyBytes = 32;
var secretKeyBytes = Encoding.UTF8.GetBytes(jwtOptions.SecretKey ?? string.Empty);

if (secretKeyBytes.Length < minimumSecretKeyBytes)
{
    throw new InvalidOperationException(
        $"Jwt:SecretKey must be at least {minimumSecretKeyBytes} bytes (256 bits) for HS256; " +
        $"got {secretKeyBytes.Length}. Set it in appsettings.json, or supply the Jwt__SecretKey " +
        "environment variable.");
}

var key = new SymmetricSecurityKey(secretKeyBytes);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            ClockSkew = TimeSpan.Zero
        };
    });

// Discovered once at boot (assembly scan, not I/O) and registered as a singleton -- every service
// that used to consume the static Modules.All/Permissions.All/Modules.PermissionModuleMap now
// consumes this instead. Replace the scanned assembly list with a growing set of module-provider
// assemblies as real business modules are added later.
var discoveredModules = new ProjectNamePlaceholder.Application.ModuleSystem.ModuleHost()
    .DiscoverModules([typeof(ProjectNamePlaceholder.Application.DependencyInjection).Assembly]);
builder.Services.AddSingleton<IReadOnlyList<ProjectNamePlaceholder.Application.ModuleSystem.IOlympusModule>>(discoveredModules);

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in discoveredModules.SelectMany(m => m.Permissions))
    {
        options.AddPolicy(permission.Name, policy =>
            policy.Requirements.Add(new PermissionRequirement(permission.Name)));
    }

    options.AddPolicy(Policies.UsersReadOwnOrAny, policy =>
        policy.Requirements.Add(new SelfOrPermissionRequirement(Permissions.UsersRead, "id")));
});

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, SelfOrPermissionAuthorizationHandler>();

// Rate limiting: partitioned per tenant (JWT tenant_id claim) so one noisy tenant can't starve
// another's throughput; unauthenticated requests (login, registration submission) partition by
// remote IP instead. Reads the raw claim directly rather than ICurrentTenantService -- this
// middleware runs before tenant resolution validates the tenant is active, and a stable rate-limit
// key doesn't need that validation to have already happened.
var rateLimitingOptions = builder.Configuration.GetSection("RateLimiting").Get<RateLimitingOptions>() ?? new RateLimitingOptions();
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection("RateLimiting"));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    if (rateLimitingOptions.Enabled)
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var partitionKey = context.User.FindFirst("tenant_id")?.Value
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitingOptions.PermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitingOptions.WindowSeconds),
                QueueLimit = rateLimitingOptions.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
        });
    }

    options.OnRejected = async (rejectedContext, cancellationToken) =>
    {
        rejectedContext.HttpContext.Response.ContentType = "application/json";
        var response = ApiResponse.FailureResponse(
            "Too many requests. Please try again later.",
            StatusCodes.Status429TooManyRequests);
        await rejectedContext.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
    };
});
// Integration Hub: RabbitMQ-backed IEventBus if configured, in-process (same-transaction, no delivery
// guarantees) otherwise -- see Integration/IEventBus.cs and templates/GUIDE.md §8.
var useRabbitMq = builder.Configuration.GetSection("Integration").GetValue<bool>("UseRabbitMq", false);
if (useRabbitMq)
{
    builder.Services.AddSingleton<ProjectNamePlaceholder.Application.Integration.IEventBus, ProjectNamePlaceholder.Infrastructure.Messaging.RabbitMqEventBus>();
    builder.Services.AddHostedService<ProjectNamePlaceholder.Infrastructure.Messaging.RabbitMqEventConsumer>();
}
else
{
    builder.Services.AddScoped<ProjectNamePlaceholder.Application.Integration.IEventBus, ProjectNamePlaceholder.Api.Startup.InProcessEventBus>();
}
builder.Services.AddScoped<ProjectNamePlaceholder.Api.Startup.MenuPermissionValidator>();
builder.Services.AddScoped<ProjectNamePlaceholder.Api.Startup.ModuleSyncService>();
builder.Services.AddScoped<ProjectNamePlaceholder.Api.Startup.TenantCoreModuleBackfillService>();
builder.Services.AddScoped<ProjectNamePlaceholder.Api.Startup.PermissionSyncService>();

var app = builder.Build();

// Must run before anything that reads the caller's IP, scheme or host -- it rewrites those from
// the X-Forwarded-* headers the proxy set. TrustAllProxies is required to accept them from an
// unlisted peer (see the ForwardedHeadersOptions registration above).
var trustAllProxies = builder.Configuration.GetValue<bool>("Proxy:TrustAllProxies");
var hasKnownProxies = (builder.Configuration.GetSection("Proxy:KnownProxies").Get<string[]>()?.Length ?? 0) > 0
    || (builder.Configuration.GetSection("Proxy:KnownNetworks").Get<string[]>()?.Length ?? 0) > 0;

if (trustAllProxies || hasKnownProxies)
{
    app.UseForwardedHeaders();
}

app.UseExceptionHandler();

// Bare status codes (a 404 from an unmatched route, the rate limiter's 429) return an empty body
// otherwise, while everything else in the API answers with ProblemDetails.
app.UseStatusCodePages();

app.UseRequestId();
app.UseSerilogRequestLogging(options =>
{
    // Runs at the end of the request, by which point TenantResolutionMiddleware has resolved the
    // tenant -- so the completion log line for every request carries the tenant it belonged to.
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        var tenant = httpContext.RequestServices.GetService<ICurrentTenantService>();
        if (tenant?.TenantSlug is { } slug)
        {
            diagnosticContext.Set("TenantSlug", slug);
        }
    };
});
app.UseRouting();

// CORS must be placed before authentication/authorization middleware
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Emporio.Api v1");
        // Add JWT Bearer input to Swagger UI
        options.ConfigObject.AdditionalItems["persistAuthorization"] = true;
    });
}

// Placed after the Swagger branch so it does not apply to Swagger UI's own HTML/JS, which the
// strict default-src would otherwise block. Swagger is development-only anyway.
app.UseSecurityHeaders();

if (!app.Environment.IsDevelopment())
{
    // Not enabled in development: an HSTS pin on localhost outlives the debugging session and
    // breaks any other local http:// project on the same host.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRateLimiter();
app.UseTenantResolution();
app.UseTokenValidation();
app.UseAuthorization();


app.MapHealthChecks("/health").WithMetadata(new ProjectNamePlaceholder.Api.Middleware.SkipTenantResolutionAttribute()).DisableRateLimiting();
app.MapControllers();
app.MapHub<ProjectNamePlaceholder.Api.Hubs.NotificationHub>("/hubs/notifications");


using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    // Guards the whole migration/catalog-sync sequence below against multiple instances starting
    // concurrently (e.g. a rolling deploy with >1 replica) -- without this, two instances can race
    // EF migrations against the same database at once.
    var bootLock = scope.ServiceProvider.GetRequiredService<ProjectNamePlaceholder.Application.Common.Interfaces.IDistributedBootLock>();
    await using var bootLockHandle = await bootLock.AcquireAsync();

    logger.LogDebug($"Starting database bootstrapper...");
    var databaseBootstrapper = scope.ServiceProvider.GetRequiredService<IDatabaseBootstrapper>();
    await databaseBootstrapper.MigrateAsync();
    logger.LogDebug($"Database migration finished.");

    // Module and Permission catalogs must be synced before any tenant is provisioned --
    // RbacSeeder grants Admin only permissions belonging to modules the tenant is entitled to,
    // which requires Permission.ModuleId (backfilled here) and the Module rows to already exist.
    try
    {
        var moduleSync = scope.ServiceProvider.GetRequiredService<ProjectNamePlaceholder.Api.Startup.ModuleSyncService>();
        await moduleSync.SyncAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Module sync failed.");
    }

    // Sync permission constants into the permissions table so DB reflects code constants
    try
    {
        var sync = scope.ServiceProvider.GetRequiredService<ProjectNamePlaceholder.Api.Startup.PermissionSyncService>();
        await sync.SyncAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Permission sync failed.");
    }

    // Validate menus reference existing permissions. Controlled by configuration key: Validation:MenuPermissionStrict (bool, default false)
    try
    {
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var strict = config.GetValue<bool>("Validation:MenuPermissionStrict", false);
        var validator = scope.ServiceProvider.GetRequiredService<ProjectNamePlaceholder.Api.Startup.MenuPermissionValidator>();
        await validator.ValidateAsync(strict);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Menu permission validation failed.");
        if (ex is InvalidOperationException)
            throw;
    }

    // One-time-per-environment fix-up: tenants provisioned before Core became a revocable
    // TenantModule entitlement have no TenantModule row for it yet. Idempotent, cheap no-op once
    // every tenant has caught up. Runs before the default tenant is (re)provisioned below since it
    // only concerns tenants that already existed prior to this feature.
    try
    {
        var coreModuleBackfill = scope.ServiceProvider.GetRequiredService<ProjectNamePlaceholder.Api.Startup.TenantCoreModuleBackfillService>();
        await coreModuleBackfill.BackfillAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Tenant Core module entitlement backfill failed.");
    }

    await databaseBootstrapper.SeedDefaultTenantAsync();
    logger.LogDebug($"Database bootstrapper finished.");

    // Verify Seq (if enabled) is reachable and Serilog is emitting
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    try
    {
        var enableSeq = configuration.GetValue<bool?>("Serilog:EnableSeq") ?? true;
        if (enableSeq)
        {
            var seqUrl = configuration["Serilog:SeqServerUrl"] ?? "http://localhost:5341";
            try
            {
                using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                var resp = await http.GetAsync(seqUrl, cancellationToken: CancellationToken.None);
                if (resp.IsSuccessStatusCode)
                {
                    logger.LogInformation("Seq is reachable at {Url}", seqUrl);
                }
                else
                {
                    logger.LogWarning("Seq responded with status {Status} at {Url}", resp.StatusCode, seqUrl);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not reach Seq at {Url}", seqUrl);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Seq verification failed.");
    }
}

var loggerApp = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
var urls = app.Urls.Any() ? string.Join(", ", app.Urls) : "default Kestrel port";
loggerApp.LogDebug($"App is running on: {urls}");
app.Run();
