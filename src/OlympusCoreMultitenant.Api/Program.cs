using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OlympusCoreMultitenant.Api.Authorization;
using OlympusCoreMultitenant.Api.Middleware;
using OlympusCoreMultitenant.Application;
using OlympusCoreMultitenant.Application.Security;
using OlympusCoreMultitenant.Infrastructure;
using OlympusCoreMultitenant.Infrastructure.Authentication;
using OlympusCoreMultitenant.Infrastructure.Logging;
using OlympusCoreMultitenant.Persistence;
using OlympusCoreMultitenant.Persistence.Seeding;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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

// Add CORS
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
        // Production: Restrict to specific origins
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")?.Get<string[]>() 
            ?? new[] { "https://yourdomain.com" };

        options.AddPolicy("AllowFrontend", policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
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
builder.Services.Configure<OlympusCoreMultitenant.Application.Common.Configuration.AuthOptions>(builder.Configuration.GetSection("Auth"));

// Caching: bind options and register distributed cache (Redis if configured)
var cachingSection = builder.Configuration.GetSection("Caching");
builder.Services.Configure<OlympusCoreMultitenant.Application.Common.Configuration.CachingOptions>(cachingSection);
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

builder.Services.AddScoped<OlympusCoreMultitenant.Application.Permissions.IPermissionCache, OlympusCoreMultitenant.Api.Startup.DistributedPermissionCache>();
builder.Services.AddScoped<OlympusCoreMultitenant.Application.Common.Interfaces.IAppCache, OlympusCoreMultitenant.Api.Startup.DistributedAppCache>();
builder.Services.AddScoped<OlympusCoreMultitenant.Api.Startup.RedisCacheAdminService>();
builder.Services.AddSingleton<OlympusCoreMultitenant.Api.Startup.CacheKeyRegistry>();
builder.Services.AddScoped<OlympusCoreMultitenant.Api.Startup.DistributedCacheAdminService>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey));

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

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.All)
    {
        options.AddPolicy(permission, policy =>
            policy.Requirements.Add(new PermissionRequirement(permission)));
    }

    options.AddPolicy(Policies.UsersReadOwnOrAny, policy =>
        policy.Requirements.Add(new SelfOrPermissionRequirement(Permissions.UsersRead, "id")));
});

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, SelfOrPermissionAuthorizationHandler>();
builder.Services.AddScoped<OlympusCoreMultitenant.Api.Startup.MenuPermissionValidator>();
builder.Services.AddScoped<OlympusCoreMultitenant.Api.Startup.ModuleSyncService>();
builder.Services.AddScoped<OlympusCoreMultitenant.Api.Startup.TenantCoreModuleBackfillService>();
builder.Services.AddScoped<OlympusCoreMultitenant.Api.Startup.PermissionSyncService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseTenantResolution();
app.UseTokenValidation();
app.UseAuthorization();


app.MapHealthChecks("/health").WithMetadata(new OlympusCoreMultitenant.Api.Middleware.SkipTenantResolutionAttribute());
app.MapControllers();
app.MapHub<OlympusCoreMultitenant.Api.Hubs.NotificationHub>("/hubs/notifications");


using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogDebug($"Starting database bootstrapper...");
    var databaseBootstrapper = scope.ServiceProvider.GetRequiredService<IDatabaseBootstrapper>();
    await databaseBootstrapper.MigrateAsync();
    logger.LogDebug($"Database migration finished.");

    // Module and Permission catalogs must be synced before any tenant is provisioned --
    // RbacSeeder grants Admin only permissions belonging to modules the tenant is entitled to,
    // which requires Permission.ModuleId (backfilled here) and the Module rows to already exist.
    try
    {
        var moduleSync = scope.ServiceProvider.GetRequiredService<OlympusCoreMultitenant.Api.Startup.ModuleSyncService>();
        await moduleSync.SyncAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Module sync failed.");
    }

    // Sync permission constants into the permissions table so DB reflects code constants
    try
    {
        var sync = scope.ServiceProvider.GetRequiredService<OlympusCoreMultitenant.Api.Startup.PermissionSyncService>();
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
        var validator = scope.ServiceProvider.GetRequiredService<OlympusCoreMultitenant.Api.Startup.MenuPermissionValidator>();
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
        var coreModuleBackfill = scope.ServiceProvider.GetRequiredService<OlympusCoreMultitenant.Api.Startup.TenantCoreModuleBackfillService>();
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
