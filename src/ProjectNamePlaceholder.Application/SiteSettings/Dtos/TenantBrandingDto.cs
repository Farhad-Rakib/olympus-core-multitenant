namespace ProjectNamePlaceholder.Application.SiteSettings.Dtos;

public sealed record TenantBrandingDto(
    string? Title,
    string? LogoUrl,
    string? Tagline,
    string? FaviconUrl,
    string? BrandColor,
    string? SupportEmail);
