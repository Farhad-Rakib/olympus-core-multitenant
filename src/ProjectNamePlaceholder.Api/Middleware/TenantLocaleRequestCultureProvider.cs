using Microsoft.AspNetCore.Localization;
using ProjectNamePlaceholder.Application.Common.Interfaces;

namespace ProjectNamePlaceholder.Api.Middleware;

// Resolves the request culture from the tenant's own UI.Locale site setting, so the API formats
// and validates in the same locale the frontend renders in (site-settings.store.ts applies the
// very same key). Must run AFTER tenant resolution -- with no ambient tenant it yields nothing and
// the next provider (Accept-Language) takes over, which is the anonymous login/registration case.
public sealed class TenantLocaleRequestCultureProvider : RequestCultureProvider
{
    private const string LocaleSettingKey = "UI.Locale";
    private const string CachePrefix = "tenant-locale:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var currentTenant = httpContext.RequestServices.GetService<ICurrentTenantService>();
        if (currentTenant?.TenantId is not { } tenantId)
        {
            return null;
        }

        var cache = httpContext.RequestServices.GetService<IAppCache>();
        var cacheKey = CachePrefix + tenantId;

        string? locale = null;
        if (cache is not null)
        {
            locale = await cache.GetAsync<string>(cacheKey, httpContext.RequestAborted);
        }

        if (locale is null)
        {
            var settings = httpContext.RequestServices.GetService<ISiteSettingRepository>();
            if (settings is null)
            {
                return null;
            }

            // The global query filter scopes this to the ambient tenant; a tenant without the key
            // (pre-dating the seeder) caches an empty string so we don't re-query every request.
            var setting = await settings.GetByKeyAsync(LocaleSettingKey, httpContext.RequestAborted);
            locale = setting?.Value?.Trim() ?? string.Empty;

            if (cache is not null)
            {
                await cache.SetAsync(cacheKey, locale, CacheTtl, httpContext.RequestAborted);
            }
        }

        return string.IsNullOrWhiteSpace(locale) ? null : new ProviderCultureResult(locale);
    }
}
