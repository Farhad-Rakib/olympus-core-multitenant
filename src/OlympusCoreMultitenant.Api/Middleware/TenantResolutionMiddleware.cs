using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.Common.Interfaces;
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

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext, ICurrentTenantService currentTenantService)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null || endpoint.Metadata.GetMetadata<ISkipTenantResolutionMetadata>() is not null)
        {
            await _next(context);
            return;
        }

        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;

        if (context.User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(tenantIdClaim))
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

            currentTenantService.Set(tenant.Id, tenant.Slug);
        }
        else
        {
            var slug = context.Request.Headers[TenantHeaderName].ToString().Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(slug))
            {
                if (endpoint.Metadata.GetMetadata<IOptionalTenantResolutionMetadata>() is not null)
                {
                    await _next(context);
                    return;
                }

                await WriteProblemAsync(context, StatusCodes.Status400BadRequest, $"The {TenantHeaderName} header is required.");
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
