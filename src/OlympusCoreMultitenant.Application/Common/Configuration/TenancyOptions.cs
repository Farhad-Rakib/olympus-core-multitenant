namespace OlympusCoreMultitenant.Application.Common.Configuration;

public sealed class TenancyOptions
{
    // e.g. "olympus.app" -- lets "acme.olympus.app" resolve to tenant slug "acme". Empty/unset
    // disables subdomain resolution entirely (existing header/claim-based resolution is unaffected).
    public string? BaseDomain { get; set; }
}
