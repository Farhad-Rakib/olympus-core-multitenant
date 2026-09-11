namespace ProjectNamePlaceholder.Application.Common.Configuration;

public enum EmailUniquenessScope
{
    PerTenant,
    Global
}

public sealed class AuthOptions
{
    public EmailUniquenessScope EmailUniquenessScope { get; set; } = EmailUniquenessScope.PerTenant;

    // Lifetime of an impersonation access token. Short on purpose: there is no refresh token for
    // these, so a support session simply expires rather than lingering in someone's browser.
    public int ImpersonationMinutes { get; set; } = 30;
}
