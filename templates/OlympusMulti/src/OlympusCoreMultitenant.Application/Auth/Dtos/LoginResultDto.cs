using OlympusCoreMultitenant.Application.SiteSettings.Dtos;

namespace OlympusCoreMultitenant.Application.Auth.Dtos;

public sealed record LoginResultDto(AuthTokensDto Tokens, TenantBrandingDto Branding);
