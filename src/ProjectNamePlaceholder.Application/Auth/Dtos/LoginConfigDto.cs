using ProjectNamePlaceholder.Application.SiteSettings.Dtos;

namespace ProjectNamePlaceholder.Application.Auth.Dtos;

// Branding is nullable on purpose: this endpoint is anonymous and the tenant is only known when
// the request arrives on a tenant's custom domain/subdomain (or carries X-Tenant-Id). On the bare
// platform host there is no tenant to brand for, and the frontend falls back to its own defaults.
public sealed record LoginConfigDto(bool RequiresTenantSlug, TenantBrandingDto? Branding);
