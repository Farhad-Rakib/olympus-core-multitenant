namespace ProjectNamePlaceholder.Application.SiteSettings.Dtos;

public sealed record TenantBrandingDto(
    string? Title,
    string? LogoUrl,
    string? Tagline,
    string? FaviconUrl,
    string? BrandColor,
    string? SupportEmail,
    // Presentation locale and time zone travel with branding so the very first screen -- the login
    // page, which is anonymous -- can already format in the tenant's conventions.
    string? Locale,
    string? TimeZone);
