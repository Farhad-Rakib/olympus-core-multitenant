using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OlympusCoreMultitenant.Application.Common.Configuration;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Application.Licensing;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Domain.Enums;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.Api.Middleware;

public sealed class TenantResolutionMiddleware
{
    private const string TenantHeaderName = "X-Tenant-Id";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ApplicationDbContext dbContext,
        ICurrentTenantService currentTenantService,
        ICurrentUserService currentUserService,
        IOptions<TenancyOptions> tenancyOptions,
        ILicenseService licenseService)
    {
        var userIdClaim = context.User.FindFirst("sub")?.Value;
        if (context.User.Identity?.IsAuthenticated == true && long.TryParse(userIdClaim, out var userId))
        {
            currentUserService.Set(userId);
        }

        var endpoint = context.GetEndpoint();
        if (endpoint is null || endpoint.Metadata.GetMetadata<ISkipTenantResolutionMetadata>() is not null)
        {
            await _next(context);
            return;
        }

        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
        var isPlatformAdmin = context.User.HasClaim(c => c.Type == "platform_admin" && c.Value == "true");
        var tenantOverrideSlug = context.Request.Headers[TenantHeaderName].ToString().Trim().ToLowerInvariant();

        // Platform super admins are pinned to their home tenant's JWT claim like everyone else, but
        // they legitimately need to act on other tenants' data (manage users/roles/permissions there)
        // without re-authenticating as a member of that tenant. Let them override the ambient tenant
        // per-request via the same X-Tenant-Id header anonymous requests already use. Anyone without
        // the platform_admin claim is always confined to their JWT's tenant_id -- this header is a
        // no-op for them so a regular user can't spoof their way into another tenant's data.
        if (context.User.Identity?.IsAuthenticated == true && isPlatformAdmin && !string.IsNullOrWhiteSpace(tenantOverrideSlug))
        {
            var overrideTenant = await dbContext.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Slug == tenantOverrideSlug, context.RequestAborted);

            if (overrideTenant is null || !overrideTenant.IsActive)
            {
                await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Unknown or inactive tenant.");
                return;
            }

            currentTenantService.Set(overrideTenant.Id, overrideTenant.Slug);
            await RecordOverrideAccessAsync(dbContext, currentUserService, overrideTenant.Id, tenantIdClaim, context);
        }
        else if (context.User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(tenantIdClaim))
        {
            if (!long.TryParse(tenantIdClaim, out var tenantId))
            {
                await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Token tenant claim is invalid.");
                return;
            }

            var tenant = await dbContext.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId, context.RequestAborted);

            if (tenant is null || !tenant.IsActive)
            {
                await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Tenant is missing or inactive.");
                return;
            }

            // Licensing is checked only for ordinary tenant members, never for the platform-admin
            // override branch above -- a platform admin must still be able to reach a
            // suspended/expired tenant in order to fix it (renew, unsuspend, export, purge).
            // [SkipLicenseCheck] carves out the same allowance for a tenant's own members, but only
            // for endpoints that are themselves about explaining the license state (see the
            // attribute's doc comment) -- tenant context is still set normally either way.
            var licenseStatus = licenseService.ComputeStatus(tenant);
            var skipLicenseCheck = endpoint.Metadata.GetMetadata<ISkipLicenseCheckMetadata>() is not null;
            if (!skipLicenseCheck && !licenseService.IsAccessAllowed(licenseStatus))
            {
                await WriteProblemAsync(context, StatusCodes.Status402PaymentRequired, $"Tenant license is {licenseStatus}.");
                return;
            }

            currentTenantService.Set(tenant.Id, tenant.Slug);
        }
        else
        {
            var slug = context.Request.Headers[TenantHeaderName].ToString().Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(slug))
            {
                // No explicit X-Tenant-Id header: fall back to resolving from the Host the request
                // actually arrived on (custom domain, or a subdomain of the configured base domain)
                // -- the anonymous login/register/self-registration path a browser hits directly,
                // where there's no header to set. Only attempted when the header is absent; an
                // explicitly-provided-but-wrong header below is still a hard error, never silently
                // overridden by Host.
                var hostTenant = await TryResolveTenantFromHostAsync(context, dbContext, tenancyOptions.Value);
                if (hostTenant is not null && hostTenant.IsActive)
                {
                    currentTenantService.Set(hostTenant.Id, hostTenant.Slug);
                    await _next(context);
                    return;
                }

                if (endpoint.Metadata.GetMetadata<IOptionalTenantResolutionMetadata>() is not null)
                {
                    await _next(context);
                    return;
                }

                await WriteProblemAsync(context, StatusCodes.Status400BadRequest,
                    $"The {TenantHeaderName} header is required, or the request must arrive on a resolvable tenant domain.");
                return;
            }

            var tenant = await dbContext.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Slug == slug, context.RequestAborted);

            if (tenant is null || !tenant.IsActive)
            {
                await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Unknown or inactive tenant.");
                return;
            }

            currentTenantService.Set(tenant.Id, tenant.Slug);
        }

        await _next(context);
    }

    // Explicit access-audit entry, distinct from the generic SaveChanges interceptor: overriding
    // the ambient tenant is a read (and potential write) surface in its own right, and reviewers
    // need "who looked at tenant Y, and from which home tenant" even on requests that mutate nothing.
    private static async Task RecordOverrideAccessAsync(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        long overrideTenantId,
        string? homeTenantIdClaim,
        HttpContext context)
    {
        var changes = JsonSerializer.Serialize(new
        {
            HomeTenantId = homeTenantIdClaim,
            Method = context.Request.Method,
            Path = context.Request.Path.Value
        });

        dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = overrideTenantId,
            UserId = currentUserService.UserId,
            EntityName = "TenantContextOverride",
            Action = AuditAction.Accessed,
            Changes = changes,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(context.RequestAborted);
    }

    // Tries an exact custom-domain match first (e.g. "shop.acme.com" registered via
    // TenantsController's domain endpoints), then a subdomain-of-the-configured-base-domain match
    // (e.g. "acme.olympus.app" -> slug "acme") if TenancyOptions.BaseDomain is configured. Returns
    // null if neither resolves, in which case the caller falls back to its existing behavior.
    private static async Task<Tenant?> TryResolveTenantFromHostAsync(HttpContext context, ApplicationDbContext dbContext, TenancyOptions tenancyOptions)
    {
        var host = context.Request.Host.Host.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        var customDomain = await dbContext.TenantDomains.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Domain == host, context.RequestAborted);

        if (customDomain is not null)
        {
            return await dbContext.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == customDomain.TenantId, context.RequestAborted);
        }

        var baseDomain = tenancyOptions.BaseDomain?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(baseDomain) || !host.EndsWith("." + baseDomain, StringComparison.Ordinal))
        {
            return null;
        }

        var subdomain = host[..^(baseDomain.Length + 1)];
        if (string.IsNullOrWhiteSpace(subdomain) || subdomain.Contains('.'))
        {
            // A bare match on the base domain itself, or a deeper subdomain than one level, isn't a
            // tenant slug -- nothing to resolve.
            return null;
        }

        return await dbContext.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == subdomain, context.RequestAborted);
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        context.Response.StatusCode = statusCode;
        var problem = new ProblemDetails
        {
            Title = statusCode == StatusCodes.Status401Unauthorized ? "Unauthorized" : "Bad Request",
            Detail = detail,
            Status = statusCode
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
