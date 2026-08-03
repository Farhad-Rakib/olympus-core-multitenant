using ProjectNamePlaceholder.Application.SiteSettings.Dtos;

namespace ProjectNamePlaceholder.Application.Auth.Dtos;

public sealed record LoginResultDto(AuthTokensDto Tokens, TenantBrandingDto Branding);
